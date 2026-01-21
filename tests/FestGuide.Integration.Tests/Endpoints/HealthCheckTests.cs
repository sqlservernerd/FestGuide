using System.Net;
using FluentAssertions;

namespace FestGuide.Integration.Tests.Endpoints;

/// <summary>
/// Integration tests for health check endpoints.
/// </summary>
public class HealthCheckTests : IClassFixture<FestGuideWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(FestGuideWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiRoot_ReturnsNotFound_ForUnknownEndpoint()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/unknown-endpoint");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
