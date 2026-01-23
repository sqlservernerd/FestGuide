namespace FestConnect.Security;

/// <summary>
/// Configuration options for JWT token generation.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the secret key for signing tokens.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string Issuer { get; set; } = "FestConnect";

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string Audience { get; set; } = "FestConnect";

    /// <summary>
    /// Gets or sets the access token expiration in minutes.
    /// Default: 15 minutes.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets the refresh token expiration in days.
    /// Default: 7 days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
