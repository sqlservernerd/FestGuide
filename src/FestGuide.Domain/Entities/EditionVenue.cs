namespace FestGuide.Domain.Entities;

/// <summary>
/// Junction entity linking editions to venues.
/// </summary>
public class EditionVenue
{
    /// <summary>
    /// Gets or sets the unique identifier for the edition-venue link.
    /// </summary>
    public Guid EditionVenueId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the edition.
    /// </summary>
    public Guid EditionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the venue.
    /// </summary>
    public Guid VenueId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the link was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the link.
    /// </summary>
    public Guid CreatedBy { get; set; }
}
