namespace FestGuide.Security;

/// <summary>
/// Represents a JWT token pair (access + refresh).
/// </summary>
public record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>
/// Interface for JWT token operations.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a new access token for the specified user.
    /// </summary>
    string GenerateAccessToken(long userId, string email, string userType);

    /// <summary>
    /// Generates a new refresh token (cryptographically random).
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Gets the hash of a refresh token for storage.
    /// </summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Gets the access token expiration time.
    /// </summary>
    DateTime GetAccessTokenExpiration();

    /// <summary>
    /// Gets the refresh token expiration time.
    /// </summary>
    DateTime GetRefreshTokenExpiration();

    /// <summary>
    /// Validates an access token and returns claims if valid.
    /// </summary>
    TokenValidationResult ValidateAccessToken(string token);
}

/// <summary>
/// Result of token validation.
/// </summary>
public record TokenValidationResult(
    bool IsValid,
    long? UserId = null,
    string? Email = null,
    string? UserType = null,
    string? Error = null);
