using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for DeviceToken data access operations.
/// </summary>
public interface IDeviceTokenRepository
{
    /// <summary>
    /// Gets a device token by its unique identifier.
    /// </summary>
    Task<DeviceToken?> GetByIdAsync(Guid deviceTokenId, CancellationToken ct = default);

    /// <summary>
    /// Gets a device token by its token string.
    /// </summary>
    Task<DeviceToken?> GetByTokenAsync(string token, CancellationToken ct = default);

    /// <summary>
    /// Gets all active device tokens for a user.
    /// </summary>
    Task<IReadOnlyList<DeviceToken>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active device tokens for multiple users.
    /// </summary>
    Task<IReadOnlyList<DeviceToken>> GetByUsersAsync(IEnumerable<Guid> userIds, CancellationToken ct = default);

    /// <summary>
    /// Registers or updates a device token.
    /// </summary>
    Task<Guid> UpsertAsync(DeviceToken deviceToken, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a device token.
    /// </summary>
    Task DeactivateAsync(Guid deviceTokenId, Guid modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Deactivates a device token by its token string.
    /// </summary>
    Task DeactivateByTokenAsync(string token, Guid modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Deactivates all device tokens for a user.
    /// </summary>
    Task DeactivateAllForUserAsync(Guid userId, Guid modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Updates the last used timestamp for a device token.
    /// </summary>
    Task UpdateLastUsedAsync(Guid deviceTokenId, DateTime lastUsedAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Removes expired device tokens.
    /// </summary>
    Task CleanupExpiredAsync(CancellationToken ct = default);
}
