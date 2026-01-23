using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for DeviceToken data access operations.
/// </summary>
public interface IDeviceTokenRepository
{
    /// <summary>
    /// Gets a device token by its unique identifier.
    /// </summary>
    Task<DeviceToken?> GetByIdAsync(long deviceTokenId, CancellationToken ct = default);

    /// <summary>
    /// Gets a device token by its token string.
    /// </summary>
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Gets all active device tokens for a user.
    /// </summary>
    Task<IReadOnlyList<DeviceToken>> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active device tokens for multiple users.
    /// </summary>
    Task<IReadOnlyList<DeviceToken>> GetByUsersAsync(IEnumerable<long> userIds, CancellationToken ct = default);

    /// <summary>
    /// Registers or updates a device token.
    /// </summary>
    Task<long> UpsertAsync(DeviceToken deviceToken, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a device token.
    /// </summary>
    Task DeactivateAsync(long deviceTokenId, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a device token by its token string.
    /// </summary>
    Task DeactivateByTokenAsync(string token, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Deactivates all device tokens for a user.
    /// </summary>
    Task DeactivateAllForUserAsync(long userId, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Updates the last used timestamp for a device token.
    /// </summary>
    Task UpdateLastUsedAsync(long deviceTokenId, DateTime lastUsedAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Removes expired device tokens.
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken ct = default);
}
