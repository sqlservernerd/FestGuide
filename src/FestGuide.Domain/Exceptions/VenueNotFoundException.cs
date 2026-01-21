namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when a venue is not found.
/// </summary>
public class VenueNotFoundException : DomainException
{
    public VenueNotFoundException(Guid venueId)
        : base($"Venue with ID '{venueId}' was not found.")
    {
        VenueId = venueId;
    }

    public Guid VenueId { get; }
}
