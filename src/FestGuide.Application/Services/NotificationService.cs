using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain;
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

    private const int NotificationBatchSize = 100;

    #region Device Management

    /// <inheritdoc />
    public async Task<DeviceTokenDto> RegisterDeviceAsync(long userId, RegisterDeviceRequest request, CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var deviceToken = new DeviceToken
        {
            DeviceTokenId = 0,
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

        await _deviceTokenRepository.UpsertAsync(deviceToken, ct).ConfigureAwait(false);

        _logger.LogInformation("Device registered for user {UserId} on platform {Platform}", userId, request.Platform);

        return DeviceTokenDto.FromEntity(deviceToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceTokenDto>> GetDevicesAsync(long userId, CancellationToken ct = default)
    {
        var devices = await _deviceTokenRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        return devices.Select(DeviceTokenDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task UnregisterDeviceAsync(long userId, long deviceTokenId, CancellationToken ct = default)
    {
        var device = await _deviceTokenRepository.GetByIdAsync(deviceTokenId, ct).ConfigureAwait(false);
        if (device == null || device.UserId != userId)
        {
            throw new ForbiddenException("Device not found or does not belong to user.");
        }

        await _deviceTokenRepository.DeactivateAsync(deviceTokenId, userId, ct).ConfigureAwait(false);

        _logger.LogInformation("Device {DeviceTokenId} unregistered for user {UserId}", deviceTokenId, userId);
    }

    /// <inheritdoc />
    public async Task UnregisterDeviceByTokenAsync(long userId, string token, CancellationToken ct = default)
    {
        await _deviceTokenRepository.DeactivateByTokenAsync(token, userId, ct).ConfigureAwait(false);
        _logger.LogInformation("Device with token unregistered for user {UserId}", userId);
    }

    #endregion

    #region Notification Preferences

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(long userId, CancellationToken ct = default)
    {
        var prefs = await _preferenceRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        if (prefs == null)
        {
            return NotificationPreferenceDto.Default();
        }

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(long userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var existing = await _preferenceRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        var now = _dateTimeProvider.UtcNow;

        var prefs = existing ?? new NotificationPreference
        {
            NotificationPreferenceId = 0,
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
        if (request.TimeZoneId != null) prefs.TimeZoneId = request.TimeZoneId;

        prefs.ModifiedAtUtc = now;
        prefs.ModifiedBy = userId;

        await _preferenceRepository.UpsertAsync(prefs, ct).ConfigureAwait(false);

        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    #endregion

    #region Notification History

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var logs = await _notificationLogRepository.GetByUserAsync(userId, limit, offset, ct).ConfigureAwait(false);
        return logs.Select(NotificationDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default)
    {
        return await _notificationLogRepository.GetUnreadCountAsync(userId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(long userId, long notificationId, CancellationToken ct = default)
    {
        var log = await _notificationLogRepository.GetByIdAsync(notificationId, ct).ConfigureAwait(false);
        if (log == null || log.UserId != userId)
        {
            throw new ForbiddenException("Notification not found or does not belong to user.");
        }

        await _notificationLogRepository.MarkAsReadAsync(notificationId, userId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(long userId, CancellationToken ct = default)
    {
        await _notificationLogRepository.MarkAllAsReadAsync(userId, userId, ct).ConfigureAwait(false);
    }

    #endregion

    #region Sending Notifications

    /// <inheritdoc />
    public async Task SendToUserAsync(
        long userId,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        // Check user preferences
        var prefs = await _preferenceRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
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

        // Check quiet hours
        if (prefs != null && IsInQuietHours(prefs))
        {
            _logger.LogDebug("User {UserId} is in quiet hours, skipping notification", userId);
            return;
        }

        var devices = await _deviceTokenRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
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
                NotificationLogId = 0,
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
                CreatedBy = SystemConstants.SystemUserId,
                ModifiedAtUtc = now,
                ModifiedBy = SystemConstants.SystemUserId
            };

            try
            {
                var message = new PushNotificationMessage(title, body, notificationType, data);
                await _pushProvider.SendAsync(device.Token, device.Platform, message, ct).ConfigureAwait(false);

                log.IsDelivered = true;
                await _deviceTokenRepository.UpdateLastUsedAsync(device.DeviceTokenId, now, ct).ConfigureAwait(false);

                _logger.LogInformation("Notification sent to user {UserId} on device {DeviceId}", userId, device.DeviceTokenId);
            }
            catch (InvalidDeviceTokenException ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Invalid device token for device {DeviceId}, deactivating token.", device.DeviceTokenId);

                await _deviceTokenRepository.DeactivateAsync(device.DeviceTokenId, SystemConstants.SystemUserId, ct).ConfigureAwait(false);
            }
            catch (UnregisteredDeviceException ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Unregistered device {DeviceId}, deactivating token.", device.DeviceTokenId);

                await _deviceTokenRepository.DeactivateAsync(device.DeviceTokenId, SystemConstants.SystemUserId, ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to send notification to device {DeviceId}", device.DeviceTokenId);
            }

            await _notificationLogRepository.CreateAsync(log, ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SendToUsersAsync(
        IEnumerable<long> userIds,
        string notificationType,
        string title,
        string body,
        string? relatedEntityType = null,
        long? relatedEntityId = null,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        var userIdList = userIds.ToList();

        for (var i = 0; i < userIdList.Count; i += NotificationBatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = userIdList
                .Skip(i)
                .Take(NotificationBatchSize);

            var sendTasks = batch.Select(userId =>
                SendToUserAsync(
                    userId,
                    notificationType,
                    title,
                    body,
                    relatedEntityType,
                    relatedEntityId,
                    data,
                    ct));

            await Task.WhenAll(sendTasks).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SendScheduleChangeAsync(ScheduleChangeNotification change, CancellationToken ct = default)
    {
        IEnumerable<long> userIds;

        // If we have a specific engagement, only notify users who have that engagement saved
        if (change.EngagementId.HasValue)
        {
            userIds = await _personalScheduleRepository.GetUserIdsWithEngagementAsync(change.EngagementId.Value, ct).ConfigureAwait(false);
        }
        else
        {
            // Fetch all users who have schedules for this edition using pagination
            // to avoid data loss for large festivals
            const int batchSize = 1000;
            var offset = 0;
            var allUserIds = new HashSet<long>(batchSize);
            
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                
                var schedules = await _personalScheduleRepository.GetByEditionAsync(
                    change.EditionId, 
                    limit: batchSize, 
                    offset: offset, 
                    ct).ConfigureAwait(false);
                
                if (!schedules.Any())
                {
                    break;
                }
                
                foreach (var schedule in schedules)
                {
                    allUserIds.Add(schedule.UserId);
                }
                
                // If we received fewer results than the batch size, we've reached the end
                if (schedules.Count < batchSize)
                {
                    break;
                }
                
                offset += batchSize;
            }
            
            userIds = allUserIds;
        }

        var userIdList = userIds.ToList();
        if (!userIdList.Any())
        {
            _logger.LogDebug("No users to notify for schedule change on edition {EditionId}", change.EditionId);
            return;
        }

        var data = new Dictionary<string, string>
        {
            ["editionId"] = change.EditionId.ToString(),
            ["changeType"] = change.ChangeType
        };

        if (change.EngagementId.HasValue) data["engagementId"] = change.EngagementId.Value.ToString();
        if (change.TimeSlotId.HasValue) data["timeSlotId"] = change.TimeSlotId.Value.ToString();

        await SendToUsersAsync(
            userIdList,
            "schedule_change",
            $"Schedule Update: {change.ArtistName ?? "Performance"}",
            change.Message,
            "Edition",
            change.EditionId,
            data,
            ct).ConfigureAwait(false);

        _logger.LogInformation("Schedule change notification sent to {Count} users for edition {EditionId}",
            userIdList.Count, change.EditionId);
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

    private bool IsInQuietHours(NotificationPreference prefs)
    {
        if (!prefs.QuietHoursStart.HasValue || !prefs.QuietHoursEnd.HasValue)
        {
            return false;
        }

        // Convert UTC time to user's local timezone
        TimeZoneInfo userTimeZone;
        try
        {
            userTimeZone = TimeZoneInfo.FindSystemTimeZoneById(prefs.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Invalid timezone '{TimeZone}' for user quiet hours, defaulting to UTC", prefs.TimeZoneId);
            userTimeZone = TimeZoneInfo.Utc;
        }

        var userLocalTime = TimeZoneInfo.ConvertTimeFromUtc(_dateTimeProvider.UtcNow, userTimeZone);
        var now = TimeOnly.FromTimeSpan(userLocalTime.TimeOfDay);
        var start = prefs.QuietHoursStart.Value;
        var end = prefs.QuietHoursEnd.Value;

        // Handle quiet hours that span midnight (e.g., 23:00 to 06:00)
        // Start is inclusive, end is exclusive
        if (start > end)
        {
            return now >= start || now < end;
        }

        return now >= start && now < end;
    }

    #endregion
}
