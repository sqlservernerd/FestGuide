using FluentValidation;
using FestConnect.Application.Authorization;
using FestConnect.Application.Services;
using FestConnect.Application.Validators;
using FestConnect.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FestConnect.Application;

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

        // Phase 3 Services - Permissions
        services.AddScoped<IPermissionService, PermissionService>();

        // Phase 4 Services - Attendee Experience
        services.AddScoped<IPersonalScheduleService, PersonalScheduleService>();

        // Phase 5 Services - Real-Time & Notifications
        services.AddScoped<INotificationService, NotificationService>();

        // Phase 6 Services - Analytics & Reporting
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IExportService, ExportService>();

        // Authorization
        services.AddScoped<IFestivalAuthorizationService, FestivalAuthorizationService>();

        // Date/Time provider
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // Validators - registered from assembly containing validator types
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
