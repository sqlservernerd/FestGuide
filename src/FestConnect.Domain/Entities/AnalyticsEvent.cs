namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents an analytics event for tracking user interactions.
/// </summary>
public class AnalyticsEvent
{
    /// <summary>
    /// Gets or sets the unique identifier for the event.
    /// </summary>
    public long AnalyticsEventId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who triggered the event (null for anonymous).
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the festival related to the event.
    /// </summary>
    public long? FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the edition related to the event.
    /// </summary>
    public long? EditionId { get; set; }

    /// <summary>
    /// Gets or sets the type of event (e.g., schedule_view, artist_save, engagement_add).
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entity type involved (e.g., Schedule, Artist, Engagement).
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity ID involved.
    /// </summary>
    public long? EntityId { get; set; }

    /// <summary>
    /// Gets or sets optional JSON metadata for the event.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the platform (ios, android, web).
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// Gets or sets the device type.
    /// </summary>
    public string? DeviceType { get; set; }

    /// <summary>
    /// Gets or sets the user's session ID for grouping events.
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// Gets or sets when the event occurred.
    /// </summary>
    public DateTime EventTimestampUtc { get; set; }

    /// <summary>
    /// Gets or sets when the event was recorded in the database.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}
