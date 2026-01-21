using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FestGuide.Integration.Tests;

/// <summary>
/// Custom WebApplicationFactory for integration testing.
/// Configures test-specific services and database.
/// </summary>
public class FestGuideWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // TODO: Replace database connection with test database
            // For now, tests will use in-memory or mock implementations

            // Remove existing database registrations if needed
            // var descriptor = services.SingleOrDefault(
            //     d => d.ServiceType == typeof(IDbConnection));
            // if (descriptor != null)
            // {
            //     services.Remove(descriptor);
            // }

            // Add test-specific services here
            // services.AddScoped<IDbConnection>(_ => ...);
        });
    }
}
