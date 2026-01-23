namespace FestConnect.Domain;

/// <summary>
/// System-wide constants for the domain.
/// </summary>
public static class SystemConstants
{
    /// <summary>
    /// Well-known system user ID used for system-generated operations (e.g., notifications, automated processes).
    /// This ID is used instead of 0 to maintain proper audit trails while distinguishing
    /// system operations from user-initiated actions.
    /// </summary>
    public static readonly long SystemUserId = -1;
}
