namespace FestGuide.Domain.Enums;

/// <summary>
/// Represents the permission scope a user has for a festival.
/// Scopes determine what areas a Manager or Viewer can access.
/// </summary>
public enum PermissionScope
{
    /// <summary>
    /// Access to all areas.
    /// </summary>
    All = 0,

    /// <summary>
    /// Access to venue and stage management.
    /// </summary>
    Venues = 1,

    /// <summary>
    /// Access to schedule, time slots, and engagements.
    /// </summary>
    Schedule = 2,

    /// <summary>
    /// Access to artist management.
    /// </summary>
    Artists = 3,

    /// <summary>
    /// Access to edition management.
    /// </summary>
    Editions = 4,

    /// <summary>
    /// Access to integrations (API keys, webhooks, widgets).
    /// </summary>
    Integrations = 5
}
