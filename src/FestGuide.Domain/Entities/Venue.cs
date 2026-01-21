namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a physical location where festival events take place.
/// </summary>
public class Venue : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the venue.
    /// </summary>
    public Guid VenueId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the festival this venue belongs to.
    /// </summary>
    public Guid FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the venue name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the venue description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the venue address.
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Gets or sets the latitude coordinate.
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude coordinate.
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// Gets or sets whether the venue has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the venue was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
