namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents an attendee's personal schedule for a festival edition.
/// Stores the list of engagements (artist performances) the attendee wants to see.
/// </summary>
public class PersonalSchedule : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the personal schedule.
    /// </summary>
    public Guid PersonalScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns this schedule.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the edition this schedule is for.
    /// </summary>
    public Guid EditionId { get; set; }

    /// <summary>
    /// Gets or sets the display name for this schedule.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets whether this is the default schedule for the edition.
    /// A user can have multiple schedules per edition but only one default.
    /// </summary>
    public bool IsDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets the last sync timestamp for offline support.
    /// </summary>
    public DateTime? LastSyncedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether the schedule has been deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets when the schedule was deleted.
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
