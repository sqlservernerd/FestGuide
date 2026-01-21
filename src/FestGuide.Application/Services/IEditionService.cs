using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for edition operations.
/// </summary>
public interface IEditionService
{
    /// <summary>
    /// Gets an edition by ID.
    /// </summary>
    Task<EditionDto> GetByIdAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all editions for a festival.
    /// </summary>
    Task<IReadOnlyList<EditionSummaryDto>> GetByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Gets published editions for a festival (attendee view).
    /// </summary>
    Task<IReadOnlyList<EditionSummaryDto>> GetPublishedByFestivalAsync(Guid festivalId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new edition.
    /// </summary>
    Task<EditionDto> CreateAsync(Guid festivalId, Guid userId, CreateEditionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing edition.
    /// </summary>
    Task<EditionDto> UpdateAsync(Guid editionId, Guid userId, UpdateEditionRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an edition.
    /// </summary>
    Task DeleteAsync(Guid editionId, Guid userId, CancellationToken ct = default);
}
