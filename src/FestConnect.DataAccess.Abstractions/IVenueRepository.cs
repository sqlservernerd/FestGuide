using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Venue data access operations.
/// </summary>
public interface IVenueRepository
{
    /// <summary>
    /// Gets a venue by its unique identifier.
    /// </summary>
    Task<Venue?> GetByIdAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues for a festival.
    /// </summary>
    Task<IReadOnlyList<Venue>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues associated with an edition.
    /// </summary>
    Task<IReadOnlyList<Venue>> GetByEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new venue.
    /// </summary>
    Task<long> CreateAsync(Venue venue, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing venue.
    /// </summary>
    Task UpdateAsync(Venue venue, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a venue.
    /// </summary>
    Task DeleteAsync(long venueId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a venue exists.
    /// </summary>
    Task<bool> ExistsAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a venue.
    /// </summary>
    Task<long?> GetFestivalIdAsync(long venueId, CancellationToken ct = default);

    /// <summary>
    /// Associates a venue with an edition.
    /// </summary>
    Task AddToEditionAsync(long editionId, long venueId, long createdBy, CancellationToken ct = default);

    /// <summary>
    /// Removes a venue association from an edition.
    /// </summary>
    Task RemoveFromEditionAsync(long editionId, long venueId, CancellationToken ct = default);
}
