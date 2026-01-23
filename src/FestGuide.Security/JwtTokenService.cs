using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace FestGuide.Security;

/// <summary>
/// JWT token service implementation.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        _signingCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _options.Issuer,
            ValidAudience = _options.Audience,
            IssuerSigningKey = key,
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <inheritdoc />
    public string GenerateAccessToken(long userId, string email, string userType)
    {
        var claims = new Dictionary<string, object>
        {
            [JwtRegisteredClaimNames.Sub] = userId.ToString(),
            [JwtRegisteredClaimNames.Email] = email,
            [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
            ["user_type"] = userType
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            Expires = GetAccessTokenExpiration(),
            SigningCredentials = _signingCredentials
        };

        var handler = new JsonWebTokenHandler();
        return handler.CreateToken(descriptor);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public DateTime GetAccessTokenExpiration()
    {
        return DateTime.UtcNow.AddMinutes(_options.AccessTokenExpirationMinutes);
    }

    /// <inheritdoc />
    public DateTime GetRefreshTokenExpiration()
    {
        return DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
    }

    /// <inheritdoc />
    public TokenValidationResult ValidateAccessToken(string token)
    {
        try
        {
            var handler = new JsonWebTokenHandler();
            var result = handler.ValidateTokenAsync(token, _validationParameters).GetAwaiter().GetResult();

            if (!result.IsValid)
            {
                return new TokenValidationResult(false, Error: result.Exception?.Message ?? "Invalid token");
            }

            var userId = long.Parse(result.Claims[ClaimTypes.NameIdentifier]?.ToString() 
                ?? result.Claims[JwtRegisteredClaimNames.Sub]?.ToString() 
                ?? string.Empty);
            var email = result.Claims[ClaimTypes.Email]?.ToString() 
                ?? result.Claims[JwtRegisteredClaimNames.Email]?.ToString();
            var userType = result.Claims["user_type"]?.ToString();

            return new TokenValidationResult(true, userId, email, userType);
        }
        catch (Exception ex)
        {
            return new TokenValidationResult(false, Error: ex.Message);
        }
    }
}
