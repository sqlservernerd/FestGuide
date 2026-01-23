using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for RefreshToken data access operations.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Gets a refresh token by its hash.
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Gets all active tokens for a user.
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new refresh token.
    /// </summary>
    Task<long> CreateAsync(RefreshToken token, CancellationToken ct = default);

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    Task RevokeAsync(long tokenId, long? replacedByTokenId, CancellationToken ct = default);

    /// <summary>
    /// Revokes all refresh tokens for a user.
    /// </summary>
    Task RevokeAllForUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Removes expired tokens (cleanup).
    /// </summary>
    Task RemoveExpiredAsync(CancellationToken ct = default);
}
