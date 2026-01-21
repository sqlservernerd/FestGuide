namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when an artist is not found.
/// </summary>
public class ArtistNotFoundException : DomainException
{
    public ArtistNotFoundException(Guid artistId)
        : base($"Artist with ID '{artistId}' was not found.")
    {
        ArtistId = artistId;
    }

    public Guid ArtistId { get; }
}
