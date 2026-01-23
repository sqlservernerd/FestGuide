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
    Task<FestivalPermission?> GetByIdAsync(long permissionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a user's permission for a specific festival.
    /// </summary>
    Task<FestivalPermission?> GetByUserAndFestivalAsync(long userId, long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all permissions for a festival.
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all active permissions for a festival (accepted and not revoked).
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetActiveByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all permissions for a user across all festivals.
    /// </summary>
    Task<IEnumerable<FestivalPermission>> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the owner permission for a festival.
    /// </summary>
    Task<FestivalPermission?> GetOwnerAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has any permission for a festival.
    /// </summary>
    Task<bool> HasAnyPermissionAsync(long userId, long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has at least the specified role for a festival.
    /// </summary>
    Task<bool> HasRoleOrHigherAsync(long userId, long festivalId, FestivalRole minimumRole, CancellationToken ct = default);

    /// <summary>
    /// Checks if a user has the specified scope for a festival.
    /// </summary>
    Task<bool> HasScopeAsync(long userId, long festivalId, PermissionScope scope, CancellationToken ct = default);

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    Task<long> CreateAsync(FestivalPermission permission, CancellationToken ct = default);
    
    /// <summary>
    /// Creates a new permission within an existing transaction.
    /// </summary>
    Task<long> CreateAsync(FestivalPermission permission, ITransactionScope transactionScope, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    Task UpdateAsync(FestivalPermission permission, CancellationToken ct = default);

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    Task RevokeAsync(long permissionId, CancellationToken ct = default);

    /// <summary>
    /// Transfers ownership from one user to another.
    /// </summary>
    Task TransferOwnershipAsync(long festivalId, long fromUserId, long toUserId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pending permission invitation.
    /// </summary>
    Task AcceptInvitationAsync(long permissionId, CancellationToken ct = default);

    /// <summary>
    /// Declines a pending permission invitation.
    /// </summary>
    Task DeclineInvitationAsync(long permissionId, CancellationToken ct = default);
}
