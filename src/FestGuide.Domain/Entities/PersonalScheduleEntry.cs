namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents an entry in an attendee's personal schedule.
/// Links a personal schedule to a specific engagement (artist + time slot).
/// </summary>
public class PersonalScheduleEntry : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the entry.
    /// </summary>
    public long PersonalScheduleEntryId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the personal schedule this entry belongs to.
    /// </summary>
    public long PersonalScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the engagement (artist performance) saved.
    /// </summary>
    public long EngagementId { get; set; }

    /// <summary>
    /// Gets or sets optional notes the attendee added for this entry.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the attendee wants notifications for this entry.
    /// </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this entry has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets when the entry was deleted.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
