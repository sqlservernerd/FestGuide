using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for edition operations.
/// </summary>
public interface IEditionService
{
    /// <summary>
    /// Gets an edition by ID.
    /// </summary>
    Task<EditionDto> GetByIdAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all editions for a festival.
    /// </summary>
    Task<IReadOnlyList<EditionSummaryDto>> GetByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets published editions for a festival (attendee view).
    /// </summary>
    Task<IReadOnlyList<EditionSummaryDto>> GetPublishedByFestivalAsync(long festivalId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new edition.
    /// </summary>
    Task<EditionDto> CreateAsync(long festivalId, long userId, CreateEditionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing edition.
    /// </summary>
    Task<EditionDto> UpdateAsync(long editionId, long userId, UpdateEditionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an edition.
    /// </summary>
    Task DeleteAsync(long editionId, long userId, CancellationToken ct = default);
}
