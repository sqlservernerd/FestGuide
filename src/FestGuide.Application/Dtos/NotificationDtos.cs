using FestGuide.Domain.Entities;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Request DTO for registering a device for push notifications.
/// </summary>
public sealed record RegisterDeviceRequest(
    string Token,
    string Platform,
    string? DeviceName);

/// <summary>
/// Response DTO for device token.
/// </summary>
public sealed record DeviceTokenDto(
    long DeviceTokenId,
    string Platform,
    string? DeviceName,
    bool IsActive,
    DateTime? LastUsedAtUtc,
    DateTime CreatedAtUtc)
{
    public static DeviceTokenDto FromEntity(DeviceToken device) =>
        new(
            device.DeviceTokenId,
            device.Platform,
            device.DeviceName,
            device.IsActive,
            device.LastUsedAtUtc,
            device.CreatedAtUtc);
}

/// <summary>
/// Response DTO for notification log.
/// </summary>
public sealed record NotificationDto(
    long NotificationId,
    string NotificationType,
    string Title,
    string Body,
    string? RelatedEntityType,
    long? RelatedEntityId,
    DateTime SentAtUtc,
    bool IsRead)
{
    public static NotificationDto FromEntity(NotificationLog log) =>
        new(
            log.NotificationLogId,
            log.NotificationType,
            log.Title,
            log.Body,
            log.RelatedEntityType,
            log.RelatedEntityId,
            log.SentAtUtc,
            log.ReadAtUtc.HasValue);
}

/// <summary>
/// Response DTO for notification preferences.
/// </summary>
public sealed record NotificationPreferenceDto(
    bool PushEnabled,
    bool EmailEnabled,
    bool ScheduleChangesEnabled,
    bool RemindersEnabled,
    int ReminderMinutesBefore,
    bool AnnouncementsEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string TimeZoneId)
{
    public static NotificationPreferenceDto FromEntity(NotificationPreference pref) =>
        new(
            pref.PushEnabled,
            pref.EmailEnabled,
            pref.ScheduleChangesEnabled,
            pref.RemindersEnabled,
            pref.ReminderMinutesBefore,
            pref.AnnouncementsEnabled,
            pref.QuietHoursStart,
            pref.QuietHoursEnd,
            pref.TimeZoneId);

    public static NotificationPreferenceDto Default() =>
        new(true, true, true, true, 30, true, null, null, "UTC");
}

/// <summary>
/// Request DTO for updating notification preferences.
/// </summary>
public sealed record UpdateNotificationPreferenceRequest(
    bool? PushEnabled,
    bool? EmailEnabled,
    bool? ScheduleChangesEnabled,
    bool? RemindersEnabled,
    int? ReminderMinutesBefore,
    bool? AnnouncementsEnabled,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string? TimeZoneId);

/// <summary>
/// Response DTO for unread notification count.
/// </summary>
public sealed record UnreadCountDto(int Count);

/// <summary>
/// DTO for sending a push notification (internal use).
/// </summary>
public sealed record PushNotificationMessage(
    string Title,
    string Body,
    string? NotificationType,
    Dictionary<string, string>? Data);

/// <summary>
/// DTO for schedule change notification payload.
/// </summary>
public sealed record ScheduleChangeNotification(
    long EditionId,
    string ChangeType,
    long? EngagementId,
    long? TimeSlotId,
    string? ArtistName,
    string? StageName,
    DateTime? OldStartTime,
    DateTime? NewStartTime,
    string Message);
