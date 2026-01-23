namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the refresh token.
    /// </summary>
    public long RefreshTokenId { get; set; }

    /// <summary>
    /// Gets or sets the user ID this token belongs to.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the hashed token value.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expiration date and time (UTC).
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the token was revoked (UTC).
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the token that replaced this one (for rotation).
    /// </summary>
    public long? ReplacedByTokenId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the token was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP address that created this token.
    /// </summary>
    public string? CreatedByIp { get; set; }

    /// <summary>
    /// Checks if the token is active (not expired and not revoked).
    /// </summary>
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAtUtc;
}
