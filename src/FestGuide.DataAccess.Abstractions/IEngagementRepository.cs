using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for Engagement data access operations.
/// </summary>
public interface IEngagementRepository
{
    /// <summary>
    /// Gets an engagement by its unique identifier.
    /// </summary>
    Task<Engagement?> GetByIdAsync(Guid engagementId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple engagements by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<Engagement>> GetByIdsAsync(IEnumerable<Guid> engagementIds, CancellationToken ct = default);

    /// <summary>
    /// Gets the engagement for a time slot.
    /// </summary>
    Task<Engagement?> GetByTimeSlotAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets all engagements for an edition.
    /// </summary>
    Task<IReadOnlyList<Engagement>> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all engagements for an artist.
    /// </summary>
    Task<IReadOnlyList<Engagement>> GetByArtistAsync(Guid artistId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new engagement.
    /// </summary>
    Task<Guid> CreateAsync(Engagement engagement, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing engagement.
    /// </summary>
    Task UpdateAsync(Engagement engagement, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes an engagement.
    /// </summary>
    Task DeleteAsync(Guid engagementId, Guid deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if an engagement exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid engagementId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a time slot already has an engagement.
    /// </summary>
    Task<bool> TimeSlotHasEngagementAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for an engagement (through time slot chain).
    /// </summary>
    Task<Guid?> GetFestivalIdAsync(Guid engagementId, CancellationToken ct = default);
}
