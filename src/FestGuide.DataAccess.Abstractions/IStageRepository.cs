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
    Task<Stage?> GetByIdAsync(long stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple stages by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByIdsAsync(IEnumerable<long> stageIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for a venue.
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByVenueAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all stages for an edition (through venue associations).
    /// </summary>
    Task<IReadOnlyList<Stage>> GetByEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new stage.
    /// </summary>
    Task<long> CreateAsync(Stage stage, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing stage.
    /// </summary>
    Task UpdateAsync(Stage stage, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a stage.
    /// </summary>
    Task DeleteAsync(long stageId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a stage exists.
    /// </summary>
    Task<bool> ExistsAsync(long stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets the venue ID for a stage.
    /// </summary>
    Task<long?> GetVenueIdAsync(long stageId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a stage (through venue).
    /// </summary>
    Task<long?> GetFestivalIdAsync(long stageId, CancellationToken ct = default);
}
