namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a block of time on a stage for a performance.
/// </summary>
public class TimeSlot : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the time slot.
    /// </summary>
    public Guid TimeSlotId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the stage this time slot belongs to.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the edition this time slot belongs to.
    /// </summary>
    public Guid EditionId { get; set; }

    /// <summary>
    /// Gets or sets the start time of the slot (UTC).
    /// </summary>
    public DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the end time of the slot (UTC).
    /// </summary>
    public DateTime EndTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets whether the time slot has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the time slot was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
