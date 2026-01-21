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
    Task<VenueDto> GetByIdAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues for a festival.
    /// </summary>
    Task<IReadOnlyList<VenueSummaryDto>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues associated with an edition.
    /// </summary>
    Task<IReadOnlyList<VenueSummaryDto>> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new venue.
    /// </summary>
    Task<VenueDto> CreateAsync(Guid festivalId, Guid userId, CreateVenueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing venue.
    /// </summary>
    Task<VenueDto> UpdateAsync(Guid venueId, Guid userId, UpdateVenueRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a venue.
    /// </summary>
    Task DeleteAsync(Guid venueId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Associates a venue with an edition.
    /// </summary>
    Task AddVenueToEditionAsync(Guid editionId, Guid venueId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Removes a venue association from an edition.
    /// </summary>
    Task RemoveVenueFromEditionAsync(Guid editionId, Guid venueId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a stage by ID.
    /// </summary>
    Task<StageDto> GetStageByIdAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for a venue.
    /// </summary>
    Task<IReadOnlyList<StageSummaryDto>> GetStagesByVenueAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new stage.
    /// </summary>
    Task<StageDto> CreateStageAsync(Guid venueId, Guid userId, CreateStageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing stage.
    /// </summary>
    Task<StageDto> UpdateStageAsync(Guid stageId, Guid userId, UpdateStageRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a stage.
    /// </summary>
    Task DeleteStageAsync(Guid stageId, Guid userId, CancellationToken ct = default);
}
