using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for NotificationLog data access operations.
/// </summary>
public interface INotificationLogRepository
{
    /// <summary>
    /// Gets a notification log by its unique identifier.
    /// </summary>
    Task<NotificationLog?> GetByIdAsync(long notificationLogId, CancellationToken ct = default);

    /// <summary>
    /// Gets notification logs for a user.
    /// </summary>
    Task<IReadOnlyList<NotificationLog>> GetByUserAsync(long userId, int limit = 50, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Gets unread notification logs for a user.
    /// </summary>
    Task<IReadOnlyList<NotificationLog>> GetUnreadByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of unread notifications for a user.
    /// </summary>
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new notification log entry.
    /// </summary>
    Task<long> CreateAsync(NotificationLog notificationLog, CancellationToken ct = default);

    /// <summary>
    /// Creates multiple notification log entries.
    /// </summary>
    Task CreateBatchAsync(IEnumerable<NotificationLog> notificationLogs, CancellationToken ct = default);

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    Task MarkAsReadAsync(long notificationLogId, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Marks all notifications as read for a user.
    /// </summary>
    Task MarkAllAsReadAsync(long userId, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Updates delivery status.
    /// </summary>
    Task UpdateDeliveryStatusAsync(long notificationLogId, bool isDelivered, string? errorMessage, CancellationToken ct = default);

    /// <summary>
    /// Deletes old notification logs (for cleanup).
    /// </summary>
    Task CleanupOldLogsAsync(int daysToKeep, CancellationToken ct = default);
}
