using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for artist operations.
/// </summary>
public interface IArtistService
{
    /// <summary>
    /// Gets an artist by ID.
    /// </summary>
    Task<ArtistDto> GetByIdAsync(Guid artistId, CancellationToken ct = default);

    /// <summary>
    /// Gets all artists for a festival.
    /// </summary>
    Task<IReadOnlyList<ArtistSummaryDto>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Searches artists by name within a festival.
    /// </summary>
    Task<IReadOnlyList<ArtistSummaryDto>> SearchAsync(Guid festivalId, string searchTerm, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Creates a new artist.
    /// </summary>
    Task<ArtistDto> CreateAsync(Guid festivalId, Guid userId, CreateArtistRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing artist.
    /// </summary>
    Task<ArtistDto> UpdateAsync(Guid artistId, Guid userId, UpdateArtistRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an artist.
    /// </summary>
    Task DeleteAsync(Guid artistId, Guid userId, CancellationToken ct = default);
}
