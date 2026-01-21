using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Venue data access operations.
/// </summary>
public interface IVenueRepository
{
    /// <summary>
    /// Gets a venue by its unique identifier.
    /// </summary>
    Task<Venue?> GetByIdAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues for a festival.
    /// </summary>
    Task<IReadOnlyList<Venue>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets all venues associated with an edition.
    /// </summary>
    Task<IReadOnlyList<Venue>> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new venue.
    /// </summary>
    Task<Guid> CreateAsync(Venue venue, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing venue.
    /// </summary>
    Task UpdateAsync(Venue venue, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a venue.
    /// </summary>
    Task DeleteAsync(Guid venueId, Guid deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a venue exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a venue.
    /// </summary>
    Task<Guid?> GetFestivalIdAsync(Guid venueId, CancellationToken ct = default);

    /// <summary>
    /// Associates a venue with an edition.
    /// </summary>
    Task AddToEditionAsync(Guid editionId, Guid venueId, Guid createdBy, CancellationToken ct = default);

    /// <summary>
    /// Removes a venue association from an edition.
    /// </summary>
    Task RemoveFromEditionAsync(Guid editionId, Guid venueId, CancellationToken ct = default);
}
