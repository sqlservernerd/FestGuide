using FestGuide.Infrastructure.Email;
using FestGuide.Infrastructure.Timezone;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FestGuide.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds infrastructure services including logging to the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, string? baseUrl = null)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register email service (console logger for development)
        services.AddScoped<IEmailService>(sp =>
            new ConsoleEmailService(
                sp.GetRequiredService<ILogger<ConsoleEmailService>>(),
                baseUrl ?? "https://localhost:5001"));

        // Register timezone service (NodaTime-based IANA timezone handling)
        services.AddSingleton<ITimezoneService, NodaTimeTimezoneService>();

        return services;
    }
}
