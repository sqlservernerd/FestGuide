using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for User data access operations.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    Task<User?> GetByIdAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple users by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<User>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user exists with the specified email.
    /// </summary>
    Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<long> CreateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a user.
    /// </summary>
    Task DeleteAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Updates the user's failed login attempts count.
    /// </summary>
    Task UpdateLoginAttemptsAsync(long userId, int failedAttempts, DateTime? lockoutEndUtc, CancellationToken ct = default);

    /// <summary>
    /// Resets the user's failed login attempts after successful login.
    /// </summary>
    Task ResetLoginAttemptsAsync(long userId, CancellationToken ct = default);
}
