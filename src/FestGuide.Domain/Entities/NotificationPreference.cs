namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a user's notification preferences.
/// </summary>
public class NotificationPreference : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the preference record.
    /// </summary>
    public long NotificationPreferenceId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets whether push notifications are enabled globally.
    /// </summary>
    public bool PushEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether email notifications are enabled.
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether schedule change notifications are enabled.
    /// </summary>
    public bool ScheduleChangesEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether reminder notifications are enabled.
    /// </summary>
    public bool RemindersEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets how many minutes before a performance to send reminders.
    /// Valid range: 5-1440 minutes (up to 24 hours). Validation is enforced at the application layer.
    /// </summary>
    public int ReminderMinutesBefore { get; set; } = 30;

    /// <summary>
    /// Gets or sets whether announcement notifications are enabled.
    /// </summary>
    public bool AnnouncementsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets quiet hours start time (local to user).
    /// </summary>
    public TimeOnly? QuietHoursStart { get; set; }

    /// <summary>
    /// Gets or sets quiet hours end time (local to user).
    /// </summary>
    public TimeOnly? QuietHoursEnd { get; set; }

    /// <summary>
    /// Gets or sets the user's IANA timezone identifier (e.g., "America/New_York", "Europe/London").
    /// Used to correctly apply quiet hours in the user's local timezone.
    /// Defaults to "UTC" if not specified.
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";
}
