using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for analytics data access operations.
/// </summary>
public interface IAnalyticsRepository
{
    /// <summary>
    /// Records an analytics event.
    /// </summary>
    Task<long> RecordEventAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default);

    /// <summary>
    /// Records multiple analytics events in batch.
    /// </summary>
    Task RecordEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default);

    /// <summary>
    /// Gets the total schedule views for an edition.
    /// </summary>
    Task<int> GetScheduleViewCountAsync(long editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of unique users who viewed the schedule.
    /// </summary>
    Task<int> GetUniqueViewerCountAsync(long editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);

    /// <summary>
    /// Gets the most saved engagements for an edition.
    /// </summary>
    Task<IReadOnlyList<(long EngagementId, int SaveCount)>> GetTopSavedEngagementsAsync(long editionId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets the most popular artists for an edition based on saves.
    /// </summary>
    Task<IReadOnlyList<(long ArtistId, string ArtistName, int SaveCount)>> GetTopArtistsAsync(long editionId, int limit = 10, CancellationToken ct = default);

    /// <summary>
    /// Gets event counts grouped by hour for timeline charts.
    /// </summary>
    Task<IReadOnlyList<(DateTime Hour, int Count)>> GetEventTimelineAsync(long editionId, string eventType, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets platform distribution for an edition.
    /// </summary>
    Task<IReadOnlyList<(string Platform, int Count)>> GetPlatformDistributionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of personal schedules created for an edition.
    /// </summary>
    Task<int> GetPersonalScheduleCountAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the total number of engagement saves for an edition.
    /// </summary>
    Task<int> GetTotalEngagementSavesAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets daily active users for an edition.
    /// </summary>
    Task<IReadOnlyList<(DateTime Date, int Count)>> GetDailyActiveUsersAsync(long editionId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets event counts by type for an edition.
    /// </summary>
    Task<IReadOnlyList<(string EventType, int Count)>> GetEventTypeDistributionAsync(long editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default);
}
