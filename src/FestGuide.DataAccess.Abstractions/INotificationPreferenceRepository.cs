using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for NotificationPreference data access operations.
/// </summary>
public interface INotificationPreferenceRepository
{
    /// <summary>
    /// Gets notification preferences for a user.
    /// </summary>
    Task<NotificationPreference?> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates notification preferences for a user.
    /// </summary>
    Task<long> UpsertAsync(NotificationPreference preference, CancellationToken ct = default);
}
