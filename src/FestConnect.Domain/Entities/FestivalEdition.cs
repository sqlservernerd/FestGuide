using FestConnect.Domain.Enums;

namespace FestConnect.Domain.Entities;

/// <summary>
/// Represents a specific instance of a festival with dates and timezone.
/// </summary>
public class FestivalEdition : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the edition.
    /// </summary>
    public long EditionId { get; set; }

    /// <summary>
    /// Gets or sets the ID of the festival this edition belongs to.
    /// </summary>
    public long FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the edition name (e.g., "2026 Summer Edition").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start date and time of the edition (UTC).
    /// </summary>
    public DateTime StartDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the end date and time of the edition (UTC).
    /// </summary>
    public DateTime EndDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the IANA timezone identifier for the edition.
    /// </summary>
    public string TimezoneId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the external URL for ticket purchases.
    /// </summary>
    public string? TicketUrl { get; set; }

    /// <summary>
    /// Gets or sets the status of the edition (Draft, Published, Archived).
    /// </summary>
    public EditionStatus Status { get; set; } = EditionStatus.Draft;

    /// <summary>
    /// Gets or sets whether the edition has been soft-deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the edition was deleted (UTC).
    /// </summary>
    public DateTime? DeletedAtUtc { get; set; }
}
