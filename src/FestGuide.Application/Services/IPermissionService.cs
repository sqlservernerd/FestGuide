using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for permission management operations.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permissions for a festival.
    /// </summary>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of permissions with user details.</returns>
    Task<IReadOnlyList<PermissionSummaryDto>> GetByFestivalAsync(Guid festivalId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific permission by ID.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The permission details.</returns>
    Task<PermissionDto> GetByIdAsync(Guid permissionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Invites a user to a festival with a specific role and scope.
    /// </summary>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="invitingUserId">The user sending the invitation.</param>
    /// <param name="request">The invitation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The invitation result.</returns>
    Task<InvitationResultDto> InviteUserAsync(Guid festivalId, Guid invitingUserId, InviteUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated permission.</returns>
    Task<PermissionDto> UpdateAsync(Guid permissionId, Guid userId, UpdatePermissionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeAsync(Guid permissionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets pending invitations for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending invitations.</returns>
    Task<IReadOnlyList<PendingInvitationDto>> GetPendingInvitationsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pending invitation.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The user accepting the invitation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The accepted permission.</returns>
    Task<PermissionDto> AcceptInvitationAsync(Guid permissionId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Declines a pending invitation.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The user declining the invitation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeclineInvitationAsync(Guid permissionId, Guid userId, CancellationToken ct = default);
}
