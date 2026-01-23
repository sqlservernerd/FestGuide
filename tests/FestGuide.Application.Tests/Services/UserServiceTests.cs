using FluentAssertions;
using Moq;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IRefreshTokenRepository> _mockTokenRepo;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public UserServiceTests()
    {
        _mockUserRepo = new Mock<IUserRepository>();
        _mockTokenRepo = new Mock<IRefreshTokenRepository>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<UserService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new UserService(
            _mockUserRepo.Object,
            _mockTokenRepo.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WithValidUserId_ReturnsProfile()
    {
        // Arrange
        var userId = 1L;
        var user = new User
        {
            UserId = userId,
            Email = "test@example.com",
            EmailVerified = true,
            DisplayName = "Test User",
            UserType = UserType.Attendee,
            PreferredTimezoneId = "America/New_York",
            CreatedAtUtc = _now.AddDays(-30)
        };

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.GetProfileAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.DisplayName.Should().Be("Test User");
        result.UserType.Should().Be(UserType.Attendee);
        result.PreferredTimezoneId.Should().Be("America/New_York");
    }

    [Fact]
    public async Task GetProfileAsync_WithInvalidUserId_ThrowsUserNotFoundException()
    {
        // Arrange
        var userId = 2L;
        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _sut.GetProfileAsync(userId);

        // Assert
        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidRequest_UpdatesProfile()
    {
        // Arrange
        var userId = 3L;
        var user = new User
        {
            UserId = userId,
            Email = "test@example.com",
            DisplayName = "Old Name",
            UserType = UserType.Attendee,
            CreatedAtUtc = _now.AddDays(-30)
        };

        var request = new UpdateProfileRequest("New Name", "Europe/London");

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.UpdateProfileAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be("New Name");
        result.PreferredTimezoneId.Should().Be("Europe/London");

        _mockUserRepo.Verify(x => x.UpdateAsync(It.Is<User>(u => 
            u.DisplayName == "New Name" && 
            u.PreferredTimezoneId == "Europe/London" &&
            u.ModifiedAtUtc == _now), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAccountAsync_WithValidUserId_SoftDeletesUser()
    {
        // Arrange
        var userId = 4L;
        var user = new User { UserId = userId };

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.DeleteAccountAsync(userId);

        // Assert
        _mockTokenRepo.Verify(x => x.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUserRepo.Verify(x => x.DeleteAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExportDataAsync_WithValidUserId_ReturnsUserData()
    {
        // Arrange
        var userId = 5L;
        var user = new User
        {
            UserId = userId,
            Email = "test@example.com",
            EmailVerified = true,
            DisplayName = "Test User",
            UserType = UserType.Organizer,
            PreferredTimezoneId = "Asia/Tokyo",
            CreatedAtUtc = _now.AddDays(-60),
            ModifiedAtUtc = _now.AddDays(-5)
        };

        _mockUserRepo.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.ExportDataAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.EmailVerified.Should().BeTrue();
        result.DisplayName.Should().Be("Test User");
        result.UserType.Should().Be(UserType.Organizer);
        result.PreferredTimezoneId.Should().Be("Asia/Tokyo");
    }
}
