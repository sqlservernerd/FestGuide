using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for festival permission.
/// </summary>
public sealed record PermissionDto(
    long PermissionId,
    long FestivalId,
    long UserId,
    string? UserEmail,
    string? UserDisplayName,
    FestivalRole Role,
    PermissionScope Scope,
    bool IsPending,
    DateTime? AcceptedAtUtc,
    long? InvitedByUserId,
    DateTime CreatedAtUtc)
{
    public static PermissionDto FromEntity(FestivalPermission permission, string? userEmail = null, string? userDisplayName = null) =>
        new(
            permission.FestivalPermissionId,
            permission.FestivalId,
            permission.UserId,
            userEmail,
            userDisplayName,
            permission.Role,
            permission.Scope,
            permission.IsPending,
            permission.AcceptedAtUtc,
            permission.InvitedByUserId,
            permission.CreatedAtUtc);
}

/// <summary>
/// Summary DTO for permission list items.
/// </summary>
public sealed record PermissionSummaryDto(
    long PermissionId,
    long UserId,
    string? UserEmail,
    string? UserDisplayName,
    FestivalRole Role,
    PermissionScope Scope,
    bool IsPending);

/// <summary>
/// Request DTO for inviting a user to a festival.
/// </summary>
public sealed record InviteUserRequest(
    string Email,
    FestivalRole Role,
    PermissionScope Scope);

/// <summary>
/// Request DTO for updating a permission.
/// </summary>
public sealed record UpdatePermissionRequest(
    FestivalRole? Role,
    PermissionScope? Scope);

/// <summary>
/// Response DTO for invitation result.
/// </summary>
public sealed record InvitationResultDto(
    long PermissionId,
    string InvitedEmail,
    FestivalRole Role,
    PermissionScope Scope,
    bool IsNewUser,
    string Message);

/// <summary>
/// Request DTO for accepting an invitation.
/// </summary>
public sealed record AcceptInvitationRequest(
    long PermissionId);

/// <summary>
/// Response DTO for pending invitations for a user.
/// </summary>
public sealed record PendingInvitationDto(
    long PermissionId,
    long FestivalId,
    string FestivalName,
    FestivalRole Role,
    PermissionScope Scope,
    string? InvitedByUserName,
    DateTime CreatedAtUtc);
