using FestGuide.Domain.Enums;

namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents the master schedule for a festival edition.
/// Tracks the publishing state and history.
/// </summary>
public class Schedule : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the schedule.
    /// </summary>
    public long ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the edition this schedule belongs to.
    /// </summary>
    public long EditionId { get; set; }

    /// <summary>
    /// Gets or sets the schedule version number.
    /// Incremented each time the schedule is published.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the date and time when the schedule was published (UTC).
    /// Null if never published.
    /// </summary>
    public DateTime? PublishedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who published the schedule.
    /// </summary>
    public long? PublishedBy { get; set; }

    /// <summary>
    /// Gets whether the schedule has been published.
    /// </summary>
    public bool IsPublished => PublishedAtUtc.HasValue;
}
