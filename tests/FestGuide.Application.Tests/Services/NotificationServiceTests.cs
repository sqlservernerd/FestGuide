using FluentAssertions;
using Moq;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IDeviceTokenRepository> _mockDeviceTokenRepo;
    private readonly Mock<INotificationLogRepository> _mockNotificationLogRepo;
    private readonly Mock<INotificationPreferenceRepository> _mockPreferenceRepo;
    private readonly Mock<IPersonalScheduleRepository> _mockPersonalScheduleRepo;
    private readonly Mock<IPushNotificationProvider> _mockPushProvider;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<ILogger<NotificationService>> _mockLogger;
    private readonly NotificationService _sut;
    private readonly DateTime _now = new(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);

    public NotificationServiceTests()
    {
        _mockDeviceTokenRepo = new Mock<IDeviceTokenRepository>();
        _mockNotificationLogRepo = new Mock<INotificationLogRepository>();
        _mockPreferenceRepo = new Mock<INotificationPreferenceRepository>();
        _mockPersonalScheduleRepo = new Mock<IPersonalScheduleRepository>();
        _mockPushProvider = new Mock<IPushNotificationProvider>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockLogger = new Mock<ILogger<NotificationService>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_now);

        _sut = new NotificationService(
            _mockDeviceTokenRepo.Object,
            _mockNotificationLogRepo.Object,
            _mockPreferenceRepo.Object,
            _mockPersonalScheduleRepo.Object,
            _mockPushProvider.Object,
            _mockDateTimeProvider.Object,
            _mockLogger.Object);
    }

    #region Device Management Tests

    [Fact]
    public async Task RegisterDeviceAsync_WithValidRequest_RegistersDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RegisterDeviceRequest(
            Token: "fcm-token-12345",
            Platform: "android",
            DeviceName: "Pixel 8");

        _mockDeviceTokenRepo.Setup(r => r.UpsertAsync(It.IsAny<DeviceToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.RegisterDeviceAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.Platform.Should().Be("android");
        result.DeviceName.Should().Be("Pixel 8");
        result.IsActive.Should().BeTrue();

        _mockDeviceTokenRepo.Verify(r => r.UpsertAsync(
            It.Is<DeviceToken>(d => d.Token == request.Token && d.Platform == "android" && d.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterDeviceAsync_NormalizesPlatformToLowercase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new RegisterDeviceRequest(Token: "token", Platform: "IOS", DeviceName: null);

        _mockDeviceTokenRepo.Setup(r => r.UpsertAsync(It.IsAny<DeviceToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.RegisterDeviceAsync(userId, request);

        // Assert
        result.Platform.Should().Be("ios");
    }

    [Fact]
    public async Task GetDevicesAsync_WithDevices_ReturnsDevices()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var devices = new List<DeviceToken>
        {
            CreateTestDeviceToken(userId: userId, platform: "ios"),
            CreateTestDeviceToken(userId: userId, platform: "android")
        };

        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        // Act
        var result = await _sut.GetDevicesAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetDevicesAsync_WithNoDevices_ReturnsEmptyList()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken>());

        // Act
        var result = await _sut.GetDevicesAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UnregisterDeviceAsync_WithValidDevice_DeactivatesDevice()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = CreateTestDeviceToken(deviceId, userId);

        _mockDeviceTokenRepo.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _sut.UnregisterDeviceAsync(userId, deviceId);

        // Assert
        _mockDeviceTokenRepo.Verify(r => r.DeactivateAsync(deviceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnregisterDeviceAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();
        var device = CreateTestDeviceToken(deviceId, ownerId);

        _mockDeviceTokenRepo.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        var act = () => _sut.UnregisterDeviceAsync(differentUserId, deviceId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UnregisterDeviceAsync_WithNonExistentDevice_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var deviceId = Guid.NewGuid();

        _mockDeviceTokenRepo.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DeviceToken?)null);

        // Act
        var act = () => _sut.UnregisterDeviceAsync(userId, deviceId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task UnregisterDeviceByTokenAsync_DeactivatesByToken()
    {
        // Arrange
        var token = "fcm-token-12345";

        // Act
        await _sut.UnregisterDeviceByTokenAsync(token);

        // Assert
        _mockDeviceTokenRepo.Verify(r => r.DeactivateByTokenAsync(token, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Notification Preferences Tests

    [Fact]
    public async Task GetPreferencesAsync_WithExistingPreferences_ReturnsPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = CreateTestPreference(userId);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        var result = await _sut.GetPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.PushEnabled.Should().Be(prefs.PushEnabled);
        result.ReminderMinutesBefore.Should().Be(prefs.ReminderMinutesBefore);
    }

    [Fact]
    public async Task GetPreferencesAsync_WithNoPreferences_ReturnsDefaults()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        // Act
        var result = await _sut.GetPreferencesAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.PushEnabled.Should().BeTrue();
        result.EmailEnabled.Should().BeTrue();
        result.ScheduleChangesEnabled.Should().BeTrue();
        result.RemindersEnabled.Should().BeTrue();
        result.ReminderMinutesBefore.Should().Be(30);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithExistingPreferences_UpdatesPreferences()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existing = CreateTestPreference(userId);
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: false,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: 60,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockPreferenceRepo.Setup(r => r.UpsertAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        result.PushEnabled.Should().BeFalse();
        result.ReminderMinutesBefore.Should().Be(60);

        _mockPreferenceRepo.Verify(r => r.UpsertAsync(
            It.Is<NotificationPreference>(p => !p.PushEnabled && p.ReminderMinutesBefore == 60),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePreferencesAsync_WithNoExistingPreferences_CreatesNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: false,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockPreferenceRepo.Setup(r => r.UpsertAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        var result = await _sut.UpdatePreferencesAsync(userId, request);

        // Assert
        result.Should().NotBeNull();
        _mockPreferenceRepo.Verify(r => r.UpsertAsync(
            It.Is<NotificationPreference>(p => p.UserId == userId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Notification History Tests

    [Fact]
    public async Task GetNotificationsAsync_WithNotifications_ReturnsNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<NotificationLog>
        {
            CreateTestNotificationLog(userId: userId, title: "Schedule Update"),
            CreateTestNotificationLog(userId: userId, title: "Reminder")
        };

        _mockNotificationLogRepo.Setup(r => r.GetByUserAsync(userId, 50, 0, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _sut.GetNotificationsAsync(userId);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCount()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockNotificationLogRepo.Setup(r => r.GetUnreadCountAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _sut.GetUnreadCountAsync(userId);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithValidNotification_MarksAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var log = CreateTestNotificationLog(notificationId, userId);

        _mockNotificationLogRepo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        await _sut.MarkAsReadAsync(userId, notificationId);

        // Assert
        _mockNotificationLogRepo.Verify(r => r.MarkAsReadAsync(notificationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var differentUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        var log = CreateTestNotificationLog(notificationId, ownerId);

        _mockNotificationLogRepo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        var act = () => _sut.MarkAsReadAsync(differentUserId, notificationId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAsReadAsync_WithNonExistentNotification_ThrowsForbiddenException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        _mockNotificationLogRepo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationLog?)null);

        // Act
        var act = () => _sut.MarkAsReadAsync(userId, notificationId);

        // Assert
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksAllAsRead()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.MarkAllAsReadAsync(userId);

        // Assert
        _mockNotificationLogRepo.Verify(r => r.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Send Notification Tests

    [Fact]
    public async Task SendToUserAsync_WithActiveDevices_SendsNotifications()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var devices = new List<DeviceToken>
        {
            CreateTestDeviceToken(userId: userId, platform: "ios"),
            CreateTestDeviceToken(userId: userId, platform: "android")
        };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.SendToUserAsync(userId, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));

        _mockNotificationLogRepo.Verify(r => r.CreateAsync(
            It.IsAny<NotificationLog>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendToUserAsync_WithPushDisabled_DoesNotSend()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = CreateTestPreference(userId);
        prefs.PushEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(userId, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithScheduleChangesDisabled_DoesNotSendScheduleChange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var prefs = CreateTestPreference(userId);
        prefs.ScheduleChangesEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(userId, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithNoDevices_DoesNotSend()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken>());

        // Act
        await _sut.SendToUserAsync(userId, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUsersAsync_SendsToAllUsers()
    {
        // Arrange
        var userIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken> { CreateTestDeviceToken() });
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.SendToUsersAsync(userIds, "announcement", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendToUserAsync_WhenPushFails_LogsErrorAndContinues()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var devices = new List<DeviceToken>
        {
            CreateTestDeviceToken(userId: userId)
        };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockPushProvider.Setup(p => p.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PushNotificationMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Push failed"));
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act - should not throw
        await _sut.SendToUserAsync(userId, "schedule_change", "Title", "Body");

        // Assert - notification log should still be created with error
        _mockNotificationLogRepo.Verify(r => r.CreateAsync(
            It.Is<NotificationLog>(l => l.ErrorMessage != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private DeviceToken CreateTestDeviceToken(
        Guid? deviceTokenId = null,
        Guid? userId = null,
        string platform = "ios")
    {
        return new DeviceToken
        {
            DeviceTokenId = deviceTokenId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Token = $"token-{Guid.NewGuid()}",
            Platform = platform,
            DeviceName = "Test Device",
            IsActive = true,
            LastUsedAtUtc = _now,
            CreatedAtUtc = _now,
            CreatedBy = userId ?? Guid.NewGuid(),
            ModifiedAtUtc = _now,
            ModifiedBy = userId ?? Guid.NewGuid()
        };
    }

    private NotificationPreference CreateTestPreference(Guid userId)
    {
        return new NotificationPreference
        {
            NotificationPreferenceId = Guid.NewGuid(),
            UserId = userId,
            PushEnabled = true,
            EmailEnabled = true,
            ScheduleChangesEnabled = true,
            RemindersEnabled = true,
            ReminderMinutesBefore = 30,
            AnnouncementsEnabled = true,
            CreatedAtUtc = _now,
            CreatedBy = userId,
            ModifiedAtUtc = _now,
            ModifiedBy = userId
        };
    }

    private NotificationLog CreateTestNotificationLog(
        Guid? notificationLogId = null,
        Guid? userId = null,
        string title = "Test Notification")
    {
        return new NotificationLog
        {
            NotificationLogId = notificationLogId ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            NotificationType = "schedule_change",
            Title = title,
            Body = "Test body",
            SentAtUtc = _now,
            IsDelivered = true,
            CreatedAtUtc = _now,
            CreatedBy = Guid.Empty,
            ModifiedAtUtc = _now,
            ModifiedBy = Guid.Empty
        };
    }

    #endregion
}
