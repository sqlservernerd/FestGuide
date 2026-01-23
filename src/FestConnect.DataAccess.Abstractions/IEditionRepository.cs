using FestConnect.Domain.Entities;
using FestConnect.Domain.Enums;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for FestivalEdition data access operations.
/// </summary>
public interface IEditionRepository
{
    /// <summary>
    /// Gets an edition by its unique identifier.
    /// </summary>
    Task<FestivalEdition?> GetByIdAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple editions by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<FestivalEdition>> GetByIdsAsync(IEnumerable<long> editionIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all editions for a festival.
    /// </summary>
    Task<IReadOnlyList<FestivalEdition>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets published editions for a festival (for attendee view).
    /// </summary>
    Task<IReadOnlyList<FestivalEdition>> GetPublishedByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets current and recent editions (within archive window).
    /// </summary>
    Task<IReadOnlyList<FestivalEdition>> GetCurrentAndRecentAsync(long festivalId, int archiveMonths = 3, CancellationToken ct = default);

    /// <summary>
    /// Creates a new edition.
    /// </summary>
    Task<long> CreateAsync(FestivalEdition edition, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing edition.
    /// </summary>
    Task UpdateAsync(FestivalEdition edition, CancellationToken ct = default);

    /// <summary>
    /// Updates the status of an edition.
    /// </summary>
    Task UpdateStatusAsync(long editionId, EditionStatus status, long modifiedBy, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes an edition.
    /// </summary>
    Task DeleteAsync(long editionId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if an edition exists.
    /// </summary>
    Task<bool> ExistsAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for an edition.
    /// </summary>
    Task<long?> GetFestivalIdAsync(long editionId, CancellationToken ct = default);
}
