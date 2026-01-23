namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents a performer at a festival.
/// Artists are scoped to a festival and reusable across editions.
/// </summary>
public class Artist : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the artist.
    /// </summary>
    public long ArtistId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the festival this artist belongs to.
    /// </summary>
    public long FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the artist name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the artist's genre.
    /// </summary>
    public string? Genre { get; set; }

    /// <summary>
    /// Gets or sets the artist's biography.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the URL of the artist's image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the artist's website.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the artist's Spotify profile.
    /// </summary>
    public string? SpotifyUrl { get; set; }

    /// <summary>
    /// Gets or sets whether the artist has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the artist was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
