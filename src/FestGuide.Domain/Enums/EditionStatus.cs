namespace FestGuide.Domain.Enums;

/// <summary>
/// Represents the status of a festival edition.
/// </summary>
public enum EditionStatus
{
    /// <summary>
    /// Edition is in draft mode, not visible to attendees.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Edition is published and visible to attendees.
    /// </summary>
    Published = 1,

    /// <summary>
    /// Edition has ended and is archived.
    /// </summary>
    Archived = 2
}
