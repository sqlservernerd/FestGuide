using FestGuide.Application.Authorization;
using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Permission service implementation.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IFestivalPermissionRepository _permissionRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IUserRepository _userRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<PermissionService> _logger;

    public PermissionService(
        IFestivalPermissionRepository permissionRepository,
        IFestivalRepository festivalRepository,
        IUserRepository userRepository,
        IFestivalAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<PermissionService> logger)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionSummaryDto>> GetByFestivalAsync(Guid festivalId, Guid userId, CancellationToken ct = default)
    {
        // Verify user can view permissions
        if (!await _authorizationService.CanViewFestivalAsync(userId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to view this festival's permissions.");
        }

        var permissions = await _permissionRepository.GetActiveByFestivalAsync(festivalId, ct);
        
        // Batch fetch all users to avoid N+1 query issue
        var userIds = permissions.Select(p => p.UserId).Distinct().ToList();
        var users = await _userRepository.GetByIdsAsync(userIds, ct);
        var userLookup = users.ToDictionary(u => u.UserId);

        var result = new List<PermissionSummaryDto>();
        foreach (var permission in permissions)
        {
            if (!userLookup.TryGetValue(permission.UserId, out var user))
            {
                _logger.LogWarning(
                    "User {UserId} not found for permission {PermissionId} in festival {FestivalId}. This may indicate data integrity issues.",
                    permission.UserId, permission.FestivalPermissionId, festivalId);
            }
            
            result.Add(new PermissionSummaryDto(
                permission.FestivalPermissionId,
                permission.UserId,
                user?.Email,
                user?.DisplayName,
                permission.Role,
                permission.Scope,
                permission.IsPending));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PermissionDto> GetByIdAsync(Guid permissionId, Guid userId, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, ct)
            ?? throw new PermissionNotFoundException(permissionId);

        // Verify user can view this festival's permissions
        if (!await _authorizationService.CanViewFestivalAsync(userId, permission.FestivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to view this permission.");
        }

        var user = await _userRepository.GetByIdAsync(permission.UserId, ct);
        return PermissionDto.FromEntity(permission, user?.Email, user?.DisplayName);
    }

    /// <inheritdoc />
    public async Task<InvitationResultDto> InviteUserAsync(Guid festivalId, Guid invitingUserId, InviteUserRequest request, CancellationToken ct = default)
    {
        // Verify inviting user can manage permissions
        if (!await _authorizationService.CanManagePermissionsAsync(invitingUserId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to invite users to this festival.");
        }

        // Verify festival exists
        if (!await _festivalRepository.ExistsAsync(festivalId, ct))
        {
            throw new FestivalNotFoundException(festivalId);
        }

        // Find or check if user exists
        var invitedUser = await _userRepository.GetByEmailAsync(request.Email, ct);
        var isNewUser = invitedUser == null;

        Guid invitedUserId;
        if (invitedUser != null)
        {
            invitedUserId = invitedUser.UserId;

            // Check if user already has permission for this festival
            var existingPermission = await _permissionRepository.GetByUserAndFestivalAsync(invitedUserId, festivalId, ct);
            if (existingPermission != null && !existingPermission.IsRevoked)
            {
                throw new ConflictException("User already has permission for this festival.");
            }
        }
        else
        {
            // Create a placeholder user ID for pending invitation
            // The actual user will be linked when they register and accept
            invitedUserId = Guid.NewGuid();
        }

        var now = _dateTimeProvider.UtcNow;
        var permission = new FestivalPermission
        {
            FestivalPermissionId = Guid.NewGuid(),
            FestivalId = festivalId,
            UserId = invitedUserId,
            Role = request.Role,
            Scope = request.Role == FestivalRole.Administrator ? PermissionScope.All : request.Scope,
            InvitedByUserId = invitingUserId,
            IsPending = true,
            IsRevoked = false,
            CreatedAtUtc = now,
            CreatedBy = invitingUserId,
            ModifiedAtUtc = now,
            ModifiedBy = invitingUserId
        };

        await _permissionRepository.CreateAsync(permission, ct);

        _logger.LogInformation(
            "User {InvitingUserId} invited {Email} to festival {FestivalId} with role {Role}",
            invitingUserId, request.Email, festivalId, request.Role);

        // TODO: Send invitation email (Phase 3 enhancement)

        var message = isNewUser
            ? $"Invitation sent to {request.Email}. They will need to create an account to accept."
            : $"Invitation sent to {request.Email}.";

        return new InvitationResultDto(
            permission.FestivalPermissionId,
            request.Email,
            request.Role,
            permission.Scope,
            isNewUser,
            message);
    }

    /// <inheritdoc />
    public async Task<PermissionDto> UpdateAsync(Guid permissionId, Guid userId, UpdatePermissionRequest request, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, ct)
            ?? throw new PermissionNotFoundException(permissionId);

        // Verify user can manage permissions
        if (!await _authorizationService.CanManagePermissionsAsync(userId, permission.FestivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to update permissions for this festival.");
        }

        // Cannot modify owner permission
        if (permission.Role == FestivalRole.Owner)
        {
            throw new ForbiddenException("Cannot modify the owner's permission. Use ownership transfer instead.");
        }

        // Cannot promote to owner
        if (request.Role == FestivalRole.Owner)
        {
            throw new ForbiddenException("Cannot change role to Owner. Use ownership transfer instead.");
        }

        var now = _dateTimeProvider.UtcNow;

        if (request.Role.HasValue)
        {
            permission.Role = request.Role.Value;
            // Administrators always have all scopes
            if (request.Role.Value == FestivalRole.Administrator)
            {
                permission.Scope = PermissionScope.All;
            }
        }

        if (request.Scope.HasValue && permission.Role != FestivalRole.Administrator)
        {
            permission.Scope = request.Scope.Value;
        }

        permission.ModifiedAtUtc = now;
        permission.ModifiedBy = userId;

        await _permissionRepository.UpdateAsync(permission, ct);

        _logger.LogInformation(
            "User {UserId} updated permission {PermissionId} for festival {FestivalId}",
            userId, permissionId, permission.FestivalId);

        var user = await _userRepository.GetByIdAsync(permission.UserId, ct);
        return PermissionDto.FromEntity(permission, user?.Email, user?.DisplayName);
    }

    /// <inheritdoc />
    public async Task RevokeAsync(Guid permissionId, Guid userId, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, ct)
            ?? throw new PermissionNotFoundException(permissionId);

        // Verify user can manage permissions
        if (!await _authorizationService.CanManagePermissionsAsync(userId, permission.FestivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to revoke permissions for this festival.");
        }

        // Cannot revoke owner permission
        if (permission.Role == FestivalRole.Owner)
        {
            throw new ForbiddenException("Cannot revoke the owner's permission. Transfer ownership first.");
        }

        // Cannot revoke your own permission
        if (permission.UserId == userId)
        {
            throw new ForbiddenException("Cannot revoke your own permission.");
        }

        await _permissionRepository.RevokeAsync(permissionId, ct);

        _logger.LogInformation(
            "User {UserId} revoked permission {PermissionId} for festival {FestivalId}",
            userId, permissionId, permission.FestivalId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PendingInvitationDto>> GetPendingInvitationsAsync(Guid userId, CancellationToken ct = default)
    {
        var permissions = await _permissionRepository.GetByUserAsync(userId, ct);
        var pendingPermissions = permissions.Where(p => p.IsPending && !p.IsRevoked).ToList();

        // Batch fetch all festivals and users to avoid N+1 query issue
        var festivalIds = pendingPermissions.Select(p => p.FestivalId).Distinct().ToList();
        var userIds = pendingPermissions.Where(p => p.InvitedByUserId.HasValue)
            .Select(p => p.InvitedByUserId!.Value).Distinct().ToList();

        var festivals = await _festivalRepository.GetByIdsAsync(festivalIds, ct);
        var users = await _userRepository.GetByIdsAsync(userIds, ct);

        var festivalLookup = festivals.ToDictionary(f => f.FestivalId);
        var userLookup = users.ToDictionary(u => u.UserId);

        var result = new List<PendingInvitationDto>();
        foreach (var permission in pendingPermissions)
        {
            if (!festivalLookup.TryGetValue(permission.FestivalId, out var festival))
            {
                _logger.LogWarning(
                    "Festival {FestivalId} not found for permission {PermissionId}. This may indicate data integrity issues.",
                    permission.FestivalId, permission.FestivalPermissionId);
            }
            
            User? invitedBy = null;
            if (permission.InvitedByUserId.HasValue)
            {
                if (!userLookup.TryGetValue(permission.InvitedByUserId.Value, out invitedBy))
                {
                    _logger.LogWarning(
                        "Inviting user {UserId} not found for permission {PermissionId}. This may indicate data integrity issues.",
                        permission.InvitedByUserId.Value, permission.FestivalPermissionId);
                }
            }

            result.Add(new PendingInvitationDto(
                permission.FestivalPermissionId,
                permission.FestivalId,
                festival?.Name ?? "Unknown Festival",
                permission.Role,
                permission.Scope,
                invitedBy?.DisplayName,
                permission.CreatedAtUtc));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<PermissionDto> AcceptInvitationAsync(Guid permissionId, Guid userId, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, ct)
            ?? throw new PermissionNotFoundException(permissionId);

        // Verify this invitation is for the current user
        if (permission.UserId != userId)
        {
            throw new ForbiddenException("This invitation is not for you.");
        }

        // Verify invitation is pending
        if (!permission.IsPending)
        {
            throw new ConflictException("This invitation has already been processed.");
        }

        if (permission.IsRevoked)
        {
            throw new ConflictException("This invitation has been revoked.");
        }

        await _permissionRepository.AcceptInvitationAsync(permissionId, ct);

        _logger.LogInformation(
            "User {UserId} accepted invitation {PermissionId} for festival {FestivalId}",
            userId, permissionId, permission.FestivalId);

        // Refresh permission after update
        permission = await _permissionRepository.GetByIdAsync(permissionId, ct);
        var user = await _userRepository.GetByIdAsync(userId, ct);

        return PermissionDto.FromEntity(permission!, user?.Email, user?.DisplayName);
    }

    /// <inheritdoc />
    public async Task DeclineInvitationAsync(Guid permissionId, Guid userId, CancellationToken ct = default)
    {
        var permission = await _permissionRepository.GetByIdAsync(permissionId, ct)
            ?? throw new PermissionNotFoundException(permissionId);

        // Verify this invitation is for the current user
        if (permission.UserId != userId)
        {
            throw new ForbiddenException("This invitation is not for you.");
        }

        // Verify invitation is pending
        if (!permission.IsPending)
        {
            throw new ConflictException("This invitation has already been processed.");
        }

        if (permission.IsRevoked)
        {
            throw new ConflictException("This invitation has been revoked.");
        }
        await _permissionRepository.RevokeAsync(permissionId, ct);

        _logger.LogInformation(
            "User {UserId} declined invitation {PermissionId} for festival {FestivalId}",
            userId, permissionId, permission.FestivalId);
    }
}
