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
    Task TrackEventAsync(Guid? userId, TrackEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Tracks a schedule view event.
    /// </summary>
    Task TrackScheduleViewAsync(Guid? userId, Guid editionId, string? platform, string? sessionId, CancellationToken ct = default);

    /// <summary>
    /// Tracks an engagement save event.
    /// </summary>
    Task TrackEngagementSaveAsync(Guid userId, Guid editionId, Guid engagementId, CancellationToken ct = default);

    /// <summary>
    /// Gets the dashboard summary for an edition.
    /// </summary>
    Task<EditionDashboardDto> GetEditionDashboardAsync(Guid editionId, Guid organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival-wide summary.
    /// </summary>
    Task<FestivalAnalyticsSummaryDto> GetFestivalSummaryAsync(Guid festivalId, Guid organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets top artists for an edition.
    /// </summary>
    Task<IReadOnlyList<ArtistAnalyticsDto>> GetTopArtistsAsync(Guid editionId, Guid organizerId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets top engagements for an edition.
    /// </summary>
    Task<IReadOnlyList<EngagementAnalyticsDto>> GetTopEngagementsAsync(Guid editionId, Guid organizerId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets event timeline for charts.
    /// </summary>
    Task<IReadOnlyList<TimelineDataPointDto>> GetEventTimelineAsync(Guid editionId, Guid organizerId, TimelineRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets daily active users for an edition.
    /// </summary>
    Task<IReadOnlyList<DailyActiveUsersDto>> GetDailyActiveUsersAsync(Guid editionId, Guid organizerId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets platform distribution for an edition.
    /// </summary>
    Task<IReadOnlyList<PlatformDistributionDto>> GetPlatformDistributionAsync(Guid editionId, Guid organizerId, CancellationToken ct = default);

    /// <summary>
    /// Gets event type distribution for an edition.
    /// </summary>
    Task<IReadOnlyList<EventTypeDistributionDto>> GetEventTypeDistributionAsync(Guid editionId, Guid organizerId, DateTime? fromUtc, DateTime? toUtc, CancellationToken ct = default);
}
