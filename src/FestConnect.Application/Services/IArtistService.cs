using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for artist operations.
/// </summary>
public interface IArtistService
{
    /// <summary>
    /// Gets an artist by ID.
    /// </summary>
    Task<ArtistDto> GetByIdAsync(long artistId, CancellationToken ct = default);

    /// <summary>
    /// Gets all artists for a festival.
    /// </summary>
    Task<IReadOnlyList<ArtistSummaryDto>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Searches artists by name within a festival.
    /// </summary>
    Task<IReadOnlyList<ArtistSummaryDto>> SearchAsync(long festivalId, string searchTerm, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new artist.
    /// </summary>
    Task<ArtistDto> CreateAsync(long festivalId, long userId, CreateArtistRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing artist.
    /// </summary>
    Task<ArtistDto> UpdateAsync(long artistId, long userId, UpdateArtistRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an artist.
    /// </summary>
    Task DeleteAsync(long artistId, long userId, CancellationToken ct = default);
}
