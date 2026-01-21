using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Notification service implementation.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IDeviceTokenRepository _deviceTokenRepository;
    private readonly INotificationLogRepository _notificationLogRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly IPersonalScheduleRepository _personalScheduleRepository;
    private readonly IPushNotificationProvider _pushProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IDeviceTokenRepository deviceTokenRepository,
        INotificationLogRepository notificationLogRepository,
        INotificationPreferenceRepository preferenceRepository,
        IPersonalScheduleRepository personalScheduleRepository,
        IPushNotificationProvider pushProvider,
        IDateTimeProvider dateTimeProvider,
        ILogger<NotificationService> logger)
    {
        _deviceTokenRepository = deviceTokenRepository ?? throw new ArgumentNullException(nameof(deviceTokenRepository));
        _notificationLogRepository = notificationLogRepository ?? throw new ArgumentNullException(nameof(notificationLogRepository));
        _preferenceRepository = preferenceRepository ?? throw new ArgumentNullException(nameof(preferenceRepository));
        _personalScheduleRepository = personalScheduleRepository ?? throw new ArgumentNullException(nameof(personalScheduleRepository));
        _pushProvider = pushProvider ?? throw new ArgumentNullException(nameof(pushProvider));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Device Management

    /// <inheritdoc />
    public async Task<DeviceTokenDto> RegisterDeviceAsync(Guid userId, RegisterDeviceRequest request, CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var deviceToken = new DeviceToken
        {
            DeviceTokenId = Guid.NewGuid(),
            UserId = userId,
            Token = request.Token,
            Platform = request.Platform.ToLowerInvariant(),
            DeviceName = request.DeviceName,
            IsActive = true,
            LastUsedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _deviceTokenRepository.UpsertAsync(deviceToken, ct);

        _logger.LogInformation("Device registered for user {UserId} on platform {Platform}", userId, request.Platform);

        return DeviceTokenDto.FromEntity(deviceToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceTokenDto>> GetDevicesAsync(Guid userId, CancellationToken ct = default)
    {
        var devices = await _deviceTokenRepository.GetByUserAsync(userId, ct);
        return devices.Select(DeviceTokenDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task UnregisterDeviceAsync(Guid userId, Guid deviceTokenId, CancellationToken ct = default)
    {
        var device = await _deviceTokenRepository.GetByIdAsync(deviceTokenId, ct);
        if (device == null || device.UserId != userId)
        {
            throw new ForbiddenException("Device not found or does not belong to user.");
        }

        await _deviceTokenRepository.DeactivateAsync(deviceTokenId, ct);

        _logger.LogInformation("Device {DeviceTokenId} unregistered for user {UserId}", deviceTokenId, userId);
    }

    /// <inheritdoc />
    public async Task UnregisterDeviceByTokenAsync(string token, CancellationToken ct = default)
    {
        await _deviceTokenRepository.DeactivateByTokenAsync(token, ct);
    }

    #endregion

    #region Notification Preferences

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var prefs = await _preferenceRepository.GetByUserAsync(userId, ct);
        if (prefs == null)
        {
            return NotificationPreferenceDto.Default();
        }

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var existing = await _preferenceRepository.GetByUserAsync(userId, ct);
        var now = _dateTimeProvider.UtcNow;

        var prefs = existing ?? new NotificationPreference
        {
            NotificationPreferenceId = Guid.NewGuid(),
            UserId = userId,
            CreatedAtUtc = now,
            CreatedBy = userId
        };

        if (request.PushEnabled.HasValue) prefs.PushEnabled = request.PushEnabled.Value;
        if (request.EmailEnabled.HasValue) prefs.EmailEnabled = request.EmailEnabled.Value;
        if (request.ScheduleChangesEnabled.HasValue) prefs.ScheduleChangesEnabled = request.ScheduleChangesEnabled.Value;
        if (request.RemindersEnabled.HasValue) prefs.RemindersEnabled = request.RemindersEnabled.Value;
        if (request.ReminderMinutesBefore.HasValue) prefs.ReminderMinutesBefore = request.ReminderMinutesBefore.Value;
        if (request.AnnouncementsEnabled.HasValue) prefs.AnnouncementsEnabled = request.AnnouncementsEnabled.Value;
        if (request.QuietHoursStart.HasValue) prefs.QuietHoursStart = request.QuietHoursStart.Value;
        if (request.QuietHoursEnd.HasValue) prefs.QuietHoursEnd = request.QuietHoursEnd.Value;

        prefs.ModifiedAtUtc = now;
        prefs.ModifiedBy = userId;

        await _preferenceRepository.UpsertAsync(prefs, ct);

        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    #endregion

    #region Notification History

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var logs = await _notificationLogRepository.GetByUserAsync(userId, limit, offset, ct);
        return logs.Select(NotificationDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _notificationLogRepository.GetUnreadCountAsync(userId, ct);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var log = await _notificationLogRepository.GetByIdAsync(notificationId, ct);
        if (log == null || log.UserId != userId)
        {
            throw new ForbiddenException("Notification not found or does not belong to user.");
        }

        await _notificationLogRepository.MarkAsReadAsync(notificationId, ct);
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _notificationLogRepository.MarkAllAsReadAsync(userId, ct);
    }

    #endregion

    #region Sending Notifications

    /// <inheritdoc />
    public async Task SendToUserAsync(
        Guid userId,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        // Check user preferences
        var prefs = await _preferenceRepository.GetByUserAsync(userId, ct);
        if (prefs != null && !prefs.PushEnabled)
        {
            _logger.LogDebug("Push notifications disabled for user {UserId}, skipping", userId);
            return;
        }

        // Check notification type preference
        if (!ShouldSendNotificationType(prefs, notificationType))
        {
            _logger.LogDebug("Notification type {Type} disabled for user {UserId}, skipping", notificationType, userId);
            return;
        }

        var devices = await _deviceTokenRepository.GetByUserAsync(userId, ct);
        if (!devices.Any())
        {
            _logger.LogDebug("No registered devices for user {UserId}", userId);
            return;
        }

        var now = _dateTimeProvider.UtcNow;

        foreach (var device in devices)
        {
            var log = new NotificationLog
            {
                NotificationLogId = Guid.NewGuid(),
                UserId = userId,
                DeviceTokenId = device.DeviceTokenId,
                NotificationType = notificationType,
                Title = title,
                Body = body,
                DataPayload = data != null ? System.Text.Json.JsonSerializer.Serialize(data) : null,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                SentAtUtc = now,
                IsDelivered = false,
                CreatedAtUtc = now,
                CreatedBy = Guid.Empty,
                ModifiedAtUtc = now,
                ModifiedBy = Guid.Empty
            };

            try
            {
                var message = new PushNotificationMessage(title, body, notificationType, data);
                await _pushProvider.SendAsync(device.Token, device.Platform, message, ct);

                log.IsDelivered = true;
                await _deviceTokenRepository.UpdateLastUsedAsync(device.DeviceTokenId, now, ct);

                _logger.LogInformation("Notification sent to user {UserId} on device {DeviceId}", userId, device.DeviceTokenId);
            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to send notification to device {DeviceId}", device.DeviceTokenId);

                // Deactivate invalid tokens
                if (ex.Message.Contains("invalid") || ex.Message.Contains("unregistered"))
                {
                    await _deviceTokenRepository.DeactivateAsync(device.DeviceTokenId, ct);
                }
            }

            await _notificationLogRepository.CreateAsync(log, ct);
        }
    }

    /// <inheritdoc />
    public async Task SendToUsersAsync(
        IEnumerable<Guid> userIds,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        foreach (var userId in userIds)
        {
            await SendToUserAsync(userId, notificationType, title, body, relatedEntityType, relatedEntityId, data, ct);
        }
    }

    /// <inheritdoc />
    public async Task SendScheduleChangeAsync(ScheduleChangeNotification change, CancellationToken ct = default)
    {
        // Find all users who have saved entries for this edition
        var schedules = await GetSchedulesForEditionAsync(change.EditionId, ct);
        var userIds = schedules.Select(s => s.UserId).Distinct();

        var data = new Dictionary<string, string>
        {
            ["editionId"] = change.EditionId.ToString(),
            ["changeType"] = change.ChangeType
        };

        if (change.EngagementId.HasValue) data["engagementId"] = change.EngagementId.Value.ToString();
        if (change.TimeSlotId.HasValue) data["timeSlotId"] = change.TimeSlotId.Value.ToString();

        await SendToUsersAsync(
            userIds,
            "schedule_change",
            $"Schedule Update: {change.ArtistName ?? "Performance"}",
            change.Message,
            "Edition",
            change.EditionId,
            data,
            ct);

        _logger.LogInformation("Schedule change notification sent to {Count} users for edition {EditionId}",
            userIds.Count(), change.EditionId);
    }

    private async Task<IReadOnlyList<PersonalSchedule>> GetSchedulesForEditionAsync(Guid editionId, CancellationToken ct)
    {
        // Get all personal schedules for the edition (from all users)
        // This would need a repository method - for now return empty
        // In a real implementation, we'd add a method to get schedules by edition
        return await Task.FromResult<IReadOnlyList<PersonalSchedule>>(new List<PersonalSchedule>());
    }

    private static bool ShouldSendNotificationType(NotificationPreference? prefs, string notificationType)
    {
        if (prefs == null) return true;

        return notificationType switch
        {
            "schedule_change" => prefs.ScheduleChangesEnabled,
            "reminder" => prefs.RemindersEnabled,
            "announcement" => prefs.AnnouncementsEnabled,
            _ => true
        };
    }

    #endregion
}
