namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a performance area within a venue.
/// </summary>
public class Stage : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the stage.
    /// </summary>
    public Guid StageId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the venue this stage belongs to.
    /// </summary>
    public Guid VenueId { get; set; }

    /// <summary>
    /// Gets or sets the stage name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stage description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the display order of the stage within the venue.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets whether the stage has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the stage was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
