using FluentAssertions;
using Moq;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using FestGuide.Infrastructure.Email;
using FestGuide.Security;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRefreshTokenRepository> _mockTokenRepo;
    private readonly Mock<IEmailVerificationTokenRepository> _mockEmailTokenRepo;
    private readonly Mock<IPasswordResetTokenRepository> _mockPasswordTokenRepo;
    private readonly Mock<IPasswordHasher> _mockPasswordHasher;
    private readonly Mock<IJwtTokenService> _mockJwtService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<AuthenticationService>> _mockLogger;
    private readonly AuthenticationService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public AuthenticationServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockEmailTokenRepo = new Mock<IEmailVerificationTokenRepository>();
        _mockPasswordTokenRepo = new Mock<IPasswordResetTokenRepository>();
        _mockPasswordHasher = new Mock<IPasswordHasher>();
        _mockJwtService = new Mock<IJwtTokenService>();
        _mockEmailService = new Mock<IEmailService>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<AuthenticationService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new AuthenticationService(
            _mockUserRepo.Object,
            _mockTokenRepo.Object,
            _mockEmailTokenRepo.Object,
            _mockPasswordTokenRepo.Object,
            _mockPasswordHasher.Object,
            _mockJwtService.Object,
            _mockEmailService.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserAndReturnsTokens()
    {
        // Arrange
        var request = new RegisterRequest("test@example.com", "SecurePassword123!", "Test User", UserType.Attendee);
        
        _mockUserRepo.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockPasswordHasher.Setup(x => x.HashPassword(request.Password))
            .Returns("hashed_password");
        _mockJwtService.Setup(x => x.GenerateAccessToken(It.IsAny<long>(), request.Email, "Attendee"))
            .Returns("access_token");
        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");
        _mockJwtService.Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("hashed_refresh_token");
        _mockJwtService.Setup(x => x.GetAccessTokenExpiration())
            .Returns(_now.AddMinutes(15));
        _mockJwtService.Setup(x => x.GetRefreshTokenExpiration())
            .Returns(_now.AddDays(7));

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(request.Email);
        result.DisplayName.Should().Be(request.DisplayName);
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");

        _mockUserRepo.Verify(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockTokenRepo.Verify(x => x.CreateAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsDuplicateException()
    {
        // Arrange
        var request = new RegisterRequest("existing@example.com", "SecurePassword123!", "Test User", UserType.Attendee);
        
        _mockUserRepo.Setup(x => x.ExistsByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<DuplicateException>()
            .WithMessage("*email*existing@example.com*");
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokens()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "SecurePassword123!");
        var user = new User
        {
            UserId = 1L,
            Email = "test@example.com",
            EmailNormalized = "test@example.com",
            PasswordHash = "hashed_password",
            DisplayName = "Test User",
            UserType = UserType.Attendee
        };

        _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(true);
        _mockJwtService.Setup(x => x.GenerateAccessToken(user.UserId, user.Email, "Attendee"))
            .Returns("access_token");
        _mockJwtService.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh_token");
        _mockJwtService.Setup(x => x.HashRefreshToken("refresh_token"))
            .Returns("hashed_refresh_token");
        _mockJwtService.Setup(x => x.GetAccessTokenExpiration())
            .Returns(_now.AddMinutes(15));
        _mockJwtService.Setup(x => x.GetRefreshTokenExpiration())
            .Returns(_now.AddDays(7));

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.UserId);
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "WrongPassword");
        var user = new User
        {
            UserId = 1L,
            Email = "test@example.com",
            PasswordHash = "hashed_password"
        };

        _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPasswordHasher.Setup(x => x.VerifyPassword(request.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithNonExistentUser_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@example.com", "Password123!");

        _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthenticationException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithLockedAccount_ThrowsAuthenticationException()
    {
        // Arrange
        var request = new LoginRequest("test@example.com", "Password123!");
        var user = new User
        {
            UserId = 1L,
            Email = "test@example.com",
            LockoutEndUtc = _now.AddMinutes(10)
        };

        _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _sut.LoginAsync(request);

        // Assert
                await act.Should().ThrowAsync<AuthenticationException>()
                    .WithMessage("*locked*");
            }

            [Fact]
            public async Task VerifyEmailAsync_WithValidToken_VerifiesUserEmail()
            {
                // Arrange
                var token = "valid_token";
                var tokenHash = "hashed_token";
                var userId = 1L;
                var request = new VerifyEmailRequest(token);

                var storedToken = new EmailVerificationToken
                {
                    TokenId = 1L,
                    UserId = userId,
                    TokenHash = tokenHash,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(24), // Use actual future time for IsValid check
                    IsUsed = false
                };

                var user = new User
                {
                    UserId = userId,
                    Email = "test@example.com",
                    EmailVerified = false,
                    DisplayName = "Test User"
                };

                _mockJwtService.Setup(x => x.HashRefreshToken(token)).Returns(tokenHash);
                _mockEmailTokenRepo.Setup(x => x.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storedToken);
                _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);

                // Act
                var result = await _sut.VerifyEmailAsync(request);

                // Assert
                result.Should().NotBeNull();
                result.Message.Should().Contain("verified");
                _mockUserRepo.Verify(x => x.UpdateAsync(It.Is<User>(u => u.EmailVerified), It.IsAny<CancellationToken>()), Times.Once);
                _mockEmailTokenRepo.Verify(x => x.MarkAsUsedAsync(storedToken.TokenId, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task VerifyEmailAsync_WithInvalidToken_ThrowsAuthenticationException()
            {
                // Arrange
                var request = new VerifyEmailRequest("invalid_token");

                _mockJwtService.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns("hashed");
                _mockEmailTokenRepo.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((EmailVerificationToken?)null);

                // Act
                var act = async () => await _sut.VerifyEmailAsync(request);

                // Assert
                await act.Should().ThrowAsync<AuthenticationException>()
                    .WithMessage("*verification token*");
            }

            [Fact]
            public async Task ForgotPasswordAsync_WithExistingUser_SendsResetEmail()
            {
                // Arrange
                var request = new ForgotPasswordRequest("test@example.com");
                var user = new User
                {
                    UserId = 1L,
                    Email = "test@example.com",
                    DisplayName = "Test User"
                };

                _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);
                _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("reset_token");
                _mockJwtService.Setup(x => x.HashRefreshToken("reset_token")).Returns("hashed_reset_token");

                // Act
                var result = await _sut.ForgotPasswordAsync(request);

                // Assert
                result.Should().NotBeNull();
                result.Message.Should().Contain("reset link");
                _mockPasswordTokenRepo.Verify(x => x.CreateAsync(It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
                _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(user.Email, user.DisplayName, "reset_token", It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task ForgotPasswordAsync_WithNonExistentUser_ReturnsSuccessWithoutSendingEmail()
            {
                // Arrange
                var request = new ForgotPasswordRequest("nonexistent@example.com");

                _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync((User?)null);

                // Act
                var result = await _sut.ForgotPasswordAsync(request);

                // Assert
                result.Should().NotBeNull();
                result.Message.Should().Contain("reset link");
                _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            }

            [Fact]
            public async Task ResetPasswordAsync_WithValidToken_ResetsPassword()
            {
                // Arrange
                var token = "valid_token";
                var tokenHash = "hashed_token";
                var userId = 2L;
                var request = new ResetPasswordRequest(token, "NewSecurePassword123!");

                var storedToken = new PasswordResetToken
                {
                    TokenId = 1L,
                    UserId = userId,
                    TokenHash = tokenHash,
                    ExpiresAtUtc = DateTime.UtcNow.AddHours(1), // Use actual future time for IsValid check
                    IsUsed = false
                };

                var user = new User
                {
                    UserId = userId,
                    Email = "test@example.com",
                    DisplayName = "Test User",
                    PasswordHash = "old_hash"
                };

                _mockJwtService.Setup(x => x.HashRefreshToken(token)).Returns(tokenHash);
                _mockPasswordTokenRepo.Setup(x => x.GetByTokenHashAsync(tokenHash, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(storedToken);
                _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);
                _mockPasswordHasher.Setup(x => x.HashPassword(request.NewPassword)).Returns("new_hashed_password");

                // Act
                var result = await _sut.ResetPasswordAsync(request);

                // Assert
                result.Should().NotBeNull();
                result.Message.Should().Contain("reset successfully");
                _mockUserRepo.Verify(x => x.UpdateAsync(It.Is<User>(u => u.PasswordHash == "new_hashed_password"), It.IsAny<CancellationToken>()), Times.Once);
                _mockPasswordTokenRepo.Verify(x => x.MarkAsUsedAsync(storedToken.TokenId, It.IsAny<CancellationToken>()), Times.Once);
                _mockTokenRepo.Verify(x => x.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
                _mockEmailService.Verify(x => x.SendPasswordChangedNotificationAsync(user.Email, user.DisplayName, It.IsAny<CancellationToken>()), Times.Once);
            }

            [Fact]
            public async Task ResetPasswordAsync_WithInvalidToken_ThrowsAuthenticationException()
            {
                // Arrange
                var request = new ResetPasswordRequest("invalid_token", "NewPassword123!");

                _mockJwtService.Setup(x => x.HashRefreshToken(It.IsAny<string>())).Returns("hashed");
                _mockPasswordTokenRepo.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((PasswordResetToken?)null);

                // Act
                var act = async () => await _sut.ResetPasswordAsync(request);

                // Assert
                await act.Should().ThrowAsync<AuthenticationException>()
                    .WithMessage("*password reset token*");
            }

            [Fact]
            public async Task ResendVerificationEmailAsync_WithUnverifiedUser_SendsEmail()
            {
                // Arrange
                var request = new ResendVerificationRequest("test@example.com");
                var user = new User
                {
                    UserId = 1L,
                    Email = "test@example.com",
                    EmailVerified = false,
                    DisplayName = "Test User"
                };

                _mockUserRepo.Setup(x => x.GetByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(user);
                _mockJwtService.Setup(x => x.GenerateRefreshToken()).Returns("verification_token");
                _mockJwtService.Setup(x => x.HashRefreshToken("verification_token")).Returns("hashed_token");

                // Act
                var result = await _sut.ResendVerificationEmailAsync(request);

                // Assert
                        result.Should().NotBeNull();
                        _mockEmailTokenRepo.Verify(x => x.InvalidateAllForUserAsync(user.UserId, It.IsAny<CancellationToken>()), Times.Once);
                        _mockEmailService.Verify(x => x.SendVerificationEmailAsync(user.Email, user.DisplayName, "verification_token", It.IsAny<CancellationToken>()), Times.Once);
                    }
                }
