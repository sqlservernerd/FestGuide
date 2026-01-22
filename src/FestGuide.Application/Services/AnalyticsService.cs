using System.Text.Json;
using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Analytics service implementation.
/// </summary>
public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analyticsRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IEngagementRepository _engagementRepository;
    private readonly IArtistRepository _artistRepository;
    private readonly ITimeSlotRepository _timeSlotRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IFestivalAuthorizationService _authService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IAnalyticsRepository analyticsRepository,
        IEditionRepository editionRepository,
        IFestivalRepository festivalRepository,
        IEngagementRepository engagementRepository,
        IArtistRepository artistRepository,
        ITimeSlotRepository timeSlotRepository,
        IStageRepository stageRepository,
        IFestivalAuthorizationService authService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AnalyticsService> logger)
    {
        _analyticsRepository = analyticsRepository ?? throw new ArgumentNullException(nameof(analyticsRepository));
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _engagementRepository = engagementRepository ?? throw new ArgumentNullException(nameof(engagementRepository));
        _artistRepository = artistRepository ?? throw new ArgumentNullException(nameof(artistRepository));
        _timeSlotRepository = timeSlotRepository ?? throw new ArgumentNullException(nameof(timeSlotRepository));
        _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task TrackEventAsync(Guid? userId, TrackEventRequest request, CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        
        Guid? festivalId = null;
        if (request.EditionId.HasValue)
        {
            var edition = await _editionRepository.GetByIdAsync(request.EditionId.Value, ct);
            festivalId = edition?.FestivalId;
        }

        var analyticsEvent = new AnalyticsEvent
        {
            AnalyticsEventId = Guid.NewGuid(),
            UserId = userId,
            FestivalId = festivalId,
            EditionId = request.EditionId,
            EventType = request.EventType,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            Metadata = request.Metadata != null ? JsonSerializer.Serialize(request.Metadata) : null,
            Platform = request.Platform,
            SessionId = request.SessionId,
            EventTimestampUtc = now,
            CreatedAtUtc = now
        };

        await _analyticsRepository.RecordEventAsync(analyticsEvent, ct);

        _logger.LogDebug("Analytics event recorded: {EventType} for edition {EditionId}",
            request.EventType, request.EditionId);
    }

    /// <inheritdoc />
    public async Task TrackScheduleViewAsync(Guid? userId, Guid editionId, string? platform, string? sessionId, CancellationToken ct = default)
    {
        var request = new TrackEventRequest(
            EventType: "schedule_view",
            EditionId: editionId,
            EntityType: "Schedule",
            EntityId: null,
            Platform: platform,
            SessionId: sessionId,
            Metadata: null);

        await TrackEventAsync(userId, request, ct);
    }

    /// <inheritdoc />
    public async Task TrackEngagementSaveAsync(Guid userId, Guid editionId, Guid engagementId, CancellationToken ct = default)
    {
        var request = new TrackEventRequest(
            EventType: "engagement_save",
            EditionId: editionId,
            EntityType: "Engagement",
            EntityId: engagementId,
            Platform: null,
            SessionId: null,
            Metadata: null);

        await TrackEventAsync(userId, request, ct);
    }

    /// <inheritdoc />
    public async Task<EditionDashboardDto> GetEditionDashboardAsync(Guid editionId, Guid organizerId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var festival = await _festivalRepository.GetByIdAsync(edition.FestivalId, ct);

        var scheduleViews = await _analyticsRepository.GetScheduleViewCountAsync(editionId, ct: ct);
        var uniqueViewers = await _analyticsRepository.GetUniqueViewerCountAsync(editionId, ct: ct);
        var personalSchedules = await _analyticsRepository.GetPersonalScheduleCountAsync(editionId, ct);
        var totalSaves = await _analyticsRepository.GetTotalEngagementSavesAsync(editionId, ct);
        var topArtistsRaw = await _analyticsRepository.GetTopArtistsAsync(editionId, 5, ct);
        var topEngagementsRaw = await _analyticsRepository.GetTopSavedEngagementsAsync(editionId, 5, ct);
        var platformDistribution = await _analyticsRepository.GetPlatformDistributionAsync(editionId, ct);

        var topArtists = topArtistsRaw.Select(a => new TopArtistDto(a.ArtistId, a.ArtistName, a.SaveCount)).ToList();
        var topEngagements = await BuildTopEngagementDtosAsync(topEngagementsRaw, ct);

        var totalPlatformCount = platformDistribution.Sum(p => p.Count);
        var platforms = platformDistribution.Select(p => new PlatformDistributionDto(
            p.Platform,
            p.Count,
            totalPlatformCount > 0 ? Math.Round((decimal)p.Count / totalPlatformCount * 100, 1) : 0
        )).ToList();

        return new EditionDashboardDto(
            editionId,
            edition.Name,
            festival?.Name ?? "Unknown",
            scheduleViews,
            uniqueViewers,
            personalSchedules,
            totalSaves,
            topArtists,
            topEngagements,
            platforms);
    }

    /// <inheritdoc />
    public async Task<FestivalAnalyticsSummaryDto> GetFestivalSummaryAsync(Guid festivalId, Guid organizerId, CancellationToken ct = default)
    {
        await EnsureOrganizerAccessAsync(festivalId, organizerId, ct);

        var festival = await _festivalRepository.GetByIdAsync(festivalId, ct)
            ?? throw new FestivalNotFoundException(festivalId);

        var editions = await _editionRepository.GetByFestivalAsync(festivalId, ct);

        var editionMetricsTasks = editions.Select(async edition =>
        {
            var viewsTask = _analyticsRepository.GetScheduleViewCountAsync(edition.EditionId, ct: ct);
            var schedulesTask = _analyticsRepository.GetPersonalScheduleCountAsync(edition.EditionId, ct);
            var savesTask = _analyticsRepository.GetTotalEngagementSavesAsync(edition.EditionId, ct);

            await Task.WhenAll(viewsTask, schedulesTask, savesTask).ConfigureAwait(false);

            return new EditionMetricsSummaryDto(
                edition.EditionId,
                edition.Name,
                await viewsTask.ConfigureAwait(false),
                await schedulesTask.ConfigureAwait(false),
                await savesTask.ConfigureAwait(false));
        });

        var editionMetrics = await Task.WhenAll(editionMetricsTasks).ConfigureAwait(false);

        var totalViews = editionMetrics.Sum(m => m.ScheduleViews);
        var totalSchedules = editionMetrics.Sum(m => m.PersonalSchedules);
        var totalSaves = editionMetrics.Sum(m => m.EngagementSaves);

        return new FestivalAnalyticsSummaryDto(
            festivalId,
            festival.Name,
            editions.Count,
            totalViews,
            totalSchedules,
            totalSaves,
            editionMetrics);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArtistAnalyticsDto>> GetTopArtistsAsync(Guid editionId, Guid organizerId, int limit = 10, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var topArtists = await _analyticsRepository.GetTopArtistsAsync(editionId, limit, ct);
        var totalSaves = topArtists.Sum(a => a.SaveCount);

        // Batch fetch all artist details to avoid N+1 queries
        var artistIds = topArtists.Select(a => a.ArtistId).ToList();
        var artistTasks = artistIds.Select(id => _artistRepository.GetByIdAsync(id, ct)).ToArray();
        var artists = await Task.WhenAll(artistTasks).ConfigureAwait(false);
        
        var artistDictionary = artists
            .Where(a => a != null)
            .ToDictionary(a => a!.ArtistId, a => a!);

        var result = new List<ArtistAnalyticsDto>();
        int rank = 1;

        foreach (var (artistId, artistName, saveCount) in topArtists)
        {
            artistDictionary.TryGetValue(artistId, out var artist);
            result.Add(new ArtistAnalyticsDto(
                artistId,
                artistName,
                artist?.ImageUrl,
                saveCount,
                rank++,
                totalSaves > 0 ? Math.Round((decimal)saveCount / totalSaves * 100, 1) : 0));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EngagementAnalyticsDto>> GetTopEngagementsAsync(Guid editionId, Guid organizerId, int limit = 10, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var topEngagements = await _analyticsRepository.GetTopSavedEngagementsAsync(editionId, limit, ct);
        
        // Batch fetch all engagements first
        var engagementIds = topEngagements.Select(e => e.EngagementId).ToList();
        var engagementTasks = engagementIds.Select(id => _engagementRepository.GetByIdAsync(id, ct)).ToArray();
        var engagements = await Task.WhenAll(engagementTasks).ConfigureAwait(false);
        
        var validEngagements = engagements
            .Where(e => e != null)
            .Select(e => e!)
            .ToList();

        if (validEngagements.Count == 0)
        {
            return Array.Empty<EngagementAnalyticsDto>();
        }

        // Batch fetch artists
        var artistIds = validEngagements.Select(e => e.ArtistId).Distinct().ToList();
        var artistTasks = artistIds.Select(id => _artistRepository.GetByIdAsync(id, ct)).ToArray();
        var artists = await Task.WhenAll(artistTasks).ConfigureAwait(false);
        var artistDictionary = artists.Where(a => a != null).ToDictionary(a => a!.ArtistId, a => a!);

        // Batch fetch time slots
        var timeSlotIds = validEngagements.Select(e => e.TimeSlotId).Distinct().ToList();
        var timeSlotTasks = timeSlotIds.Select(id => _timeSlotRepository.GetByIdAsync(id, ct)).ToArray();
        var timeSlots = await Task.WhenAll(timeSlotTasks).ConfigureAwait(false);
        var timeSlotDictionary = timeSlots.Where(t => t != null).ToDictionary(t => t!.TimeSlotId, t => t!);

        // Batch fetch stages
        var stageIds = timeSlots.Where(t => t != null).Select(t => t!.StageId).Distinct().ToList();
        var stageTasks = stageIds.Select(id => _stageRepository.GetByIdAsync(id, ct)).ToArray();
        var stages = await Task.WhenAll(stageTasks).ConfigureAwait(false);
        var stageDictionary = stages.Where(s => s != null).ToDictionary(s => s!.StageId, s => s!);

        var result = new List<EngagementAnalyticsDto>();
        int rank = 1;

        foreach (var (engagementId, saveCount) in topEngagements)
        {
            var engagement = validEngagements.FirstOrDefault(e => e.EngagementId == engagementId);
            if (engagement == null) continue;

            artistDictionary.TryGetValue(engagement.ArtistId, out var artist);
            timeSlotDictionary.TryGetValue(engagement.TimeSlotId, out var timeSlot);
            
            Domain.Entities.Stage? stage = null;
            if (timeSlot != null)
            {
                stageDictionary.TryGetValue(timeSlot.StageId, out stage);
            }

            result.Add(new EngagementAnalyticsDto(
                engagementId,
                engagement.ArtistId,
                artist?.Name ?? "Unknown",
                timeSlot?.StageId ?? Guid.Empty,
                stage?.Name ?? "Unknown",
                timeSlot?.StartTimeUtc ?? DateTime.MinValue,
                timeSlot?.EndTimeUtc ?? DateTime.MinValue,
                saveCount,
                rank++));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TimelineDataPointDto>> GetEventTimelineAsync(Guid editionId, Guid organizerId, TimelineRequest request, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var timeline = await _analyticsRepository.GetEventTimelineAsync(
            editionId,
            request.EventType ?? "schedule_view",
            request.FromUtc,
            request.ToUtc,
            ct);

        return timeline.Select(t => new TimelineDataPointDto(t.Hour, t.Count)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DailyActiveUsersDto>> GetDailyActiveUsersAsync(Guid editionId, Guid organizerId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var dau = await _analyticsRepository.GetDailyActiveUsersAsync(editionId, fromUtc, toUtc, ct);
        return dau.Select(d => new DailyActiveUsersDto(d.Date, d.Count)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PlatformDistributionDto>> GetPlatformDistributionAsync(Guid editionId, Guid organizerId, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var distribution = await _analyticsRepository.GetPlatformDistributionAsync(editionId, ct);
        var total = distribution.Sum(d => d.Count);

        return distribution.Select(d => new PlatformDistributionDto(
            d.Platform,
            d.Count,
            total > 0 ? Math.Round((decimal)d.Count / total * 100, 1) : 0
        )).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EventTypeDistributionDto>> GetEventTypeDistributionAsync(Guid editionId, Guid organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default)
    {
        var edition = await _editionRepository.GetByIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        await EnsureOrganizerAccessAsync(edition.FestivalId, organizerId, ct);

        var distribution = await _analyticsRepository.GetEventTypeDistributionAsync(editionId, fromUtc, toUtc, ct);
        var total = distribution.Sum(d => d.Count);

        return distribution.Select(d => new EventTypeDistributionDto(
            d.EventType,
            d.Count,
            total > 0 ? Math.Round((decimal)d.Count / total * 100, 1) : 0
        )).ToList();
    }

    private async Task EnsureOrganizerAccessAsync(Guid festivalId, Guid organizerId, CancellationToken ct)
    {
        if (!await _authService.CanViewAnalyticsAsync(organizerId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to view analytics for this festival.");
        }
    }

    private async Task<IReadOnlyList<TopEngagementDto>> BuildTopEngagementDtosAsync(
        IReadOnlyList<(Guid EngagementId, int SaveCount)> engagements,
        CancellationToken ct)
    {
        if (engagements.Count == 0)
        {
            return Array.Empty<TopEngagementDto>();
        }

        // Batch fetch all engagements
        var engagementIds = engagements.Select(e => e.EngagementId).ToList();
        var engagementTasks = engagementIds.Select(id => _engagementRepository.GetByIdAsync(id, ct)).ToArray();
        var engagementsData = await Task.WhenAll(engagementTasks).ConfigureAwait(false);
        
        var engagementDictionary = engagementsData
            .Where(e => e != null)
            .ToDictionary(e => e!.EngagementId, e => e!);

        if (engagementDictionary.Count == 0)
        {
            return Array.Empty<TopEngagementDto>();
        }

        // Batch fetch all artists
        var artistIds = engagementDictionary.Values.Select(e => e.ArtistId).Distinct().ToList();
        var artistTasks = artistIds.Select(id => _artistRepository.GetByIdAsync(id, ct)).ToArray();
        var artists = await Task.WhenAll(artistTasks).ConfigureAwait(false);
        var artistDictionary = artists.Where(a => a != null).ToDictionary(a => a!.ArtistId, a => a!);

        // Batch fetch all time slots
        var timeSlotIds = engagementDictionary.Values.Select(e => e.TimeSlotId).Distinct().ToList();
        var timeSlotTasks = timeSlotIds.Select(id => _timeSlotRepository.GetByIdAsync(id, ct)).ToArray();
        var timeSlots = await Task.WhenAll(timeSlotTasks).ConfigureAwait(false);
        var timeSlotDictionary = timeSlots.Where(t => t != null).ToDictionary(t => t!.TimeSlotId, t => t!);

        // Batch fetch all stages
        var stageIds = timeSlots.Where(t => t != null).Select(t => t!.StageId).Distinct().ToList();
        var stageTasks = stageIds.Select(id => _stageRepository.GetByIdAsync(id, ct)).ToArray();
        var stages = await Task.WhenAll(stageTasks).ConfigureAwait(false);
        var stageDictionary = stages.Where(s => s != null).ToDictionary(s => s!.StageId, s => s!);

        // Build DTOs
        var result = new List<TopEngagementDto>();
        foreach (var (engagementId, saveCount) in engagements)
        {
            if (!engagementDictionary.TryGetValue(engagementId, out var engagement))
            {
                continue;
            }

            artistDictionary.TryGetValue(engagement.ArtistId, out var artist);
            timeSlotDictionary.TryGetValue(engagement.TimeSlotId, out var timeSlot);
            
            Domain.Entities.Stage? stage = null;
            if (timeSlot != null)
            {
                stageDictionary.TryGetValue(timeSlot.StageId, out stage);
            }

            result.Add(new TopEngagementDto(
                engagementId,
                artist?.Name,
                stage?.Name,
                timeSlot?.StartTimeUtc,
                saveCount));
        }

        return result;
    }
}
