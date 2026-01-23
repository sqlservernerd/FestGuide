namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents an email verification token for user account verification.
/// </summary>
public class EmailVerificationToken : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the token.
    /// </summary>
    public long TokenId { get; set; }

    /// <summary>
    /// Gets or sets the user ID this token belongs to.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the hashed token value.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date/time in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this token has been used.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets the date/time when the token was used (UTC).
    /// </summary>
    public DateTime? UsedAtUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether this token is still valid (not expired and not used).
    /// </summary>
    public bool IsValid => !IsUsed && ExpiresAtUtc > DateTime.UtcNow;
}
