using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Artist data access operations.
/// </summary>
public interface IArtistRepository
{
    /// <summary>
    /// Gets an artist by its unique identifier.
    /// </summary>
    Task<Artist?> GetByIdAsync(long artistId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple artists by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<Artist>> GetByIdsAsync(IEnumerable<long> artistIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all artists for a festival.
    /// </summary>
    Task<IReadOnlyList<Artist>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Searches artists by name within a festival.
    /// </summary>
    Task<IReadOnlyList<Artist>> SearchByNameAsync(long festivalId, string searchTerm, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new artist.
    /// </summary>
    Task<long> CreateAsync(Artist artist, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing artist.
    /// </summary>
    Task UpdateAsync(Artist artist, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes an artist.
    /// </summary>
    Task DeleteAsync(long artistId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if an artist exists.
    /// </summary>
    Task<bool> ExistsAsync(long artistId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for an artist.
    /// </summary>
    Task<long?> GetFestivalIdAsync(long artistId, CancellationToken ct = default);
}
