namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents a device registered for push notifications.
/// </summary>
public class DeviceToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the device token record.
    /// </summary>
    public long DeviceTokenId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns this device.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the push notification token (FCM, APNS, etc.).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the platform (ios, android, web).
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets an optional device name for user reference.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets whether this device is active for notifications.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets when the token was last successfully used.
    /// </summary>
    public DateTime? LastUsedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the token expires (if applicable).
    /// </summary>
    public DateTime? ExpiresAtUtc { get; set; }
}
