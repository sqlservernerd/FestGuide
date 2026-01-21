using FestGuide.Domain.Enums;

namespace FestGuide.Domain.Entities;

/// <summary>
/// Represents a user's permission to access a festival.
/// Controls what role and scope a user has for organizer features.
/// </summary>
public class FestivalPermission : BaseEntity
{
    /// <summary>
    /// Gets or sets the unique identifier for the permission.
    /// </summary>
    public Guid FestivalPermissionId { get; set; }

    /// <summary>
    /// Gets or sets the festival this permission applies to.
    /// </summary>
    public Guid FestivalId { get; set; }

    /// <summary>
    /// Gets or sets the user who has this permission.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the role level (Owner, Administrator, Manager, Viewer).
    /// </summary>
    public FestivalRole Role { get; set; }

    /// <summary>
    /// Gets or sets the permission scope (All, Venues, Schedule, Artists, Editions, Integrations).
    /// Only applicable for Manager and Viewer roles.
    /// </summary>
    public PermissionScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the user who invited this user (null for Owner who created festival).
    /// </summary>
    public Guid? InvitedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the invitation was accepted (UTC).
    /// Null if invitation is still pending.
    /// </summary>
    public DateTime? AcceptedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets whether this permission is still pending acceptance.
    /// </summary>
    public bool IsPending { get; set; }

    /// <summary>
    /// Gets or sets whether this permission has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the permission was revoked (UTC).
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }

    /// <summary>
    /// Gets whether this permission is currently active (accepted and not revoked).
    /// </summary>
    public bool IsActive => !IsPending && !IsRevoked && AcceptedAtUtc.HasValue;

    /// <summary>
    /// Gets whether this user can manage other users' permissions.
    /// Only Owner and Administrator can manage permissions.
    /// </summary>
    public bool CanManagePermissions => Role >= FestivalRole.Administrator;

    /// <summary>
    /// Gets whether this user has full access to all festival areas.
    /// Owner and Administrator always have full access; others depend on scope.
    /// </summary>
    public bool HasFullAccess => Role >= FestivalRole.Administrator || Scope == PermissionScope.All;
}
