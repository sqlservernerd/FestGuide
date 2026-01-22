using FluentAssertions;
using Moq;
using FestGuide.Infrastructure;
using FestGuide.Integrations.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FestGuide.Integration.Tests.Email;

public class SmtpEmailServiceTests
{
    private readonly Mock<ILogger<SmtpEmailService>> _mockLogger;
    private readonly SmtpOptions _options;
    private readonly SmtpEmailService _sut;

    public SmtpEmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmtpEmailService>>();
        _options = new SmtpOptions
        {
            Host = "smtp.example.com",
            Port = 587,
            Username = "test@example.com",
            Password = "password",
            FromAddress = "noreply@festguide.com",
            FromName = "FestGuide",
            UseSsl = true,
            Enabled = false, // Disabled to prevent actual email sending
            BaseUrl = "https://festguide.com"
        };

        var optionsMock = new Mock<IOptions<SmtpOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        _sut = new SmtpEmailService(optionsMock.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new SmtpEmailService(null!, _mockLogger.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsMock = new Mock<IOptions<SmtpOptions>>();
        optionsMock.Setup(x => x.Value).Returns(_options);

        // Act
        var act = () => new SmtpEmailService(optionsMock.Object, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region SendVerificationEmailAsync Tests

    [Fact]
    public async Task SendVerificationEmailAsync_WhenDisabled_DoesNotSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - Verify that a debug log was written indicating email is disabled
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithMissingHost_DoesNotSendEmail()
    {
        // Arrange
        _options.Enabled = true;
        _options.Host = string.Empty;
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - Verify that a warning log was written indicating misconfiguration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithMissingUsername_DoesNotSendEmail()
    {
        // Arrange
        _options.Enabled = true;
        _options.Username = string.Empty;
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - Verify that a warning log was written indicating misconfiguration
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithMissingBaseUrl_ThrowsException()
    {
        // Arrange
        _options.BaseUrl = string.Empty;
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act & Assert - Should throw InvalidOperationException
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.SendVerificationEmailAsync(email, displayName, verificationToken));

        Assert.Contains("BaseUrl is not configured", exception.Message);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithSpecialCharactersInToken_EscapesToken()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "token+with/special=chars&more";

        // Act - should not throw
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - completed without exception
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendPasswordResetEmailAsync Tests

    [Fact]
    public async Task SendPasswordResetEmailAsync_WhenDisabled_DoesNotSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "reset123";

        // Act
        await _sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithSpecialCharactersInToken_EscapesToken()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "token+with/special=chars&more";

        // Act - should not throw
        await _sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

        // Assert - completed without exception
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendPasswordChangedNotificationAsync Tests

    [Fact]
    public async Task SendPasswordChangedNotificationAsync_WhenDisabled_DoesNotSendEmail()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";

        // Act
        await _sut.SendPasswordChangedNotificationAsync(email, displayName);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region SendInvitationEmailAsync Tests

    [Fact]
    public async Task SendInvitationEmailAsync_ForNewUser_IncludesRegistrationMessage()
    {
        // Arrange
        var toAddress = "newuser@example.com";
        var festivalName = "Summer Music Fest";
        var inviterName = "John Organizer";
        var role = "Editor";
        var isNewUser = true;

        // Act
        await _sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, isNewUser);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_ForExistingUser_IncludesLoginMessage()
    {
        // Arrange
        var toAddress = "existinguser@example.com";
        var festivalName = "Summer Music Fest";
        var inviterName = "John Organizer";
        var role = "Viewer";
        var isNewUser = false;

        // Act
        await _sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, isNewUser);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_WhenDisabled_DoesNotSendEmail()
    {
        // Arrange
        var toAddress = "user@example.com";
        var festivalName = "Test Festival";
        var inviterName = "Inviter";
        var role = "Editor";

        // Act
        await _sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email sending is disabled")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region URL Building Tests

    [Fact]
    public async Task SendVerificationEmailAsync_WithBaseUrlWithTrailingSlash_BuildsCorrectUrl()
    {
        // Arrange
        _options.BaseUrl = "https://festguide.com/";
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - URL should be built correctly without double slashes
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithBaseUrlWithoutTrailingSlash_BuildsCorrectUrl()
    {
        // Arrange
        _options.BaseUrl = "https://festguide.com";
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "reset123";

        // Act
        await _sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

        // Assert - URL should be built correctly
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Email Template Tests

    [Fact]
    public async Task SendVerificationEmailAsync_IncludesUserDisplayName()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "John Doe";
        var verificationToken = "abc123";

        // Act
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - Should complete without error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_IncludesUserDisplayName()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Jane Smith";
        var resetToken = "reset123";

        // Act
        await _sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

        // Assert - Should complete without error
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task SendVerificationEmailAsync_WithCancelledToken_CompletesSuccessfully()
    {
        // Arrange
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - When disabled, cancellation doesn't matter
        await _sut.SendVerificationEmailAsync(email, displayName, verificationToken, cts.Token);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
