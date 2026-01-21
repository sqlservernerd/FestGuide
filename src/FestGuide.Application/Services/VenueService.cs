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
/// Venue and stage service implementation.
/// </summary>
public class VenueService : IVenueService
{
    private readonly IVenueRepository _venueRepository;
    private readonly IStageRepository _stageRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly IFestivalRepository _festivalRepository;
    private readonly IFestivalAuthorizationService _authorizationService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<VenueService> _logger;

    public VenueService(
        IVenueRepository venueRepository,
        IStageRepository stageRepository,
        IEditionRepository editionRepository,
        IFestivalRepository festivalRepository,
        IFestivalAuthorizationService authorizationService,
        IDateTimeProvider dateTimeProvider,
        ILogger<VenueService> logger)
    {
        _venueRepository = venueRepository ?? throw new ArgumentNullException(nameof(venueRepository));
        _stageRepository = stageRepository ?? throw new ArgumentNullException(nameof(stageRepository));
        _editionRepository = editionRepository ?? throw new ArgumentNullException(nameof(editionRepository));
        _festivalRepository = festivalRepository ?? throw new ArgumentNullException(nameof(festivalRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<VenueDto> GetByIdAsync(Guid venueId, CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId, ct)
            ?? throw new VenueNotFoundException(venueId);

        return VenueDto.FromEntity(venue);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VenueSummaryDto>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default)
    {
        var venues = await _venueRepository.GetByFestivalAsync(festivalId, ct);
        return venues.Select(VenueSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VenueSummaryDto>> GetByEditionAsync(Guid editionId, CancellationToken ct = default)
    {
        var venues = await _venueRepository.GetByEditionAsync(editionId, ct);
        return venues.Select(VenueSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<VenueDto> CreateAsync(Guid festivalId, Guid userId, CreateVenueRequest request, CancellationToken ct = default)
    {
        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to create venues for this festival.");
        }

        if (!await _festivalRepository.ExistsAsync(festivalId, ct))
        {
            throw new FestivalNotFoundException(festivalId);
        }

        var now = _dateTimeProvider.UtcNow;
        var venue = new Venue
        {
            VenueId = Guid.NewGuid(),
            FestivalId = festivalId,
            Name = request.Name,
            Description = request.Description,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _venueRepository.CreateAsync(venue, ct);

        _logger.LogInformation("Venue {VenueId} created for festival {FestivalId} by user {UserId}",
            venue.VenueId, festivalId, userId);

        return VenueDto.FromEntity(venue);
    }

    /// <inheritdoc />
    public async Task<VenueDto> UpdateAsync(Guid venueId, Guid userId, UpdateVenueRequest request, CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId, ct)
            ?? throw new VenueNotFoundException(venueId);

        if (!await _authorizationService.HasScopeAsync(userId, venue.FestivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this venue.");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            venue.Name = request.Name;
        }

        if (request.Description != null)
        {
            venue.Description = request.Description;
        }

        if (request.Address != null)
        {
            venue.Address = request.Address;
        }

        if (request.Latitude.HasValue)
        {
            venue.Latitude = request.Latitude;
        }

        if (request.Longitude.HasValue)
        {
            venue.Longitude = request.Longitude;
        }

        venue.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        venue.ModifiedBy = userId;

        await _venueRepository.UpdateAsync(venue, ct);

        _logger.LogInformation("Venue {VenueId} updated by user {UserId}", venueId, userId);

        return VenueDto.FromEntity(venue);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid venueId, Guid userId, CancellationToken ct = default)
    {
        var venue = await _venueRepository.GetByIdAsync(venueId, ct)
            ?? throw new VenueNotFoundException(venueId);

        if (!await _authorizationService.HasScopeAsync(userId, venue.FestivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this venue.");
        }

        await _venueRepository.DeleteAsync(venueId, userId, ct);

        _logger.LogInformation("Venue {VenueId} deleted by user {UserId}", venueId, userId);
    }

    /// <inheritdoc />
    public async Task AddVenueToEditionAsync(Guid editionId, Guid venueId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _editionRepository.GetFestivalIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to manage venues for this edition.");
        }

        if (!await _venueRepository.ExistsAsync(venueId, ct))
        {
            throw new VenueNotFoundException(venueId);
        }

        await _venueRepository.AddToEditionAsync(editionId, venueId, userId, ct);

        _logger.LogInformation("Venue {VenueId} added to edition {EditionId} by user {UserId}",
            venueId, editionId, userId);
    }

    /// <inheritdoc />
    public async Task RemoveVenueFromEditionAsync(Guid editionId, Guid venueId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _editionRepository.GetFestivalIdAsync(editionId, ct)
            ?? throw new EditionNotFoundException(editionId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to manage venues for this edition.");
        }

        await _venueRepository.RemoveFromEditionAsync(editionId, venueId, ct);

        _logger.LogInformation("Venue {VenueId} removed from edition {EditionId} by user {UserId}",
            venueId, editionId, userId);
    }

    /// <inheritdoc />
    public async Task<StageDto> GetStageByIdAsync(Guid stageId, CancellationToken ct = default)
    {
        var stage = await _stageRepository.GetByIdAsync(stageId, ct)
            ?? throw new StageNotFoundException(stageId);

        return StageDto.FromEntity(stage);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StageSummaryDto>> GetStagesByVenueAsync(Guid venueId, CancellationToken ct = default)
    {
        var stages = await _stageRepository.GetByVenueAsync(venueId, ct);
        return stages.Select(StageSummaryDto.FromEntity).ToList();
    }

    /// <inheritdoc />
    public async Task<StageDto> CreateStageAsync(Guid venueId, Guid userId, CreateStageRequest request, CancellationToken ct = default)
    {
        var festivalId = await _venueRepository.GetFestivalIdAsync(venueId, ct)
            ?? throw new VenueNotFoundException(venueId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to create stages for this venue.");
        }

        var now = _dateTimeProvider.UtcNow;
        var stage = new Stage
        {
            StageId = Guid.NewGuid(),
            VenueId = venueId,
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder,
            IsDeleted = false,
            CreatedAtUtc = now,
            CreatedBy = userId,
            ModifiedAtUtc = now,
            ModifiedBy = userId
        };

        await _stageRepository.CreateAsync(stage, ct);

        _logger.LogInformation("Stage {StageId} created for venue {VenueId} by user {UserId}",
            stage.StageId, venueId, userId);

        return StageDto.FromEntity(stage);
    }

    /// <inheritdoc />
    public async Task<StageDto> UpdateStageAsync(Guid stageId, Guid userId, UpdateStageRequest request, CancellationToken ct = default)
    {
        var stage = await _stageRepository.GetByIdAsync(stageId, ct)
            ?? throw new StageNotFoundException(stageId);

        var festivalId = await _stageRepository.GetFestivalIdAsync(stageId, ct);
        if (festivalId == null || !await _authorizationService.HasScopeAsync(userId, festivalId.Value, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to edit this stage.");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            stage.Name = request.Name;
        }

        if (request.Description != null)
        {
            stage.Description = request.Description;
        }

        if (request.SortOrder.HasValue)
        {
            stage.SortOrder = request.SortOrder.Value;
        }

        stage.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        stage.ModifiedBy = userId;

        await _stageRepository.UpdateAsync(stage, ct);

        _logger.LogInformation("Stage {StageId} updated by user {UserId}", stageId, userId);

        return StageDto.FromEntity(stage);
    }

    /// <inheritdoc />
    public async Task DeleteStageAsync(Guid stageId, Guid userId, CancellationToken ct = default)
    {
        var festivalId = await _stageRepository.GetFestivalIdAsync(stageId, ct)
            ?? throw new StageNotFoundException(stageId);

        if (!await _authorizationService.HasScopeAsync(userId, festivalId, PermissionScope.Venues, ct))
        {
            throw new ForbiddenException("You do not have permission to delete this stage.");
        }

        await _stageRepository.DeleteAsync(stageId, userId, ct);

        _logger.LogInformation("Stage {StageId} deleted by user {UserId}", stageId, userId);
    }
}
