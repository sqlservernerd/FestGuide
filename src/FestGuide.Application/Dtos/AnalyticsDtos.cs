namespace FestGuide.Application.Dtos;

/// <summary>
/// Request DTO for tracking an analytics event.
/// </summary>
public sealed record TrackEventRequest(
    string EventType,
    Guid? EditionId,
    string? EntityType,
    Guid? EntityId,
    string? Platform,
    string? SessionId,
    Dictionary<string, string>? Metadata);

/// <summary>
/// Summary dashboard metrics for an edition.
/// </summary>
public sealed record EditionDashboardDto(
    Guid EditionId,
    string EditionName,
    string FestivalName,
    int TotalScheduleViews,
    int UniqueViewers,
    int PersonalSchedulesCreated,
    int TotalEngagementSaves,
    IReadOnlyList<TopArtistDto> TopArtists,
    IReadOnlyList<TopEngagementDto> TopEngagements,
    IReadOnlyList<PlatformDistributionDto> PlatformDistribution);

/// <summary>
/// Top artist by saves.
/// </summary>
public sealed record TopArtistDto(
    Guid ArtistId,
    string ArtistName,
    int SaveCount);

/// <summary>
/// Top engagement/performance by saves.
/// </summary>
public sealed record TopEngagementDto(
    Guid EngagementId,
    string? ArtistName,
    string? StageName,
    DateTime? StartTimeUtc,
    int SaveCount);

/// <summary>
/// Platform usage distribution.
/// </summary>
public sealed record PlatformDistributionDto(
    string Platform,
    int Count,
    decimal Percentage);

/// <summary>
/// Timeline data point for charts.
/// </summary>
public sealed record TimelineDataPointDto(
    DateTime Timestamp,
    int Count);

/// <summary>
/// Event type distribution.
/// </summary>
public sealed record EventTypeDistributionDto(
    string EventType,
    int Count,
    decimal Percentage);

/// <summary>
/// Daily active users data.
/// </summary>
public sealed record DailyActiveUsersDto(
    DateTime Date,
    int Count);

/// <summary>
/// Request DTO for timeline queries.
/// </summary>
public sealed record TimelineRequest(
    DateTime FromUtc,
    DateTime ToUtc,
    string? EventType);

/// <summary>
/// Request DTO for export operations.
/// </summary>
public sealed record ExportRequest(
    string Format,
    bool IncludeArtists,
    bool IncludeSchedule,
    bool IncludeAnalytics,
    DateTime? FromUtc,
    DateTime? ToUtc);

/// <summary>
/// Response DTO for export operations.
/// </summary>
public sealed record ExportResultDto(
    string FileName,
    string ContentType,
    byte[] Data);

/// <summary>
/// Festival-wide analytics summary metrics.
/// </summary>
public sealed record FestivalAnalyticsSummaryDto(
    Guid FestivalId,
    string FestivalName,
    int TotalEditions,
    int TotalScheduleViews,
    int TotalPersonalSchedules,
    int TotalEngagementSaves,
    IReadOnlyList<EditionMetricsSummaryDto> EditionMetrics);

/// <summary>
/// Brief metrics for each edition in a festival summary.
/// </summary>
public sealed record EditionMetricsSummaryDto(
    Guid EditionId,
    string EditionName,
    int ScheduleViews,
    int PersonalSchedules,
    int EngagementSaves);

/// <summary>
/// Artist analytics for an edition.
/// </summary>
public sealed record ArtistAnalyticsDto(
    Guid ArtistId,
    string ArtistName,
    string? ImageUrl,
    int SaveCount,
    int Rank,
    decimal SavePercentage);

/// <summary>
/// Engagement analytics for an edition.
/// </summary>
public sealed record EngagementAnalyticsDto(
    Guid EngagementId,
    Guid ArtistId,
    string ArtistName,
    Guid StageId,
    string StageName,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    int SaveCount,
    int Rank);
