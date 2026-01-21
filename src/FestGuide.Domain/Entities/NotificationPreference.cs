namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a user's notification preferences.
/// </summary>
public class NotificationPreference : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the preference record.
    /// </summary>
    public Guid NotificationPreferenceId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user.
    /// </summary>
    public Guid UserId { get; set; }

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
}
