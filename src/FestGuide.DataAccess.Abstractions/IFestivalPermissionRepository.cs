using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for FestivalPermission data access operations.
/// </summary>
public interface IFestivalPermissionRepository
{
    /// <summary>
    /// Gets a permission by its unique identifier.
    /// </summary>
    Task<FestivalPermission?> GetByIdAsync(Guid permissionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a user's permission for a specific festival.
    /// </summary>
    Task<FestivalPermission?> GetByUserAndFestivalAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all permissions for a festival.
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active permissions for a festival (accepted and not revoked).
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetActiveByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all permissions for a user across all festivals.
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the owner permission for a festival.
    /// </summary>
    Task<FestivalPermission?> GetOwnerAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has any permission for a festival.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(Guid userId, Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has at least the specified role for a festival.
    /// </summary>
    Task<bool> HasRoleOrHigherAsync(Guid userId, Guid festivalId, FestivalRole minimumRole, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has the specified scope for a festival.
    /// </summary>
    Task<bool> HasScopeAsync(Guid userId, Guid festivalId, PermissionScope scope, CancellationToken ct = default);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    Task<Guid> CreateAsync(FestivalPermission permission, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a new permission within an existing transaction.
    /// </summary>
    Task<Guid> CreateAsync(FestivalPermission permission, ITransactionScope transactionScope, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    Task UpdateAsync(FestivalPermission permission, CancellationToken ct = default);

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    Task RevokeAsync(Guid permissionId, CancellationToken ct = default);

    /// <summary>
    /// Transfers ownership from one user to another.
    /// </summary>
    Task TransferOwnershipAsync(Guid festivalId, Guid fromUserId, Guid toUserId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pending permission invitation.
    /// </summary>
    Task AcceptInvitationAsync(Guid permissionId, CancellationToken ct = default);

    /// <summary>
    /// Declines a pending permission invitation.
    /// </summary>
    Task DeclineInvitationAsync(Guid permissionId, CancellationToken ct = default);
}
