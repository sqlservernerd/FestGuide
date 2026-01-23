using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for analytics operations.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Tracks an analytics event.
    /// </summary>
    Task TrackEventAsync(long? userId, TrackEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tracks a schedule view event.
    /// </summary>
    Task TrackScheduleViewAsync(long? userId, long editionId, string? platform, string? sessionId, CancellationToken ct = default);

    /// <summary>
    /// Tracks an engagement save event.
    /// </summary>
    Task TrackEngagementSaveAsync(long userId, long editionId, long engagementId, CancellationToken ct = default);

    /// <summary>
    /// Gets the dashboard summary for an edition.
    /// </summary>
    Task<EditionDashboardDto> GetEditionDashboardAsync(long editionId, long organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival-wide summary.
    /// </summary>
    Task<FestivalAnalyticsSummaryDto> GetFestivalSummaryAsync(long festivalId, long organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets top artists for an edition.
    /// </summary>
    Task<IReadOnlyList<ArtistAnalyticsDto>> GetTopArtistsAsync(long editionId, long organizerId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets top engagements for an edition.
    /// </summary>
    Task<IReadOnlyList<EngagementAnalyticsDto>> GetTopEngagementsAsync(long editionId, long organizerId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets event timeline for charts.
    /// </summary>
    Task<IReadOnlyList<TimelineDataPointDto>> GetEventTimelineAsync(long editionId, long organizerId, TimelineRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets daily active users for an edition.
    /// </summary>
    Task<IReadOnlyList<DailyActiveUsersDto>> GetDailyActiveUsersAsync(long editionId, long organizerId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets platform distribution for an edition.
    /// </summary>
    Task<IReadOnlyList<PlatformDistributionDto>> GetPlatformDistributionAsync(long editionId, long organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets event type distribution for an edition.
    /// </summary>
    Task<IReadOnlyList<EventTypeDistributionDto>> GetEventTypeDistributionAsync(long editionId, long organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}
