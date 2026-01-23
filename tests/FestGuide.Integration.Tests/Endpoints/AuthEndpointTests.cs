using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using FestGuide.Api.Models;

namespace FestGuide.Integration.Tests.Endpoints;

/// <summary>
/// Integration tests for authentication endpoints.
/// Tests requiring database are marked with Skip until test database infrastructure is configured.
/// </summary>
public class AuthEndpointTests : IClassFixture<FestGuideWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointTests(FestGuideWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact(Skip = "Requires database - enable after test database infrastructure is configured")]
    public async Task Register_WithValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Email = $"test-{100L}@example.com",
            Password = "SecurePassword123!",
            DisplayName = "Test User",
            UserType = "attendee"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "not-an-email",
            Password = "SecurePassword123!",
            DisplayName = "Test User",
            UserType = "attendee"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires database - enable after test database infrastructure is configured")]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Profile_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
