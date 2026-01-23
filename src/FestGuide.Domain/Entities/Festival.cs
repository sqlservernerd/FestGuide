namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a recurring festival brand.
/// </summary>
public class Festival : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the festival.
    /// </summary>
    public long FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the festival name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the festival description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the URL of the festival's image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL of the festival's website.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who owns the festival.
    /// </summary>
    public long OwnerUserId { get; set; }

    /// <summary>
    /// Gets or sets whether the festival has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the festival was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
