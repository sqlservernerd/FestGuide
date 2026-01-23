using FluentAssertions;
using FluentValidation;
using Moq;
using FestGuide.Api.Controllers;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FestGuide.Api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IValidator<RegisterRequest>> _mockRegisterValidator;
    private readonly Mock<IValidator<LoginRequest>> _mockLoginValidator;
    private readonly Mock<IValidator<RefreshTokenRequest>> _mockRefreshValidator;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockRegisterValidator = new Mock<IValidator<RegisterRequest>>();
        _mockLoginValidator = new Mock<IValidator<LoginRequest>>();
        _mockRefreshValidator = new Mock<IValidator<RefreshTokenRequest>>();
        _mockLogger = new Mock<ILogger<AuthController>>();

        _sut = new AuthController(
            _mockAuthService.Object,
            _mockRegisterValidator.Object,
            _mockLoginValidator.Object,
            _mockRefreshValidator.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Register_WithValidRequest_Returns201Created()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "SecurePassword123!", "Test User", UserType.Attendee);
        var authResponse = new AuthResponse(
            100L,
            "test@example.com",
            "Test User",
            "Attendee",
            "access_token",
            DateTime.UtcNow.AddMinutes(15),
            "refresh_token",
            DateTime.UtcNow.AddDays(7));

        _mockRegisterValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _sut.Register(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var response = createdResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
        response.Data.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409Conflict()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "SecurePassword123!", "Test User", UserType.Attendee);

        _mockRegisterValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockAuthService.Setup(x => x.RegisterAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(DuplicateException.UserEmail("existing@example.com"));

        // Act
        var result = await _sut.Register(request, CancellationToken.None);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var error = conflictResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("DUPLICATE_EMAIL");
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200Ok()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "SecurePassword123!");
        var authResponse = new AuthResponse(
            101L,
            "test@example.com",
            "Test User",
            "Attendee",
            "access_token",
            DateTime.UtcNow.AddMinutes(15),
            "refresh_token",
            DateTime.UtcNow.AddDays(7));

        _mockLoginValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        var result = await _sut.Login(request, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<AuthResponse>>().Subject;
        response.Data.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");

        _mockLoginValidator.Setup(x => x.ValidateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
        _mockAuthService.Setup(x => x.LoginAsync(request, It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(AuthenticationException.InvalidCredentials());

        // Act
        var result = await _sut.Login(request, CancellationToken.None);

        // Assert
        var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var error = unauthorizedResult.Value.Should().BeOfType<ApiErrorResponse>().Subject;
        error.Error.Code.Should().Be("AUTHENTICATION_FAILED");
    }

    [Fact]
    public async Task Logout_WithValidToken_Returns204NoContent()
    {
        // Arrange
        var request = new LogoutRequest("refresh_token");

        // Act
        var result = await _sut.Logout(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        _mockAuthService.Verify(x => x.LogoutAsync(request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
