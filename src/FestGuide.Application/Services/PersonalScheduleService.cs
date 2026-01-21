using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Personal schedule service implementation for attendees.
/// </summary>
public class PersonalScheduleService : IPersonalScheduleService
{
    private readonly IPersonalScheduleRepository _scheduleRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IEngagementRepository _engagementRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PersonalScheduleService> _logger;

    public PersonalScheduleService(
        IPersonalScheduleRepository scheduleRepository,
        IEditionRepository editionRepository,
        IFestivalRepository festivalRepository,
        IEngagementRepository engagementRepository,
        ITimeSlotRepository timeSlotRepository,
        IArtistRepository artistRepository,
        IStageRepository stageRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<PersonalScheduleService> logger)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _engagementRepository = engagementRepository ?? throw new ArgumentNullException(nameof(engagementRepository));
        _timeSlotRepository = timeSlotRepository ?? throw new ArgumentNullException(nameof(timeSlotRepository));
        _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
        _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalScheduleSummaryDto>> GetMySchedulesAsync(Guid userId, CancellationToken ct = default)
    {
        var schedules = await _scheduleRepository.GetByUserAsync(userId, ct);
        if (!schedules.Any())
        {
            return Array.Empty<PersonalScheduleSummaryDto>();
        }

        // Collect all unique edition IDs
        var editionIds = schedules.Select(s => s.EditionId).Distinct().ToList();

        // Batch fetch all editions
        var editions = await _editionRepository.GetByIdsAsync(editionIds, ct);
        var editionDict = editions.ToDictionary(e => e.EditionId);

        // Collect all unique festival IDs from the editions
        var festivalIds = editions.Select(e => e.FestivalId).Distinct().ToList();

        // Batch fetch all festivals
        var festivals = await _festivalRepository.GetByIdsAsync(festivalIds, ct);
        var festivalDict = festivals.ToDictionary(f => f.FestivalId);

        // Build result DTOs
        var result = new List<PersonalScheduleSummaryDto>();
        foreach (var schedule in schedules)
        {
            var entries = await _scheduleRepository.GetEntriesAsync(schedule.PersonalScheduleId, ct);
            editionDict.TryGetValue(schedule.EditionId, out var edition);
            Festival? festival = null;
            if (edition != null)
            {
                festivalDict.TryGetValue(edition.FestivalId, out festival);
            }

            result.Add(new PersonalScheduleSummaryDto(
                schedule.PersonalScheduleId,
                schedule.EditionId,
                edition?.Name,
                festival?.Name,
                schedule.Name,
                schedule.IsDefault,
                entries.Count));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonalScheduleDto>> GetByEditionAsync(Guid userId, Guid editionId, CancellationToken ct = default)
    {
        var schedules = await _scheduleRepository.GetByUserAndEditionAsync(userId, editionId, ct);
        var result = new List<PersonalScheduleDto>();

        foreach (var schedule in schedules)
        {
            var entries = await _scheduleRepository.GetEntriesAsync(schedule.PersonalScheduleId, ct);
            result.Add(PersonalScheduleDto.FromEntity(schedule, entries.Count));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDto> GetByIdAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        var entries = await _scheduleRepository.GetEntriesAsync(personalScheduleId, ct);
        return PersonalScheduleDto.FromEntity(schedule, entries.Count);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDetailDto> GetDetailAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        var edition = await _editionRepository.GetByIdAsync(schedule.EditionId, ct);
        var festival = edition != null ? await _festivalRepository.GetByIdAsync(edition.FestivalId, ct) : null;

        var entries = await _scheduleRepository.GetEntriesAsync(personalScheduleId, ct);
        var entryDtos = await BuildEntryDtosAsync(entries, ct);

        return new PersonalScheduleDetailDto(
            schedule.PersonalScheduleId,
            schedule.UserId,
            schedule.EditionId,
            edition?.Name,
            festival?.Name,
            schedule.Name,
            schedule.IsDefault,
            schedule.LastSyncedAtUtc,
            entryDtos);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDto> CreateAsync(Guid userId, CreatePersonalScheduleRequest request, CancellationToken ct = default)
    {
        if (!await _editionRepository.ExistsAsync(request.EditionId, ct))
        {
            throw new EditionNotFoundException(request.EditionId);
        }

        var existingSchedules = await _scheduleRepository.GetByUserAndEditionAsync(userId, request.EditionId, ct);
        var isFirstSchedule = existingSchedules.Count == 0;

        var now = _dateTimeProvider.UtcNow;
        var schedule = new PersonalSchedule
        {
            PersonalScheduleId = Guid.NewGuid(),
            UserId = userId,
            EditionId = request.EditionId,
            Name = request.Name ?? (isFirstSchedule ? "My Schedule" : $"Schedule {existingSchedules.Count + 1}"),
            IsDefault = isFirstSchedule,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _scheduleRepository.CreateAsync(schedule, ct);

        _logger.LogInformation("Personal schedule {ScheduleId} created for user {UserId} edition {EditionId}",
            schedule.PersonalScheduleId, userId, request.EditionId);

        return PersonalScheduleDto.FromEntity(schedule, 0);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDto> UpdateAsync(Guid personalScheduleId, Guid userId, UpdatePersonalScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            schedule.Name = request.Name;
        }

        if (request.IsDefault == true && !schedule.IsDefault)
        {
            // Clear default from other schedules for this edition
            var otherSchedules = await _scheduleRepository.GetByUserAndEditionAsync(userId, schedule.EditionId, ct);
            foreach (var other in otherSchedules.Where(s => s.IsDefault && s.PersonalScheduleId != personalScheduleId))
            {
                other.IsDefault = false;
                other.ModifiedAtUtc = _dateTimeProvider.UtcNow;
                other.ModifiedBy = userId;
                await _scheduleRepository.UpdateAsync(other, ct);
            }
            schedule.IsDefault = true;
        }

        schedule.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        schedule.ModifiedBy = userId;

        await _scheduleRepository.UpdateAsync(schedule, ct);

        var entries = await _scheduleRepository.GetEntriesAsync(personalScheduleId, ct);
        return PersonalScheduleDto.FromEntity(schedule, entries.Count);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        await _scheduleRepository.DeleteAsync(personalScheduleId, ct);

        _logger.LogInformation("Personal schedule {ScheduleId} deleted by user {UserId}", personalScheduleId, userId);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDto> GetOrCreateDefaultAsync(Guid userId, Guid editionId, CancellationToken ct = default)
    {
        var existing = await _scheduleRepository.GetDefaultAsync(userId, editionId, ct);
        if (existing != null)
        {
            var entries = await _scheduleRepository.GetEntriesAsync(existing.PersonalScheduleId, ct);
            return PersonalScheduleDto.FromEntity(existing, entries.Count);
        }

        return await CreateAsync(userId, new CreatePersonalScheduleRequest(editionId, null), ct);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleEntryDto> AddEntryAsync(Guid personalScheduleId, Guid userId, AddScheduleEntryRequest request, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        // Verify engagement exists
        var engagement = await _engagementRepository.GetByIdAsync(request.EngagementId, ct)
            ?? throw new EngagementNotFoundException(request.EngagementId);

        // Check if already added
        if (await _scheduleRepository.HasEngagementAsync(personalScheduleId, request.EngagementId, ct))
        {
            throw new ConflictException("This performance is already in your schedule.");
        }

        var now = _dateTimeProvider.UtcNow;
        var entry = new PersonalScheduleEntry
        {
            PersonalScheduleEntryId = Guid.NewGuid(),
            PersonalScheduleId = personalScheduleId,
            EngagementId = request.EngagementId,
            Notes = request.Notes,
            NotificationsEnabled = request.NotificationsEnabled,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _scheduleRepository.AddEntryAsync(entry, ct);

        _logger.LogInformation("Entry {EntryId} added to schedule {ScheduleId} by user {UserId}",
            entry.PersonalScheduleEntryId, personalScheduleId, userId);

        // Get details for response
        var timeSlot = await _timeSlotRepository.GetByIdAsync(engagement.TimeSlotId, ct);
        var artist = await _artistRepository.GetByIdAsync(engagement.ArtistId, ct);
        var stage = timeSlot != null ? await _stageRepository.GetByIdAsync(timeSlot.StageId, ct) : null;

        return PersonalScheduleEntryDto.FromEntity(
            entry,
            artist?.Name,
            stage?.Name,
            timeSlot?.StartTimeUtc,
            timeSlot?.EndTimeUtc);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleEntryDto> UpdateEntryAsync(Guid entryId, Guid userId, UpdateScheduleEntryRequest request, CancellationToken ct = default)
    {
        var entry = await _scheduleRepository.GetEntryByIdAsync(entryId, ct)
            ?? throw new PersonalScheduleEntryNotFoundException(entryId);

        var schedule = await _scheduleRepository.GetByIdAsync(entry.PersonalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(entry.PersonalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        if (request.Notes != null)
        {
            entry.Notes = request.Notes;
        }

        if (request.NotificationsEnabled.HasValue)
        {
            entry.NotificationsEnabled = request.NotificationsEnabled.Value;
        }

        entry.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        entry.ModifiedBy = userId;

        await _scheduleRepository.UpdateEntryAsync(entry, ct);

        // Get details for response
        var engagement = await _engagementRepository.GetByIdAsync(entry.EngagementId, ct);
        var timeSlot = engagement != null ? await _timeSlotRepository.GetByIdAsync(engagement.TimeSlotId, ct) : null;
        var artist = engagement != null ? await _artistRepository.GetByIdAsync(engagement.ArtistId, ct) : null;
        var stage = timeSlot != null ? await _stageRepository.GetByIdAsync(timeSlot.StageId, ct) : null;

        return PersonalScheduleEntryDto.FromEntity(
            entry,
            artist?.Name,
            stage?.Name,
            timeSlot?.StartTimeUtc,
            timeSlot?.EndTimeUtc);
    }

    /// <inheritdoc />
    public async Task RemoveEntryAsync(Guid entryId, Guid userId, CancellationToken ct = default)
    {
        var scheduleId = await _scheduleRepository.GetScheduleIdForEntryAsync(entryId, ct)
            ?? throw new PersonalScheduleEntryNotFoundException(entryId);

        var schedule = await _scheduleRepository.GetByIdAsync(scheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(scheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        await _scheduleRepository.RemoveEntryAsync(entryId, ct);

        _logger.LogInformation("Entry {EntryId} removed from schedule {ScheduleId} by user {UserId}",
            entryId, scheduleId, userId);
    }

    /// <inheritdoc />
    public async Task<PersonalScheduleDetailDto> SyncAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(personalScheduleId, ct)
            ?? throw new PersonalScheduleNotFoundException(personalScheduleId);

        if (schedule.UserId != userId)
        {
            throw new ForbiddenException("You do not have access to this schedule.");
        }

        // Update last synced timestamp
        var now = _dateTimeProvider.UtcNow;
        await _scheduleRepository.UpdateLastSyncedAsync(personalScheduleId, now, ct);

        // Return full schedule data
        return await GetDetailAsync(personalScheduleId, userId, ct);
    }

    private async Task<IReadOnlyList<PersonalScheduleEntryDto>> BuildEntryDtosAsync(
        IReadOnlyList<PersonalScheduleEntry> entries,
        CancellationToken ct)
    {
        if (!entries.Any())
        {
            return Array.Empty<PersonalScheduleEntryDto>();
        }

        // Collect all unique IDs
        var engagementIds = entries.Select(e => e.EngagementId).Distinct().ToList();

        // Batch fetch all engagements
        var engagements = await _engagementRepository.GetByIdsAsync(engagementIds, ct);
        var engagementDict = engagements.ToDictionary(e => e.EngagementId);

        // Collect all unique time slot, artist IDs from the engagements
        var timeSlotIds = engagements.Select(e => e.TimeSlotId).Distinct().ToList();
        var artistIds = engagements.Select(e => e.ArtistId).Distinct().ToList();

        // Batch fetch time slots and artists
        var timeSlots = await _timeSlotRepository.GetByIdsAsync(timeSlotIds, ct);
        var artists = await _artistRepository.GetByIdsAsync(artistIds, ct);

        var timeSlotDict = timeSlots.ToDictionary(ts => ts.TimeSlotId);
        var artistDict = artists.ToDictionary(a => a.ArtistId);

        // Collect all unique stage IDs from the time slots
        var stageIds = timeSlots.Select(ts => ts.StageId).Distinct().ToList();

        // Batch fetch stages
        var stages = await _stageRepository.GetByIdsAsync(stageIds, ct);
        var stageDict = stages.ToDictionary(s => s.StageId);

        // Build DTOs using the dictionaries
        var result = new List<PersonalScheduleEntryDto>();
        foreach (var entry in entries)
        {
            if (!engagementDict.TryGetValue(entry.EngagementId, out var engagement))
                continue;

            timeSlotDict.TryGetValue(engagement.TimeSlotId, out var timeSlot);
            artistDict.TryGetValue(engagement.ArtistId, out var artist);
            Stage? stage = null;
            if (timeSlot != null)
            {
                stageDict.TryGetValue(timeSlot.StageId, out stage);
            }

            result.Add(PersonalScheduleEntryDto.FromEntity(
                entry,
                artist?.Name,
                stage?.Name,
                timeSlot?.StartTimeUtc,
                timeSlot?.EndTimeUtc));
        }

        // Sort by start time
        return result.OrderBy(e => e.StartTimeUtc).ToList();
    }
}
