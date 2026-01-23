namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents a log entry for a sent notification.
/// </summary>
public class NotificationLog : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the notification log.
    /// </summary>
    public long NotificationLogId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who received the notification.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the device token used (if push notification).
    /// </summary>
    public long? DeviceTokenId { get; set; }

    /// <summary>
    /// Gets or sets the notification type (schedule_change, reminder, announcement, etc.).
    /// </summary>
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the notification body/message.
    /// </summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional JSON data payload.
    /// </summary>
    public string? DataPayload { get; set; }

    /// <summary>
    /// Gets or sets the related entity type (Edition, Engagement, TimeSlot, etc.).
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Gets or sets the related entity ID.
    /// </summary>
    public long? RelatedEntityId { get; set; }

    /// <summary>
    /// Gets or sets when the notification was sent.
    /// </summary>
    public DateTime SentAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether the notification was successfully delivered.
    /// </summary>
    public bool IsDelivered { get; set; }

    /// <summary>
    /// Gets or sets error message if delivery failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets when the notification was read by the user.
    /// </summary>
    public DateTime? ReadAtUtc { get; set; }
}
