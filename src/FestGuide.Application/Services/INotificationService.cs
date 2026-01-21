using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for notification operations.
/// </summary>
public interface INotificationService
{
    #region Device Management

    /// <summary>
    /// Registers a device for push notifications.
    /// </summary>
    Task<DeviceTokenDto> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets all registered devices for a user.
    /// </summary>
    Task<IReadOnlyList<DeviceTokenDto>> GetDevicesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a device.
    /// </summary>
    Task UnregisterDeviceAsync(Guid userId, Guid deviceTokenId, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a device by token.
    /// </summary>
    Task UnregisterDeviceByTokenAsync(string token, CancellationToken ct = default);

    #endregion

    #region Notification Preferences

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Updates notification preferences for a user.
    /// </summary>
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);

    #endregion

    #region Notification History

    /// <summary>
    /// Gets notifications for a user.
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Gets unread notification count.
    /// </summary>
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);

    #endregion

    #region Sending Notifications

    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    Task SendToUserAsync(
        Guid userId,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends notifications to multiple users.
    /// </summary>
    Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends schedule change notification to affected attendees.
    /// </summary>
    Task SendScheduleChangeAsync(ScheduleChangeNotification change, CancellationToken ct = default);

    #endregion
}
