using Microsoft.Extensions.DependencyInjection;

namespace FestConnect.Security;

/// <summary>
/// Extension methods for registering security services.
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Adds security services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        return services;
    }
}
