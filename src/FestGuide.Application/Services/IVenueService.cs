using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for venue and stage operations.
/// </summary>
public interface IVenueService
{
    /// <summary>
    /// Gets a venue by ID.
    /// </summary>
    Task<VenueDto> GetByIdAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues for a festival.
    /// </summary>
    Task<IReadOnlyList<VenueSummaryDto>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues associated with an edition.
    /// </summary>
    Task<IReadOnlyList<VenueSummaryDto>> GetByEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new venue.
    /// </summary>
    Task<VenueDto> CreateAsync(long festivalId, long userId, CreateVenueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing venue.
    /// </summary>
    Task<VenueDto> UpdateAsync(long venueId, long userId, UpdateVenueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a venue.
    /// </summary>
    Task DeleteAsync(long venueId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Associates a venue with an edition.
    /// </summary>
    Task AddVenueToEditionAsync(long editionId, long venueId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Removes a venue association from an edition.
    /// </summary>
    Task RemoveVenueFromEditionAsync(long editionId, long venueId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a stage by ID.
    /// </summary>
    Task<StageDto> GetStageByIdAsync(long stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for a venue.
    /// </summary>
    Task<IReadOnlyList<StageSummaryDto>> GetStagesByVenueAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new stage.
    /// </summary>
    Task<StageDto> CreateStageAsync(long venueId, long userId, CreateStageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing stage.
    /// </summary>
    Task<StageDto> UpdateStageAsync(long stageId, long userId, UpdateStageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a stage.
    /// </summary>
    Task DeleteStageAsync(long stageId, long userId, CancellationToken ct = default);
}
