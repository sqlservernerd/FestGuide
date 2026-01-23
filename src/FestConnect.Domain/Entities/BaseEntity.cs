namespace FestConnect.Domain.Entities;

/// <summary>
/// Base class for all domain entities with common audit properties.
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets or sets the date and time when the entity was created (UTC).
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the entity.
    /// </summary>
    public long? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was last modified (UTC).
    /// </summary>
    public DateTime ModifiedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last modified the entity.
    /// </summary>
    public long? ModifiedBy { get; set; }
}
