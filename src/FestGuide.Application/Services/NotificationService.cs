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
    private const int MaxSchedulesPerEdition = 10000; // Maximum number of schedules to fetch per edition for notifications

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

        await _deviceTokenRepository.UpsertAsync(deviceToken, ct).ConfigureAwait(false);

        _logger.LogInformation("Device registered for user {UserId} on platform {Platform}", userId, request.Platform);

        return DeviceTokenDto.FromEntity(deviceToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeviceTokenDto>> GetDevicesAsync(Guid userId, CancellationToken ct = default)
    {
        var devices = await _deviceTokenRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        return devices.Select(DeviceTokenDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task UnregisterDeviceAsync(Guid userId, Guid deviceTokenId, CancellationToken ct = default)
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
    public async Task UnregisterDeviceByTokenAsync(string token, CancellationToken ct = default)
    {
        // NOTE: For token-based unregistration, we use Guid.Empty since we don't have authenticated user context.
        // This makes it impossible to audit who deactivated a device token, but is necessary for
        // unauthenticated device token cleanup scenarios.
        await _deviceTokenRepository.DeactivateByTokenAsync(token, Guid.Empty, ct).ConfigureAwait(false);
    }

    #endregion

    #region Notification Preferences

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> GetPreferencesAsync(Guid userId, CancellationToken ct = default)
    {
        var prefs = await _preferenceRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
        if (prefs == null)
        {
            return NotificationPreferenceDto.Default();
        }

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    /// <inheritdoc />
    public async Task<NotificationPreferenceDto> UpdatePreferencesAsync(Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        var existing = await _preferenceRepository.GetByUserAsync(userId, ct).ConfigureAwait(false);
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

        await _preferenceRepository.UpsertAsync(prefs, ct).ConfigureAwait(false);

        _logger.LogInformation("Notification preferences updated for user {UserId}", userId);

        return NotificationPreferenceDto.FromEntity(prefs);
    }

    #endregion

    #region Notification History

    /// <inheritdoc />
    public async Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(Guid userId, int limit = 50, int offset = 0, CancellationToken ct = default)
    {
        var logs = await _notificationLogRepository.GetByUserAsync(userId, limit, offset, ct).ConfigureAwait(false);
        return logs.Select(NotificationDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
    {
        return await _notificationLogRepository.GetUnreadCountAsync(userId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var log = await _notificationLogRepository.GetByIdAsync(notificationId, ct).ConfigureAwait(false);
        if (log == null || log.UserId != userId)
        {
            throw new ForbiddenException("Notification not found or does not belong to user.");
        }

        await _notificationLogRepository.MarkAsReadAsync(notificationId, userId, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        await _notificationLogRepository.MarkAllAsReadAsync(userId, userId, ct).ConfigureAwait(false);
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
            // NOTE: NotificationLog uses Guid.Empty for CreatedBy and ModifiedBy fields when notifications
            // are system-generated. This is a design pattern to distinguish system-generated notifications
            // from user-initiated operations, though it makes auditing system operations more difficult.
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
                await _pushProvider.SendAsync(device.Token, device.Platform, message, ct).ConfigureAwait(false);

                log.IsDelivered = true;
                await _deviceTokenRepository.UpdateLastUsedAsync(device.DeviceTokenId, now, ct).ConfigureAwait(false);

                _logger.LogInformation("Notification sent to user {UserId} on device {DeviceId}", userId, device.DeviceTokenId);
            }
            catch (Exception ex)
            {
                log.ErrorMessage = ex.Message;
                _logger.LogWarning(ex, "Failed to send notification to device {DeviceId}", device.DeviceTokenId);

                // NOTE: Deactivate invalid tokens using string matching.
                // This is a known limitation - it's fragile and language-dependent.
                // Future improvement: Use specific exception types or error codes from the push notification provider.
                if (ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) || 
                    ex.Message.Contains("unregistered", StringComparison.OrdinalIgnoreCase))
                {
                    await _deviceTokenRepository.DeactivateAsync(device.DeviceTokenId, Guid.Empty, ct).ConfigureAwait(false);
                }
            }

            await _notificationLogRepository.CreateAsync(log, ct).ConfigureAwait(false);
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
        const int BatchSize = 100;
        var userIdList = userIds.ToList();

        for (var i = 0; i < userIdList.Count; i += BatchSize)
        {
            ct.ThrowIfCancellationRequested();

            var batch = userIdList
                .Skip(i)
                .Take(BatchSize);

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
        IEnumerable<Guid> userIds;

        // If we have a specific engagement, only notify users who have that engagement saved
        if (change.EngagementId.HasValue)
        {
            userIds = await _personalScheduleRepository.GetUserIdsWithEngagementAsync(change.EngagementId.Value, ct).ConfigureAwait(false);
        }
        else
        {
            // Otherwise, notify all users who have schedules for this edition
            // NOTE: Limited to MaxSchedulesPerEdition schedules. If an edition has more schedules than this limit,
            // some users may not receive notifications. Consider implementing batch processing for very large editions.
            var schedules = await _personalScheduleRepository.GetByEditionAsync(change.EditionId, limit: MaxSchedulesPerEdition, offset: 0, ct).ConfigureAwait(false);
            userIds = schedules.Select(s => s.UserId).Distinct();
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

        // NOTE: This implementation uses UTC time to compare against user-local quiet hours settings.
        // This is a known limitation and will produce incorrect behavior for users in different time zones.
        // Future improvement: Store user timezone information and convert UTC to user's local time.
        var now = TimeOnly.FromTimeSpan(_dateTimeProvider.UtcNow.TimeOfDay);
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
