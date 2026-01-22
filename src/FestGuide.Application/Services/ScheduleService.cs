using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Schedule service implementation.
/// </summary>
public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IEngagementRepository _engagementRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly INotificationService _notificationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ScheduleService> _logger;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        ITimeSlotRepository timeSlotRepository,
        IEngagementRepository engagementRepository,
        IStageRepository stageRepository,
        IArtistRepository artistRepository,
        IEditionRepository editionRepository,
        IFestivalAuthorizationService authorizationService,
        INotificationService notificationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ScheduleService> logger)
    {
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _timeSlotRepository = timeSlotRepository ?? throw new ArgumentNullException(nameof(timeSlotRepository));
        _engagementRepository = engagementRepository ?? throw new ArgumentNullException(nameof(engagementRepository));
        _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
        _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ScheduleDto> GetScheduleAsync(Guid editionId, CancellationToken ct = default)
    {
        if (!await _editionRepository.ExistsAsync(editionId, ct))
        {
            throw new EditionNotFoundException(editionId);
        }

        var schedule = await _scheduleRepository.GetByEditionAsync(editionId, ct);
        if (schedule == null)
        {
            // Return empty schedule representation
            return new ScheduleDto(Guid.Empty, editionId, 0, null, false);
        }

        return ScheduleDto.FromEntity(schedule);
    }

    /// <inheritdoc />
    public async Task<ScheduleDetailDto> GetScheduleDetailAsync(Guid editionId, CancellationToken ct = default)
    {
        if (!await _editionRepository.ExistsAsync(editionId, ct))
        {
            throw new EditionNotFoundException(editionId);
        }

        var schedule = await _scheduleRepository.GetByEditionAsync(editionId, ct);
        var timeSlots = await _timeSlotRepository.GetByEditionAsync(editionId, ct);
        var engagements = await _engagementRepository.GetByEditionAsync(editionId, ct);

        // Build lookup maps
        var engagementByTimeSlot = engagements.ToDictionary(e => e.TimeSlotId);

        // Batch fetch stages and artists to avoid N+1 query issue
        var stageIds = timeSlots.Select(ts => ts.StageId).Distinct().ToList();
        var artistIds = engagements.Select(e => e.ArtistId).Distinct().ToList();

        var stagesList = await _stageRepository.GetByIdsAsync(stageIds, ct);
        var artistsList = await _artistRepository.GetByIdsAsync(artistIds, ct);

        var stages = stagesList.ToDictionary(s => s.StageId);
        var artists = artistsList.ToDictionary(a => a.ArtistId);

        // Build schedule items
        var items = timeSlots.Select(ts =>
        {
            engagementByTimeSlot.TryGetValue(ts.TimeSlotId, out var engagement);
            stages.TryGetValue(ts.StageId, out var stage);
            Artist? artist = null;
            if (engagement != null)
            {
                artists.TryGetValue(engagement.ArtistId, out artist);
            }

            return new ScheduleItemDto(
                ts.TimeSlotId,
                ts.StageId,
                stage?.Name ?? "Unknown Stage",
                ts.StartTimeUtc,
                ts.EndTimeUtc,
                engagement?.EngagementId,
                engagement?.ArtistId,
                artist?.Name,
                engagement?.Notes
            );
        }).OrderBy(i => i.StartTimeUtc).ThenBy(i => i.StageName).ToList();

        return new ScheduleDetailDto(
            schedule?.ScheduleId ?? Guid.Empty,
            editionId,
            schedule?.Version ?? 0,
            schedule?.PublishedAtUtc,
            schedule?.IsPublished ?? false,
            items
        );
    }

    /// <inheritdoc />
    public async Task<ScheduleDto> PublishScheduleAsync(Guid editionId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _editionRepository.GetFestivalIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to publish this schedule.");
        }

        // Get or create schedule
        var schedule = await _scheduleRepository.GetOrCreateAsync(editionId, userId, ct);

        // Publish
        await _scheduleRepository.PublishAsync(schedule.ScheduleId, userId, ct);

        // Update edition status to published
        await _editionRepository.UpdateStatusAsync(editionId, EditionStatus.Published, userId, ct);

        // Reload schedule for updated version
        schedule = await _scheduleRepository.GetByIdAsync(schedule.ScheduleId, ct);

        _logger.LogInformation("Schedule for edition {EditionId} published by user {UserId}, version {Version}",
            editionId, userId, schedule!.Version);

        // Notify attendees who have saved entries for this edition
        // The failure is logged but does not prevent the schedule from being published
        try
        {
            var notification = new ScheduleChangeNotification(
                EditionId: editionId,
                ChangeType: "schedule_published",
                EngagementId: null,
                TimeSlotId: null,
                ArtistName: null,
                StageName: null,
                OldStartTime: null,
                NewStartTime: null,
                Message: $"The schedule has been published (version {schedule.Version}).");

            await _notificationService.SendScheduleChangeAsync(notification, ct);
        }
        catch (OperationCanceledException)
        {
            // Honor cancellation semantics
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send schedule published notification for edition {EditionId} by user {UserId}, version {Version}",
                editionId,
                userId,
                schedule.Version);
        }

        return ScheduleDto.FromEntity(schedule);
    }

    /// <inheritdoc />
    public async Task<TimeSlotDto> GetTimeSlotByIdAsync(Guid timeSlotId, CancellationToken ct = default)
    {
        var timeSlot = await _timeSlotRepository.GetByIdAsync(timeSlotId, ct)
            ?? throw new TimeSlotNotFoundException(timeSlotId);

        return TimeSlotDto.FromEntity(timeSlot);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimeSlotDto>> GetTimeSlotsByStageAsync(Guid stageId, Guid editionId, CancellationToken ct = default)
    {
        var timeSlots = await _timeSlotRepository.GetByStageAndEditionAsync(stageId, editionId, ct);
        return timeSlots.Select(TimeSlotDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<TimeSlotDto> CreateTimeSlotAsync(Guid stageId, Guid userId, CreateTimeSlotRequest request, CancellationToken ct = default)
    {
        var festivalId = await _stageRepository.GetFestivalIdAsync(stageId, ct)
            ?? throw new StageNotFoundException(stageId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to create time slots.");
        }

        if (!await _editionRepository.ExistsAsync(request.EditionId, ct))
        {
            throw new EditionNotFoundException(request.EditionId);
        }

        // Check for overlapping time slots
        if (await _timeSlotRepository.HasOverlapAsync(stageId, request.EditionId, request.StartTimeUtc, request.EndTimeUtc, null, ct))
        {
            throw new ValidationException("Time slot overlaps with an existing time slot on this stage.");
        }

        var now = _dateTimeProvider.UtcNow;
        var timeSlot = new TimeSlot
        {
            TimeSlotId = Guid.NewGuid(),
            StageId = stageId,
            EditionId = request.EditionId,
            StartTimeUtc = request.StartTimeUtc,
            EndTimeUtc = request.EndTimeUtc,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _timeSlotRepository.CreateAsync(timeSlot, ct);

        _logger.LogInformation("TimeSlot {TimeSlotId} created for stage {StageId} by user {UserId}",
            timeSlot.TimeSlotId, stageId, userId);

        return TimeSlotDto.FromEntity(timeSlot);
    }

    /// <inheritdoc />
    public async Task<TimeSlotDto> UpdateTimeSlotAsync(Guid timeSlotId, Guid userId, UpdateTimeSlotRequest request, CancellationToken ct = default)
    {
        var timeSlot = await _timeSlotRepository.GetByIdAsync(timeSlotId, ct)
            ?? throw new TimeSlotNotFoundException(timeSlotId);

        var festivalId = await _timeSlotRepository.GetFestivalIdAsync(timeSlotId, ct);
        if (festivalId == null || !await _authorizationService.HasScopeAsync(userId, festivalId.Value, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this time slot.");
        }

        var newStart = request.StartTimeUtc ?? timeSlot.StartTimeUtc;
        var newEnd = request.EndTimeUtc ?? timeSlot.EndTimeUtc;

        // Check for overlapping time slots (excluding this one)
        if (await _timeSlotRepository.HasOverlapAsync(timeSlot.StageId, timeSlot.EditionId, newStart, newEnd, timeSlotId, ct))
        {
            throw new ValidationException("Time slot would overlap with an existing time slot on this stage.");
        }

        if (request.StartTimeUtc.HasValue)
        {
            timeSlot.StartTimeUtc = request.StartTimeUtc.Value;
        }

        if (request.EndTimeUtc.HasValue)
        {
            timeSlot.EndTimeUtc = request.EndTimeUtc.Value;
        }

        timeSlot.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        timeSlot.ModifiedBy = userId;

        await _timeSlotRepository.UpdateAsync(timeSlot, ct);

        _logger.LogInformation("TimeSlot {TimeSlotId} updated by user {UserId}", timeSlotId, userId);

        return TimeSlotDto.FromEntity(timeSlot);
    }

    /// <inheritdoc />
    public async Task DeleteTimeSlotAsync(Guid timeSlotId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _timeSlotRepository.GetFestivalIdAsync(timeSlotId, ct)
            ?? throw new TimeSlotNotFoundException(timeSlotId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this time slot.");
        }

        await _timeSlotRepository.DeleteAsync(timeSlotId, userId, ct);

        _logger.LogInformation("TimeSlot {TimeSlotId} deleted by user {UserId}", timeSlotId, userId);
    }

    /// <inheritdoc />
    public async Task<EngagementDto> GetEngagementByIdAsync(Guid engagementId, CancellationToken ct = default)
    {
        var engagement = await _engagementRepository.GetByIdAsync(engagementId, ct)
            ?? throw new EngagementNotFoundException(engagementId);

        return EngagementDto.FromEntity(engagement);
    }

    /// <inheritdoc />
    public async Task<EngagementDto> CreateEngagementAsync(Guid timeSlotId, Guid userId, CreateEngagementRequest request, CancellationToken ct = default)
    {
        if (!await _timeSlotRepository.ExistsAsync(timeSlotId, ct))
        {
            throw new TimeSlotNotFoundException(timeSlotId);
        }

        var festivalId = await _timeSlotRepository.GetFestivalIdAsync(timeSlotId, ct);
        if (festivalId == null || !await _authorizationService.HasScopeAsync(userId, festivalId.Value, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to create engagements.");
        }

        if (!await _artistRepository.ExistsAsync(request.ArtistId, ct))
        {
            throw new ArtistNotFoundException(request.ArtistId);
        }

        // Check if time slot already has an engagement
        if (await _engagementRepository.TimeSlotHasEngagementAsync(timeSlotId, ct))
        {
            throw new ValidationException("This time slot already has an artist assigned.");
        }

        var now = _dateTimeProvider.UtcNow;
        var engagement = new Engagement
        {
            EngagementId = Guid.NewGuid(),
            TimeSlotId = timeSlotId,
            ArtistId = request.ArtistId,
            Notes = request.Notes,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _engagementRepository.CreateAsync(engagement, ct);

        _logger.LogInformation("Engagement {EngagementId} created for time slot {TimeSlotId} by user {UserId}",
            engagement.EngagementId, timeSlotId, userId);

        return EngagementDto.FromEntity(engagement);
    }

    /// <inheritdoc />
    public async Task<EngagementDto> UpdateEngagementAsync(Guid engagementId, Guid userId, UpdateEngagementRequest request, CancellationToken ct = default)
    {
        var engagement = await _engagementRepository.GetByIdAsync(engagementId, ct)
            ?? throw new EngagementNotFoundException(engagementId);

        var festivalId = await _engagementRepository.GetFestivalIdAsync(engagementId, ct);
        if (festivalId == null || !await _authorizationService.HasScopeAsync(userId, festivalId.Value, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this engagement.");
        }

        if (request.ArtistId.HasValue)
        {
            if (!await _artistRepository.ExistsAsync(request.ArtistId.Value, ct))
            {
                throw new ArtistNotFoundException(request.ArtistId.Value);
            }
            engagement.ArtistId = request.ArtistId.Value;
        }

        if (request.Notes != null)
        {
            engagement.Notes = request.Notes;
        }

        engagement.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        engagement.ModifiedBy = userId;

        await _engagementRepository.UpdateAsync(engagement, ct);

        _logger.LogInformation("Engagement {EngagementId} updated by user {UserId}", engagementId, userId);

        return EngagementDto.FromEntity(engagement);
    }

    /// <inheritdoc />
    public async Task DeleteEngagementAsync(Guid engagementId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _engagementRepository.GetFestivalIdAsync(engagementId, ct)
            ?? throw new EngagementNotFoundException(engagementId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this engagement.");
        }

        await _engagementRepository.DeleteAsync(engagementId, userId, ct);

        _logger.LogInformation("Engagement {EngagementId} deleted by user {UserId}", engagementId, userId);
    }
}
