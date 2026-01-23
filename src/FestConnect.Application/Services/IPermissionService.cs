using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

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
    Task<IReadOnlyList<PermissionSummaryDto>> GetByFestivalAsync(long festivalId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific permission by ID.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The permission details.</returns>
    Task<PermissionDto> GetByIdAsync(long permissionId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Invites a user to a festival with a specific role and scope.
    /// </summary>
    /// <param name="festivalId">The festival ID.</param>
    /// <param name="invitingUserId">The user sending the invitation.</param>
    /// <param name="request">The invitation request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The invitation result.</returns>
    Task<InvitationResultDto> InviteUserAsync(long festivalId, long invitingUserId, InviteUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing permission.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="request">The update request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated permission.</returns>
    Task<PermissionDto> UpdateAsync(long permissionId, long userId, UpdatePermissionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Revokes a permission.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The requesting user ID (for authorization).</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeAsync(long permissionId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets pending invitations for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of pending invitations.</returns>
    Task<IReadOnlyList<PendingInvitationDto>> GetPendingInvitationsAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Accepts a pending invitation.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The user accepting the invitation.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The accepted permission.</returns>
    Task<PermissionDto> AcceptInvitationAsync(long permissionId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Declines a pending invitation.
    /// </summary>
    /// <param name="permissionId">The permission ID.</param>
    /// <param name="userId">The user declining the invitation.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeclineInvitationAsync(long permissionId, long userId, CancellationToken ct = default);
}
