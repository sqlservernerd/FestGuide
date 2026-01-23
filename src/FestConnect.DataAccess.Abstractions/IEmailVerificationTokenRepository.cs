using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for EmailVerificationToken data access operations.
/// </summary>
public interface IEmailVerificationTokenRepository
{
    /// <summary>
    /// Creates a new email verification token.
    /// </summary>
    Task<long> CreateAsync(EmailVerificationToken token, CancellationToken ct = default);

    /// <summary>
    /// Gets a token by its hash value.
    /// </summary>
    Task<EmailVerificationToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent unused token for a user.
    /// </summary>
    Task<EmailVerificationToken?> GetActiveByUserIdAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Marks a token as used.
    /// </summary>
    Task MarkAsUsedAsync(long tokenId, CancellationToken ct = default);

    /// <summary>
    /// Invalidates all unused tokens for a user.
    /// </summary>
    Task InvalidateAllForUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Deletes expired tokens older than the specified date.
    /// </summary>
    Task DeleteExpiredAsync(DateTime olderThan, CancellationToken ct = default);
}
