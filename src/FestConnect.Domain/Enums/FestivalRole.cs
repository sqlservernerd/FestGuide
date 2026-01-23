namespace FestConnect.Domain.Enums;

/// <summary>
/// Represents the role a user has for a festival.
/// Ordered by permission level (higher = more permissions).
/// </summary>
public enum FestivalRole
{
    /// <summary>
    /// Read-only access to assigned scopes.
    /// </summary>
    Viewer = 0,

    /// <summary>
    /// Can edit within assigned scopes.
    /// </summary>
    Manager = 1,

    /// <summary>
    /// Full control except ownership transfer.
    /// </summary>
    Administrator = 2,

    /// <summary>
    /// Full control including ownership transfer. One per festival.
    /// </summary>
    Owner = 3
}
