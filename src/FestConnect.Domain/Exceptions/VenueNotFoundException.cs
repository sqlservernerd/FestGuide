namespace FestConnect.Domain.Exceptions;

/// <summary>
/// Exception thrown when a venue is not found.
/// </summary>
public class VenueNotFoundException : DomainException
{
    public VenueNotFoundException(long venueId)
        : base($"Venue with ID '{venueId}' was not found.")
    {
        VenueId = venueId;
    }

    public long VenueId { get; }
}
