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
        var userId = 1L;
        var request = new RegisterDeviceRequest(
            Token: "fcm-token-12345",
            Platform: "android",
            DeviceName: "Pixel 8");

        _mockDeviceTokenRepo.Setup(r => r.UpsertAsync(It.IsAny<DeviceToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(101L);

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
        var userId = 2L;
        var request = new RegisterDeviceRequest(Token: "token", Platform: "IOS", DeviceName: null);

        _mockDeviceTokenRepo.Setup(r => r.UpsertAsync(It.IsAny<DeviceToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(102L);

        // Act
        var result = await _sut.RegisterDeviceAsync(userId, request);

        // Assert
        result.Platform.Should().Be("ios");
    }

    [Fact]
    public async Task GetDevicesAsync_WithDevices_ReturnsDevices()
    {
        // Arrange
        var userId = 3L;
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
        var userId = 4L;

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
        var userId = 5L;
        var deviceId = 6L;
        var device = CreateTestDeviceToken(deviceId, userId);

        _mockDeviceTokenRepo.Setup(r => r.GetByIdAsync(deviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(device);

        // Act
        await _sut.UnregisterDeviceAsync(userId, deviceId);

        // Assert
        _mockDeviceTokenRepo.Verify(r => r.DeactivateAsync(deviceId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnregisterDeviceAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var ownerId = 7L;
        var differentUserId = 8L;
        var deviceId = 9L;
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
        var userId = 10L;
        var deviceId = 11L;

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
        var userId = 12L;
        var token = "fcm-token-12345";

        // Act
        await _sut.UnregisterDeviceByTokenAsync(userId, token);

        // Assert
        _mockDeviceTokenRepo.Verify(r => r.DeactivateByTokenAsync(token, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Notification Preferences Tests

    [Fact]
    public async Task GetPreferencesAsync_WithExistingPreferences_ReturnsPreferences()
    {
        // Arrange
        var userId = 13L;
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
        var userId = 14L;

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
        var userId = 15L;
        var existing = CreateTestPreference(userId);
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: false,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: 60,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _mockPreferenceRepo.Setup(r => r.UpsertAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(103L);

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
        var userId = 16L;
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: false,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockPreferenceRepo.Setup(r => r.UpsertAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(104L);

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
        var userId = 17L;
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
        var userId = 18L;

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
        var userId = 19L;
        var notificationId = 20L;
        var log = CreateTestNotificationLog(notificationId, userId);

        _mockNotificationLogRepo.Setup(r => r.GetByIdAsync(notificationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(log);

        // Act
        await _sut.MarkAsReadAsync(userId, notificationId);

        // Assert
        _mockNotificationLogRepo.Verify(r => r.MarkAsReadAsync(notificationId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkAsReadAsync_WithWrongUser_ThrowsForbiddenException()
    {
        // Arrange
        var ownerId = 21L;
        var differentUserId = 22L;
        var notificationId = 23L;
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
        var userId = 24L;
        var notificationId = 25L;

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
        var userId = 26L;

        // Act
        await _sut.MarkAllAsReadAsync(userId);

        // Assert
        _mockNotificationLogRepo.Verify(r => r.MarkAllAsReadAsync(userId, It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Send Notification Tests

    [Fact]
    public async Task SendToUserAsync_WithActiveDevices_SendsNotifications()
    {
        // Arrange
        var userId = 27L;
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
            .ReturnsAsync(105L);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

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
        var userId = 28L;
        var prefs = CreateTestPreference(userId);
        prefs.PushEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

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
        var userId = 29L;
        var prefs = CreateTestPreference(userId);
        prefs.ScheduleChangesEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

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
        var userId = 30L;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken>());

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

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
        var userIds = new[] { 100L, 101L };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken> { CreateTestDeviceToken() });
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(106L);

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
        var userId = 31L;
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
            .ReturnsAsync(107L);

        // Act - should not throw
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert - notification log should still be created with error
        _mockNotificationLogRepo.Verify(r => r.CreateAsync(
            It.Is<NotificationLog>(l => l.ErrorMessage != null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendScheduleChangeAsync_WithEngagementId_NotifiesUsersWithEngagement()
    {
        // Arrange
        var editionId = 32L;
        var engagementId = 33L;
        var userId1 = 102L;
        var userId2 = 103L;
        var change = new ScheduleChangeNotification(
            EditionId: editionId,
            ChangeType: "time_changed",
            EngagementId: engagementId,
            TimeSlotId: 104L,
            ArtistName: "Test Artist",
            StageName: "Main Stage",
            OldStartTime: _now.AddHours(2),
            NewStartTime: _now.AddHours(3),
            Message: "Performance time has changed");

        _mockPersonalScheduleRepo.Setup(r => r.GetUserIdsWithEngagementAsync(engagementId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<long> { userId1, userId2 });
        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken> { CreateTestDeviceToken() });
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(108L);

        // Act
        await _sut.SendScheduleChangeAsync(change);

        // Assert
        _mockPersonalScheduleRepo.Verify(r => r.GetUserIdsWithEngagementAsync(engagementId, It.IsAny<CancellationToken>()), Times.Once);
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendScheduleChangeAsync_WithoutEngagementId_NotifiesAllEditionUsers()
    {
        // Arrange
        var editionId = 34L;
        var userId1 = 105L;
        var userId2 = 106L;
        var change = new ScheduleChangeNotification(
            EditionId: editionId,
            ChangeType: "schedule_published",
            EngagementId: null,
            TimeSlotId: null,
            ArtistName: null,
            StageName: null,
            OldStartTime: null,
            NewStartTime: null,
            Message: "Schedule has been published");

        var schedules = new List<PersonalSchedule>
        {
            new() { PersonalScheduleId = 1L, UserId = userId1, EditionId = editionId },
            new() { PersonalScheduleId = 1L, UserId = userId2, EditionId = editionId }
        };

        _mockPersonalScheduleRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken> { CreateTestDeviceToken() });
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(109L);

        // Act
        await _sut.SendScheduleChangeAsync(change);

        // Assert
        _mockPersonalScheduleRepo.Verify(r => r.GetByEditionAsync(editionId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SendScheduleChangeAsync_WithNoUsers_DoesNotSendNotifications()
    {
        // Arrange
        var editionId = 35L;
        var change = new ScheduleChangeNotification(
            EditionId: editionId,
            ChangeType: "schedule_published",
            EngagementId: null,
            TimeSlotId: null,
            ArtistName: null,
            StageName: null,
            OldStartTime: null,
            NewStartTime: null,
            Message: "Schedule has been published");

        _mockPersonalScheduleRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalSchedule>());

        // Act
        await _sut.SendScheduleChangeAsync(change);

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithDuplicateUsers_SendsOncePerUser()
    {
        // Arrange
        var editionId = 36L;
        var userId = 37L;
        var change = new ScheduleChangeNotification(
            EditionId: editionId,
            ChangeType: "schedule_published",
            EngagementId: null,
            TimeSlotId: null,
            ArtistName: null,
            StageName: null,
            OldStartTime: null,
            NewStartTime: null,
            Message: "Schedule has been published");

        // User has multiple schedules for the same edition
        var schedules = new List<PersonalSchedule>
        {
            new() { PersonalScheduleId = 1L, UserId = userId, EditionId = editionId },
            new() { PersonalScheduleId = 1L, UserId = userId, EditionId = editionId }
        };

        _mockPersonalScheduleRepo.Setup(r => r.GetByEditionAsync(editionId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);
        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeviceToken> { CreateTestDeviceToken(userId: userId) });
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(110L);

        // Act
        await _sut.SendScheduleChangeAsync(change);

        // Assert - should only send once, not twice
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithRemindersDisabled_DoesNotSendReminder()
    {
        // Arrange
        var userId = 38L;
        var prefs = CreateTestPreference(userId);
        prefs.RemindersEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "reminder", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithRemindersEnabled_SendsReminder()
    {
        // Arrange
        var userId = 39L;
        var prefs = CreateTestPreference(userId);
        prefs.RemindersEnabled = true;
        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(111L);

        // Act
        await _sut.SendToUserAsync(1L, "reminder", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithAnnouncementsDisabled_DoesNotSendAnnouncement()
    {
        // Arrange
        var userId = 40L;
        var prefs = CreateTestPreference(userId);
        prefs.AnnouncementsEnabled = false;

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "announcement", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithAnnouncementsEnabled_SendsAnnouncement()
    {
        // Arrange
        var userId = 41L;
        var prefs = CreateTestPreference(userId);
        prefs.AnnouncementsEnabled = true;
        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(112L);

        // Act
        await _sut.SendToUserAsync(1L, "announcement", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_DuringQuietHours_DoesNotSend()
    {
        // Arrange
        var userId = 42L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 22:00 to 08:00, and current time is 23:00 (in quiet hours)
        prefs.QuietHoursStart = new TimeOnly(22, 0);
        prefs.QuietHoursEnd = new TimeOnly(8, 0);

        // Mock current time to be 23:00 UTC (11 PM)
        var quietTime = new DateTime(2026, 1, 20, 23, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(quietTime);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_OutsideQuietHours_Sends()
    {
        // Arrange
        var userId = 43L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 22:00 to 08:00, and current time is 12:00 (not in quiet hours)
        prefs.QuietHoursStart = new TimeOnly(22, 0);
        prefs.QuietHoursEnd = new TimeOnly(8, 0);

        // Mock current time to be 12:00 UTC (noon) - outside quiet hours
        var activeTime = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(activeTime);

        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(113L);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_DuringQuietHoursEarlyMorning_DoesNotSend()
    {
        // Arrange
        var userId = 44L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 22:00 to 08:00, and current time is 02:00 (in quiet hours)
        prefs.QuietHoursStart = new TimeOnly(22, 0);
        prefs.QuietHoursEnd = new TimeOnly(8, 0);

        // Mock current time to be 02:00 UTC (2 AM) - in quiet hours
        var quietTime = new DateTime(2026, 1, 20, 2, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(quietTime);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithSameDayQuietHours_Sends()
    {
        // Arrange
        var userId = 45L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 13:00 to 15:00 (same day), and current time is 12:00 (not in quiet hours)
        prefs.QuietHoursStart = new TimeOnly(13, 0);
        prefs.QuietHoursEnd = new TimeOnly(15, 0);

        // Mock current time to be 12:00 UTC (noon) - before quiet hours
        var activeTime = new DateTime(2026, 1, 20, 12, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(activeTime);

        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(114L);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithSameDayQuietHoursDuring_DoesNotSend()
    {
        // Arrange
        var userId = 46L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 13:00 to 15:00 (same day), and current time is 14:00 (in quiet hours)
        prefs.QuietHoursStart = new TimeOnly(13, 0);
        prefs.QuietHoursEnd = new TimeOnly(15, 0);

        // Mock current time to be 14:00 UTC (2 PM) - during quiet hours
        var quietTime = new DateTime(2026, 1, 20, 14, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(quietTime);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_AtQuietHoursStartBoundary_DoesNotSend()
    {
        // Arrange
        var userId = 47L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 22:00 to 08:00, and current time is exactly 22:00
        prefs.QuietHoursStart = new TimeOnly(22, 0);
        prefs.QuietHoursEnd = new TimeOnly(8, 0);

        // Mock current time to be exactly 22:00 UTC (quiet hours start boundary)
        var boundaryTime = new DateTime(2026, 1, 20, 22, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(boundaryTime);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert - Notifications should be blocked at the quiet hours start boundary
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_AtQuietHoursEndBoundary_Sends()
    {
        // Arrange
        var userId = 48L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 22:00 to 08:00, and current time is exactly 08:00
        prefs.QuietHoursStart = new TimeOnly(22, 0);
        prefs.QuietHoursEnd = new TimeOnly(8, 0);

        // Mock current time to be exactly 08:00 UTC (quiet hours end boundary - exclusive)
        var boundaryTime = new DateTime(2026, 1, 20, 8, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(boundaryTime);

        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(115L);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert - Notifications should be allowed at the quiet hours end boundary (exclusive)
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_AtSameDayQuietHoursStartBoundary_DoesNotSend()
    {
        // Arrange
        var userId = 49L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 13:00 to 15:00 (same day), current time is exactly 13:00
        prefs.QuietHoursStart = new TimeOnly(13, 0);
        prefs.QuietHoursEnd = new TimeOnly(15, 0);

        // Mock current time to be exactly 13:00 UTC (quiet hours start boundary)
        var boundaryTime = new DateTime(2026, 1, 20, 13, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(boundaryTime);

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert - Notifications should be blocked at the quiet hours start boundary
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_AtSameDayQuietHoursEndBoundary_Sends()
    {
        // Arrange
        var userId = 50L;
        var prefs = CreateTestPreference(userId);
        // Set quiet hours from 13:00 to 15:00 (same day), current time is exactly 15:00
        prefs.QuietHoursStart = new TimeOnly(13, 0);
        prefs.QuietHoursEnd = new TimeOnly(15, 0);

        // Mock current time to be exactly 15:00 UTC (quiet hours end boundary - exclusive)
        var boundaryTime = new DateTime(2026, 1, 20, 15, 0, 0, DateTimeKind.Utc);
        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(boundaryTime);

        var devices = new List<DeviceToken> { CreateTestDeviceToken(userId: userId) };

        _mockPreferenceRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(prefs);
        _mockDeviceTokenRepo.Setup(r => r.GetByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);
        _mockNotificationLogRepo.Setup(r => r.CreateAsync(It.IsAny<NotificationLog>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(116L);

        // Act
        await _sut.SendToUserAsync(1L, "schedule_change", "Title", "Body");

        // Assert - Notifications should be allowed at the quiet hours end boundary (exclusive)
        _mockPushProvider.Verify(p => p.SendAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<PushNotificationMessage>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private DeviceToken CreateTestDeviceToken(
        long? deviceTokenId = null,
        long? userId = null,
        string platform = "ios")
    {
        return new DeviceToken
        {
            DeviceTokenId = deviceTokenId ?? 0L,
            UserId = userId ?? 0L,
            Token = $"token-{107L}",
            Platform = platform,
            DeviceName = "Test Device",
            IsActive = true,
            LastUsedAtUtc = _now,
            CreatedAtUtc = _now,
            CreatedBy = userId ?? 0L,
            ModifiedAtUtc = _now,
            ModifiedBy = userId ?? 0L
        };
    }

    private NotificationPreference CreateTestPreference(long userId)
    {
        return new NotificationPreference
        {
            NotificationPreferenceId = 1L,
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
        long? notificationLogId = null,
        long? userId = null,
        string title = "Test Notification")
    {
        return new NotificationLog
        {
            NotificationLogId = notificationLogId ?? 0L,
            UserId = userId ?? 0L,
            NotificationType = "schedule_change",
            Title = title,
            Body = "Test body",
            SentAtUtc = _now,
            IsDelivered = true,
            CreatedAtUtc = _now,
            CreatedBy = 0L,
            ModifiedAtUtc = _now,
            ModifiedBy = 0L
        };
    }

    #endregion
}
