namespace FestGuide.Domain.Exceptions;

/// <summary>
/// Exception thrown when an artist is not found.
/// </summary>
public class ArtistNotFoundException : DomainException
{
    public ArtistNotFoundException(long artistId)
        : base($"Artist with ID '{artistId}' was not found.")
    {
        ArtistId = artistId;
    }

    public long ArtistId { get; }
}
