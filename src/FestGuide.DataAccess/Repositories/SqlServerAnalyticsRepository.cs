using System.Data;
using Dapper;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Repositories;

/// <summary>
/// SQL Server implementation of IAnalyticsRepository using Dapper.
/// </summary>
public class SqlServerAnalyticsRepository : IAnalyticsRepository
{
    private readonly IDbConnection _connection;

    public SqlServerAnalyticsRepository(IDbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task<Guid> RecordEventAsync(AnalyticsEvent analyticsEvent, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO analytics.AnalyticsEvent (
                AnalyticsEventId, UserId, FestivalId, EditionId, EventType,
                EntityType, EntityId, Metadata, Platform, DeviceType,
                SessionId, EventTimestampUtc, CreatedAtUtc
            ) VALUES (
                @AnalyticsEventId, @UserId, @FestivalId, @EditionId, @EventType,
                @EntityType, @EntityId, @Metadata, @Platform, @DeviceType,
                @SessionId, @EventTimestampUtc, @CreatedAtUtc
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, analyticsEvent, cancellationToken: ct));

        return analyticsEvent.AnalyticsEventId;
    }

    /// <inheritdoc />
    public async Task RecordEventsAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(events);
        const string sql = """
            INSERT INTO analytics.AnalyticsEvent (
                AnalyticsEventId, UserId, FestivalId, EditionId, EventType,
                EntityType, EntityId, Metadata, Platform, DeviceType,
                SessionId, EventTimestampUtc, CreatedAtUtc
            ) VALUES (
                @AnalyticsEventId, @UserId, @FestivalId, @EditionId, @EventType,
                @EntityType, @EntityId, @Metadata, @Platform, @DeviceType,
                @SessionId, @EventTimestampUtc, @CreatedAtUtc
            )
            """;

        await _connection.ExecuteAsync(
            new CommandDefinition(sql, events, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> GetScheduleViewCountAsync(Guid editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId 
              AND EventType = 'schedule_view'
              AND (@FromUtc IS NULL OR EventTimestampUtc >= @FromUtc)
              AND (@ToUtc IS NULL OR EventTimestampUtc <= @ToUtc)
            """;

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> GetUniqueViewerCountAsync(Guid editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(DISTINCT UserId)
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId 
              AND EventType = 'schedule_view'
              AND UserId IS NOT NULL
              AND (@FromUtc IS NULL OR EventTimestampUtc >= @FromUtc)
              AND (@ToUtc IS NULL OR EventTimestampUtc <= @ToUtc)
            """;

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid EngagementId, int SaveCount)>> GetTopSavedEngagementsAsync(Guid editionId, int limit = 10, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit) EntityId AS EngagementId, COUNT(*) AS SaveCount
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId 
              AND EventType = 'engagement_save'
              AND EntityType = 'Engagement'
              AND EntityId IS NOT NULL
            GROUP BY EntityId
            ORDER BY COUNT(*) DESC
            """;

        var result = await _connection.QueryAsync<(Guid EngagementId, int SaveCount)>(
            new CommandDefinition(sql, new { EditionId = editionId, Limit = limit }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(Guid ArtistId, string ArtistName, int SaveCount)>> GetTopArtistsAsync(Guid editionId, int limit = 10, CancellationToken ct = default)
    {
        const string sql = """
            SELECT TOP (@Limit) 
                a.ArtistId, 
                a.Name AS ArtistName, 
                COUNT(*) AS SaveCount
            FROM analytics.AnalyticsEvent ae
            INNER JOIN core.Engagement e ON ae.EntityId = e.EngagementId
            INNER JOIN core.Artist a ON e.ArtistId = a.ArtistId
            WHERE ae.EditionId = @EditionId 
              AND ae.EventType = 'engagement_save'
              AND ae.EntityType = 'Engagement'
              AND a.IsDeleted = 0
            GROUP BY a.ArtistId, a.Name
            ORDER BY COUNT(*) DESC
            """;

        var result = await _connection.QueryAsync<(Guid ArtistId, string ArtistName, int SaveCount)>(
            new CommandDefinition(sql, new { EditionId = editionId, Limit = limit }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(DateTime Hour, int Count)>> GetEventTimelineAsync(Guid editionId, string eventType, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                DATEADD(HOUR, DATEDIFF(HOUR, 0, EventTimestampUtc), 0) AS Hour,
                COUNT(*) AS Count
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId 
              AND EventType = @EventType
              AND EventTimestampUtc >= @FromUtc
              AND EventTimestampUtc <= @ToUtc
            GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, EventTimestampUtc), 0)
            ORDER BY Hour
            """;

        var result = await _connection.QueryAsync<(DateTime Hour, int Count)>(
            new CommandDefinition(sql, new { EditionId = editionId, EventType = eventType, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string Platform, int Count)>> GetPlatformDistributionAsync(Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                COALESCE(Platform, 'unknown') AS Platform,
                COUNT(*) AS Count
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId
            GROUP BY Platform
            """;

        var result = await _connection.QueryAsync<(string Platform, int Count)>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetPersonalScheduleCountAsync(Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM attendee.PersonalSchedule
            WHERE EditionId = @EditionId AND IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<int> GetTotalEngagementSavesAsync(Guid editionId, CancellationToken ct = default)
    {
        const string sql = """
            SELECT COUNT(*)
            FROM attendee.PersonalScheduleEntry pse
            INNER JOIN attendee.PersonalSchedule ps ON pse.PersonalScheduleId = ps.PersonalScheduleId
            WHERE ps.EditionId = @EditionId 
              AND pse.IsDeleted = 0 
              AND ps.IsDeleted = 0
            """;

        return await _connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EditionId = editionId }, cancellationToken: ct));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(DateTime Date, int Count)>> GetDailyActiveUsersAsync(Guid editionId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default)
    {
        const string sql = """
            SELECT 
                CAST(EventTimestampUtc AS DATE) AS Date,
                COUNT(DISTINCT UserId) AS Count
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId 
              AND UserId IS NOT NULL
              AND EventTimestampUtc >= @FromUtc
              AND EventTimestampUtc <= @ToUtc
            GROUP BY CAST(EventTimestampUtc AS DATE)
            ORDER BY Date
            """;

        var result = await _connection.QueryAsync<(DateTime Date, int Count)>(
            new CommandDefinition(sql, new { EditionId = editionId, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: ct));

        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string EventType, int Count)>> GetEventTypeDistributionAsync(Guid editionId, DateTime? fromUtc = null, DateTime? toUtc = null, CancellationToken ct = default)
    {
        const string sql = """
            SELECT EventType, COUNT(*) AS Count
            FROM analytics.AnalyticsEvent
            WHERE EditionId = @EditionId
              AND (@FromUtc IS NULL OR EventTimestampUtc >= @FromUtc)
              AND (@ToUtc IS NULL OR EventTimestampUtc <= @ToUtc)
            GROUP BY EventType
            ORDER BY COUNT(*) DESC
            """;

        var result = await _connection.QueryAsync<(string EventType, int Count)>(
            new CommandDefinition(sql, new { EditionId = editionId, FromUtc = fromUtc, ToUtc = toUtc }, cancellationToken: ct));

        return result.ToList();
    }
}
