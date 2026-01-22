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
    /// <param name="services">
    /// The <see cref="IServiceCollection"/> to which integration services are added.
    /// </param>
    /// <param name="configuration">
    /// The application configuration used to bind <see cref="SmtpOptions"/> from the
    /// <c>"Smtp"</c> configuration section.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance to allow for fluent chaining.
    /// </returns>
    public static IServiceCollection AddIntegrationServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        // Email services - SMTP implementation (only when enabled)
        var smtpEnabled = configuration.GetValue<bool>("Smtp:Enabled", false);
        if (smtpEnabled)
        {
            services.Configure<SmtpOptions>(configuration.GetSection(SmtpOptions.SectionName));
            services.AddScoped<IEmailService, SmtpEmailService>();
        }

        // Placeholder for future implementation:
        // - IWebhookService
        // - IWidgetService
        // - ISocialSharingService

        return services;
    }
}
