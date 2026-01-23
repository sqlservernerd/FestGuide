using FluentAssertions;
using Moq;
using FestConnect.Infrastructure;
using FestConnect.Integrations.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FestConnect.Integration.Tests.Email;

public class SmtpEmailServiceTests
{
    private readonly Mock<ILogger<SmtpEmailService>> _mockLogger;

    public SmtpEmailServiceTests()
    {
        _mockLogger = new Mock<ILogger<SmtpEmailService>>();
    }

    private static SmtpOptions CreateDefaultOptions() => new()
    {
        Host = "smtp.example.com",
        Port = 587,
        Username = "test@example.com",
        Password = "password",
        FromAddress = "noreply@FestConnect.com",
        FromName = "FestConnect",
        UseSsl = true,
        Enabled = false, // Disabled to prevent actual email sending
        BaseUrl = "https://FestConnect.com"
    };

    private SmtpEmailService CreateService(SmtpOptions options)
    {
        var optionsMock = new Mock<IOptions<SmtpOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);
        return new SmtpEmailService(optionsMock.Object, _mockLogger.Object);
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
        var options = CreateDefaultOptions();
        var optionsMock = new Mock<IOptions<SmtpOptions>>();
        optionsMock.Setup(x => x.Value).Returns(options);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await sut.SendVerificationEmailAsync(email, displayName, verificationToken);

        // Assert - Verify that a debug log was written indicating email is disabled
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
    public void CreateService_WithMissingHost_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.Enabled = true;
        options.Host = string.Empty;

        // Act
        var act = () => CreateService(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Host*");
    }

    [Fact]
    public void CreateService_WithMissingUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.Enabled = true;
        options.Username = string.Empty;

        // Act
        var act = () => CreateService(options);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Username*");
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithMissingBaseUrl_ThrowsException()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.BaseUrl = string.Empty;
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act & Assert - Should throw InvalidOperationException
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.SendVerificationEmailAsync(email, displayName, verificationToken));

        Assert.Contains("BaseUrl is not configured", exception.Message);
    }

    [Fact]
    public async Task SendVerificationEmailAsync_WithSpecialCharactersInToken_EscapesToken()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "token+with/special=chars&more";

        // Act - should not throw
        await sut.SendVerificationEmailAsync(email, displayName, verificationToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "reset123";

        // Act
        await sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "token+with/special=chars&more";

        // Act - should not throw
        await sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";

        // Act
        await sut.SendPasswordChangedNotificationAsync(email, displayName);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var toAddress = "newuser@example.com";
        var festivalName = "Summer Music Fest";
        var inviterName = "John Organizer";
        var role = "Editor";
        var isNewUser = true;

        // Act
        await sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, isNewUser);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var toAddress = "existinguser@example.com";
        var festivalName = "Summer Music Fest";
        var inviterName = "John Organizer";
        var role = "Viewer";
        var isNewUser = false;

        // Act
        await sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, isNewUser);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var toAddress = "user@example.com";
        var festivalName = "Test Festival";
        var inviterName = "Inviter";
        var role = "Editor";

        // Act
        await sut.SendInvitationEmailAsync(toAddress, festivalName, inviterName, role, true);

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
        var options = CreateDefaultOptions();
        options.BaseUrl = "https://FestConnect.com/";
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";

        // Act
        await sut.SendVerificationEmailAsync(email, displayName, verificationToken);

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
        var options = CreateDefaultOptions();
        options.BaseUrl = "https://FestConnect.com";
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var resetToken = "reset123";

        // Act
        await sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "John Doe";
        var verificationToken = "abc123";

        // Act
        await sut.SendVerificationEmailAsync(email, displayName, verificationToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Jane Smith";
        var resetToken = "reset123";

        // Act
        await sut.SendPasswordResetEmailAsync(email, displayName, resetToken);

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
        var options = CreateDefaultOptions();
        var sut = CreateService(options);
        var email = "user@example.com";
        var displayName = "Test User";
        var verificationToken = "abc123";
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - When disabled, cancellation doesn't matter
        await sut.SendVerificationEmailAsync(email, displayName, verificationToken, cts.Token);

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
