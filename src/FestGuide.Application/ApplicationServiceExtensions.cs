using FluentValidation;
using FestGuide.Application.Authorization;
using FestGuide.Application.Services;
using FestGuide.Application.Validators;
using FestGuide.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FestGuide.Application;

/// <summary>
/// Extension methods for registering application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds application services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Phase 1 Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IUserService, UserService>();

        // Phase 2 Services - Organizer Publishing
        services.AddScoped<IFestivalService, FestivalService>();
        services.AddScoped<IEditionService, EditionService>();
        services.AddScoped<IVenueService, VenueService>();
        services.AddScoped<IArtistService, ArtistService>();
        services.AddScoped<IScheduleService, ScheduleService>();

        // Authorization
        services.AddScoped<IFestivalAuthorizationService, FestivalAuthorizationService>();

        // Date/Time provider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Validators - registered from assembly containing validator types
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
