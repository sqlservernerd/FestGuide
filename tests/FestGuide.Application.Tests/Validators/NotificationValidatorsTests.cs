using FluentAssertions;
using FluentValidation.TestHelper;
using FestGuide.Application.Dtos;
using FestGuide.Application.Validators;

namespace FestGuide.Application.Tests.Validators;

public class RegisterDeviceRequestValidatorTests
{
    private readonly RegisterDeviceRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: "fcm-token-12345",
            Platform: "android",
            DeviceName: "Pixel 8");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyToken_ShouldHaveValidationError(string? token)
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: token!,
            Platform: "ios",
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Device token is required.");
    }

    [Fact]
    public void Validate_WithTokenExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var longToken = new string('a', 257); // 257 characters
        var request = new RegisterDeviceRequest(
            Token: longToken,
            Platform: "ios",
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Device token must not exceed 256 characters.");
    }

    [Fact]
    public void Validate_WithMaxLengthToken_ShouldNotHaveValidationError()
    {
        // Arrange
        var maxToken = new string('a', 256); // 256 characters (max allowed)
        var request = new RegisterDeviceRequest(
            Token: maxToken,
            Platform: "ios",
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Token);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyPlatform_ShouldHaveValidationError(string? platform)
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: "token",
            Platform: platform!,
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Platform)
            .WithErrorMessage("Platform is required.");
    }

    [Theory]
    [InlineData("ios")]
    [InlineData("android")]
    [InlineData("web")]
    [InlineData("IOS")]
    [InlineData("ANDROID")]
    [InlineData("WEB")]
    [InlineData("iOS")]
    [InlineData("Android")]
    public void Validate_WithValidPlatform_ShouldNotHaveValidationError(string platform)
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: "token",
            Platform: platform,
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Platform);
    }

    [Theory]
    [InlineData("windows")]
    [InlineData("linux")]
    [InlineData("macos")]
    [InlineData("invalid")]
    public void Validate_WithInvalidPlatform_ShouldHaveValidationError(string platform)
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: "token",
            Platform: platform,
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Platform)
            .WithErrorMessage("Platform must be one of (case-insensitive): ios, android, web.");
    }

    [Fact]
    public void Validate_WithDeviceNameExceedingMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        var longName = new string('a', 101); // 101 characters
        var request = new RegisterDeviceRequest(
            Token: "token",
            Platform: "ios",
            DeviceName: longName);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeviceName)
            .WithErrorMessage("Device name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithNullDeviceName_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new RegisterDeviceRequest(
            Token: "token",
            Platform: "ios",
            DeviceName: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DeviceName);
    }
}

public class UpdateNotificationPreferenceRequestValidatorTests
{
    private readonly UpdateNotificationPreferenceRequestValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange - Using same-day quiet hours to avoid midnight-spanning complexity
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: true,
            EmailEnabled: true,
            ScheduleChangesEnabled: true,
            RemindersEnabled: true,
            ReminderMinutesBefore: 30,
            AnnouncementsEnabled: true,
            QuietHoursStart: new TimeOnly(13, 0),
            QuietHoursEnd: new TimeOnly(15, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithReminderMinutesBelowMinimum_ShouldHaveValidationError(int minutes)
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: minutes,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReminderMinutesBefore)
            .WithErrorMessage("Reminder time must be between 5 and 120 minutes.");
    }

    [Theory]
    [InlineData(121)]
    [InlineData(200)]
    [InlineData(500)]
    public void Validate_WithReminderMinutesAboveMaximum_ShouldHaveValidationError(int minutes)
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: minutes,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReminderMinutesBefore)
            .WithErrorMessage("Reminder time must be between 5 and 120 minutes.");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(120)]
    public void Validate_WithValidReminderMinutes_ShouldNotHaveValidationError(int minutes)
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: minutes,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReminderMinutesBefore);
    }

    [Fact]
    public void Validate_WithNullReminderMinutes_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ReminderMinutesBefore);
    }

    [Fact]
    public void Validate_WithQuietHoursStartButNoEnd_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Both quiet hours start and end must be provided, or neither.");
    }

    [Fact]
    public void Validate_WithQuietHoursEndButNoStart_ShouldHaveValidationError()
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: new TimeOnly(8, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Both quiet hours start and end must be provided, or neither.");
    }

    [Fact]
    public void Validate_WithQuietHoursStartAfterEnd_ShouldNotHaveValidationError()
    {
        // Arrange - Midnight-spanning range where start is after end (allowed)
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(8, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithQuietHoursStartEqualToEnd_ShouldHaveValidationError()
    {
        // Arrange - Start and end are the same time (invalid)
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(22, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Quiet hours start must not equal quiet hours end.");
    }

    [Fact]
    public void Validate_WithValidQuietHoursSameDayRange_ShouldNotHaveValidationError()
    {
        // Arrange - Same day range (e.g., 13:00 to 15:00)
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: new TimeOnly(13, 0),
            QuietHoursEnd: new TimeOnly(15, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidQuietHoursNightRange_ShouldNotHaveValidationError()
    {
        // Arrange - Midnight-spanning range (e.g., 22:00 to 08:00)
        // This is now allowed and properly handled in IsInQuietHours logic
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: new TimeOnly(22, 0),
            QuietHoursEnd: new TimeOnly(8, 0),
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithBothQuietHoursNull_ShouldNotHaveValidationError()
    {
        // Arrange
        var request = new UpdateNotificationPreferenceRequest(
            PushEnabled: null,
            EmailEnabled: null,
            ScheduleChangesEnabled: null,
            RemindersEnabled: null,
            ReminderMinutesBefore: null,
            AnnouncementsEnabled: null,
            QuietHoursStart: null,
            QuietHoursEnd: null,
            TimeZoneId: null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
