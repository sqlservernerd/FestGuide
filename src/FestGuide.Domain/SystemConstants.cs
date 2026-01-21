namespace FestGuide.Domain;

/// <summary>
/// System-wide constants for the domain.
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Well-known system user ID used for system-generated operations (e.g., notifications, automated processes).
    /// This GUID is used instead of Guid.Empty to maintain proper audit trails while distinguishing
    /// system operations from user-initiated actions.
    /// </summary>
    public static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");
}
