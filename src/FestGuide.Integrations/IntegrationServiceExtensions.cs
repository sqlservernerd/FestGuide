using Microsoft.Extensions.DependencyInjection;

namespace FestGuide.Integrations;

/// <summary>
/// Extension methods for registering integration services.
/// This project will contain:
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
    public static IServiceCollection AddIntegrationServices(this IServiceCollection services)
    {
        // Placeholder for Phase 6 implementation
        // Services will include:
        // - IWebhookService
        // - IWidgetService
        // - ISocialSharingService

        return services;
    }
}
