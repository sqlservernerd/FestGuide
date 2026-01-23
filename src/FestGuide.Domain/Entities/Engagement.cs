namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents an artist assigned to a time slot.
/// Links artists to their scheduled performance times.
/// </summary>
public class Engagement : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the engagement.
    /// </summary>
    public long EngagementId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the time slot for this engagement.
    /// </summary>
    public long TimeSlotId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the artist for this engagement.
    /// </summary>
    public long ArtistId { get; set; }

    /// <summary>
    /// Gets or sets optional notes about the engagement.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets whether the engagement has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the engagement was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
