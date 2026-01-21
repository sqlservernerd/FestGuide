namespace FestGuide.Application.Authorization;

/// <summary>
/// Defines the permission scopes available for festival access control.
/// </summary>
public static class PermissionScopes
{
    public const string Venues = "venues";
    public const string Schedule = "schedule";
    public const string Artists = "artists";
    public const string Editions = "editions";
    public const string Integrations = "integrations";
    public const string All = "all";

    public static readonly IReadOnlyList<string> ValidScopes = new[]
    {
        Venues,
        Schedule,
        Artists,
        Editions,
        Integrations,
        All
    };

    public static bool IsValidScope(string scope) =>
        ValidScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Defines the roles available for festival permission hierarchy.
/// </summary>
public static class FestivalRoles
{
    public const string Owner = "owner";
    public const string Administrator = "administrator";
    public const string Manager = "manager";
    public const string Viewer = "viewer";

    public static readonly IReadOnlyList<string> ValidRoles = new[]
    {
        Owner,
        Administrator,
        Manager,
        Viewer
    };

    /// <summary>
    /// Gets the hierarchy level of a role (higher = more permissions).
    /// </summary>
    public static int GetRoleLevel(string role) => role.ToLowerInvariant() switch
    {
        Owner => 4,
        Administrator => 3,
        Manager => 2,
        Viewer => 1,
        _ => 0
    };

    /// <summary>
    /// Checks if a role can manage another role.
    /// </summary>
    public static bool CanManageRole(string managerRole, string targetRole) =>
        GetRoleLevel(managerRole) > GetRoleLevel(targetRole);
}
