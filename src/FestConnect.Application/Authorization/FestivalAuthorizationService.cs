using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace FestConnect.Application.Authorization;

/// <summary>
/// Service for festival-level authorization checks.
/// Uses FestivalPermission repository to verify user access.
/// </summary>
public class FestivalAuthorizationService : IFestivalAuthorizationService
{
    private readonly IFestivalPermissionRepository _permissionRepository;
    private readonly ILogger<FestivalAuthorizationService> _logger;

    public FestivalAuthorizationService(
        IFestivalPermissionRepository permissionRepository,
        ILogger<FestivalAuthorizationService> logger)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<bool> CanViewFestivalAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug("Checking view permission for user {UserId} on festival {FestivalId}", userId, festivalId);
        
        // Any user with active permission can view
        return await _permissionRepository.HasAnyPermissionAsync(userId, festivalId, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanEditFestivalAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug("Checking edit permission for user {UserId} on festival {FestivalId}", userId, festivalId);
        
        // Manager or higher can edit (within their scope)
        return await _permissionRepository.HasRoleOrHigherAsync(userId, festivalId, FestivalRole.Manager, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanDeleteFestivalAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug("Checking delete permission for user {UserId} on festival {FestivalId}", userId, festivalId);
        
        // Only owner can delete
        return await _permissionRepository.HasRoleOrHigherAsync(userId, festivalId, FestivalRole.Owner, ct);
    }


    /// <inheritdoc />
    public async Task<bool> HasScopeAsync(long userId, long festivalId, PermissionScope scope, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking scope '{Scope}' for user {UserId} on festival {FestivalId}",
            scope, userId, festivalId);

        return await _permissionRepository.HasScopeAsync(userId, festivalId, scope, ct);
    }

    /// <inheritdoc />
    public async Task<string?> GetRoleAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug("Getting role for user {UserId} on festival {FestivalId}", userId, festivalId);
        
        var permission = await _permissionRepository.GetByUserAndFestivalAsync(userId, festivalId, ct);
        
        return permission?.Role.ToString().ToLowerInvariant();
    }

    /// <inheritdoc />
    public async Task<bool> CanManagePermissionsAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking permission management capability for user {UserId} on festival {FestivalId}",
            userId, festivalId);
        
        // Only Administrator or Owner can manage permissions
        return await _permissionRepository.HasRoleOrHigherAsync(userId, festivalId, FestivalRole.Administrator, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanTransferOwnershipAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking ownership transfer capability for user {UserId} on festival {FestivalId}",
            userId, festivalId);
        
        // Only owner can transfer ownership
        return await _permissionRepository.HasRoleOrHigherAsync(userId, festivalId, FestivalRole.Owner, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanPublishScheduleAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking schedule publish capability for user {UserId} on festival {FestivalId}",
            userId, festivalId);
        
        // Administrator/Owner or user with Schedule scope can publish
        return await _permissionRepository.HasScopeAsync(userId, festivalId, PermissionScope.Schedule, ct);
    }

    /// <inheritdoc />
    public async Task<bool> CanViewAnalyticsAsync(long userId, long festivalId, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Checking analytics view capability for user {UserId} on festival {FestivalId}",
            userId, festivalId);
        
        // Any team member can view analytics (Viewer role or higher)
        return await _permissionRepository.HasRoleOrHigherAsync(userId, festivalId, FestivalRole.Viewer, ct);
    }
}
