using FestGuide.Infrastructure;
using FestGuide.Infrastructure.Email;
using FestGuide.Integrations.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FestGuide.Integrations;

/// <summary>
/// Extension methods for registering integration services.
/// This project will contain:
/// - Email services (SMTP)
/// - Webhooks (outbound notifications to organizer systems)
/// - Embeddable widgets (schedule, lineup, countdown)
/// - Social sharing (Open Graph, Twitter Cards)
/// - Public API support utilities
/// </summary>
public static class IntegrationServiceExtensions
{
    /// <summary>
    /// Adds integration services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Email services - SMTP implementation
        services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
        services.AddScoped<IEmailService, SmtpEmailService>();

        // Placeholder for future implementation:
        // - IWebhookService
        // - IWidgetService
        // - ISocialSharingService

        return services;
    }
}
