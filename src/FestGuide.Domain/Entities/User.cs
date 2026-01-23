using FestGuide.Domain.Enums;

namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a user account in the system.
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the normalized (lowercase) email for uniqueness checks.
    /// </summary>
    public string EmailNormalized { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the user's email has been verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the Argon2id password hash.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's display name.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of user account.
    /// </summary>
    public UserType UserType { get; set; }

    /// <summary>
    /// Gets or sets the user's preferred timezone (IANA identifier).
    /// </summary>
    public string? PreferredTimezoneId { get; set; }

    /// <summary>
    /// Gets or sets whether the user account has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the number of failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets the date and time until which the account is locked (UTC).
    /// </summary>
    public DateTime? LockoutEndUtc { get; set; }
}
