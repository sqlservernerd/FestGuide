using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for notification operations.
/// </summary>
public interface INotificationService
{
    #region Device Management

    /// <summary>
    /// Registers a device for push notifications.
    /// </summary>
    Task<DeviceTokenDto> RegisterDeviceAsync(long userId, RegisterDeviceRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets all registered devices for a user.
    /// </summary>
    Task<IReadOnlyList<DeviceTokenDto>> GetDevicesAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a device.
    /// </summary>
    Task UnregisterDeviceAsync(long userId, long deviceTokenId, CancellationToken ct = default);

    /// <summary>
    /// Unregisters a device by token.
    /// </summary>
    /// <param name="userId">The ID of the user unregistering the device. Used for audit trails.</param>
    /// <param name="token">The device token to unregister.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UnregisterDeviceByTokenAsync(long userId, string token, CancellationToken ct = default);

    #endregion

    #region Notification Preferences

    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    Task<NotificationPreferenceDto> GetPreferencesAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Updates notification preferences for a user.
    /// </summary>
    Task<NotificationPreferenceDto> UpdatePreferencesAsync(long userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);

    #endregion

    #region Notification History

    /// <summary>
    /// Gets notifications for a user.
    /// </summary>
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Gets unread notification count.
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(long userId, long notificationId, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    Task MarkAllAsReadAsync(long userId, CancellationToken ct = default);

    #endregion

    #region Sending Notifications

    /// <summary>
    /// Sends a notification to a specific user.
    /// </summary>
    Task SendToUserAsync(
        long userId,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends notifications to multiple users.
    /// </summary>
    Task SendToUsersAsync(
        IEnumerable<long> userIds,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    /// <summary>
    /// Sends schedule change notification to affected attendees.
    /// </summary>
    Task SendScheduleChangeAsync(ScheduleChangeNotification change, CancellationToken ct = default);

    #endregion
}
