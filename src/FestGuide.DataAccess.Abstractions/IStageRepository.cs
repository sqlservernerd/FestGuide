using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Stage data access operations.
/// </summary>
public interface IStageRepository
{
    /// <summary>
    /// Gets a stage by its unique identifier.
    /// </summary>
    Task<Stage?> GetByIdAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple stages by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByIdsAsync(IEnumerable<Guid> stageIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for a venue.
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByVenueAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for an edition (through venue associations).
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new stage.
    /// </summary>
    Task<Guid> CreateAsync(Stage stage, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing stage.
    /// </summary>
    Task UpdateAsync(Stage stage, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a stage.
    /// </summary>
    Task DeleteAsync(Guid stageId, Guid deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a stage exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets the venue ID for a stage.
    /// </summary>
    Task<Guid?> GetVenueIdAsync(Guid stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a stage (through venue).
    /// </summary>
    Task<Guid?> GetFestivalIdAsync(Guid stageId, CancellationToken ct = default);
}
