using FestConnect.Application.Authorization;
using FestConnect.Application.Dtos;
using FestConnect.DataAccess.Abstractions;
using FestConnect.Domain.Entities;
using FestConnect.Domain.Enums;
using FestConnect.Domain.Exceptions;
using FestConnect.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestConnect.Application.Services;

/// <summary>
/// Festival service implementation.
/// </summary>
public class FestivalService : IFestivalService
{
    private readonly IFestivalRepository _festivalRepository;
    private readonly IFestivalPermissionRepository _permissionRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<FestivalService> _logger;

    public FestivalService(
        IFestivalRepository festivalRepository,
        IFestivalPermissionRepository permissionRepository,
        IFestivalAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<FestivalService> logger)
    {
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<FestivalDto> GetByIdAsync(long festivalId, CancellationToken ct = default)
    {
        var festival = await _festivalRepository.GetByIdAsync(festivalId, ct)
            ?? throw new FestivalNotFoundException(festivalId);

        return FestivalDto.FromEntity(festival);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalSummaryDto>> GetMyFestivalsAsync(long userId, CancellationToken ct = default)
    {
        var festivals = await _festivalRepository.GetByUserAccessAsync(userId, ct);
        return festivals.Select(f => FestivalSummaryDto.FromEntity(f, userId)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FestivalSummaryDto>> SearchAsync(string searchTerm, long? userId = null, int limit = 20, CancellationToken ct = default)
    {
        var festivals = await _festivalRepository.SearchByNameAsync(searchTerm, limit, ct);
        return festivals.Select(f => FestivalSummaryDto.FromEntity(f, userId ?? 0)).ToList();
    }

    /// <inheritdoc />
    public async Task<FestivalDto> CreateAsync(long userId, CreateFestivalRequest request, CancellationToken ct = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var festival = new Festival
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            WebsiteUrl = request.WebsiteUrl,
            OwnerUserId = userId,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _festivalRepository.CreateAsync(festival, ct);

        // Create owner permission
        var permission = new FestivalPermission
        {
            FestivalId = festival.FestivalId,
            UserId = userId,
            Role = FestivalRole.Owner,
            Scope = PermissionScope.All,
            IsPending = false,
            IsRevoked = false,
            AcceptedAtUtc = now,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _permissionRepository.CreateAsync(permission, ct);

        _logger.LogInformation("Festival {FestivalId} created by user {UserId}", festival.FestivalId, userId);

        return FestivalDto.FromEntity(festival);
    }

    /// <inheritdoc />
    public async Task<FestivalDto> UpdateAsync(long festivalId, long userId, UpdateFestivalRequest request, CancellationToken ct = default)
    {
        if (!await _authorizationService.CanEditFestivalAsync(userId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this festival.");
        }

        var festival = await _festivalRepository.GetByIdAsync(festivalId, ct)
            ?? throw new FestivalNotFoundException(festivalId);

        if (!string.IsNullOrEmpty(request.Name))
        {
            festival.Name = request.Name;
        }

        if (request.Description != null)
        {
            festival.Description = request.Description;
        }

        if (request.ImageUrl != null)
        {
            festival.ImageUrl = request.ImageUrl;
        }

        if (request.WebsiteUrl != null)
        {
            festival.WebsiteUrl = request.WebsiteUrl;
        }

        festival.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        festival.ModifiedBy = userId;

        await _festivalRepository.UpdateAsync(festival, ct);

        _logger.LogInformation("Festival {FestivalId} updated by user {UserId}", festivalId, userId);

        return FestivalDto.FromEntity(festival);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long festivalId, long userId, CancellationToken ct = default)
    {
        if (!await _authorizationService.CanDeleteFestivalAsync(userId, festivalId, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this festival.");
        }

        if (!await _festivalRepository.ExistsAsync(festivalId, ct))
        {
            throw new FestivalNotFoundException(festivalId);
        }

        await _festivalRepository.DeleteAsync(festivalId, userId, ct);

        _logger.LogInformation("Festival {FestivalId} deleted by user {UserId}", festivalId, userId);
    }

    /// <inheritdoc />
    public async Task TransferOwnershipAsync(long festivalId, long currentUserId, TransferOwnershipRequest request, CancellationToken ct = default)
    {
        var festival = await _festivalRepository.GetByIdAsync(festivalId, ct)
            ?? throw new FestivalNotFoundException(festivalId);

        if (festival.OwnerUserId != currentUserId)
        {
            throw new ForbiddenException("Only the owner can transfer ownership.");
        }

        await _festivalRepository.TransferOwnershipAsync(festivalId, request.NewOwnerUserId, currentUserId, ct);

        // Update permissions
        await _permissionRepository.TransferOwnershipAsync(festivalId, currentUserId, request.NewOwnerUserId, ct);

        _logger.LogInformation("Festival {FestivalId} ownership transferred from {OldOwner} to {NewOwner}",
            festivalId, currentUserId, request.NewOwnerUserId);
    }
}
