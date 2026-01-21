using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for TimeSlot data access operations.
/// </summary>
public interface ITimeSlotRepository
{
    /// <summary>
    /// Gets a time slot by its unique identifier.
    /// </summary>
    Task<TimeSlot?> GetByIdAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple time slots by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByIdsAsync(IEnumerable<Guid> timeSlotIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all time slots for a stage within an edition.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByStageAndEditionAsync(Guid stageId, Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all time slots for an edition.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByEditionAsync(Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new time slot.
    /// </summary>
    Task<Guid> CreateAsync(TimeSlot timeSlot, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing time slot.
    /// </summary>
    Task UpdateAsync(TimeSlot timeSlot, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a time slot.
    /// </summary>
    Task DeleteAsync(Guid timeSlotId, Guid deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a time slot exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the edition ID for a time slot.
    /// </summary>
    Task<Guid?> GetEditionIdAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a time slot (through stage/venue chain).
    /// </summary>
    Task<Guid?> GetFestivalIdAsync(Guid timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Checks for overlapping time slots on a stage.
    /// </summary>
    Task<bool> HasOverlapAsync(Guid stageId, Guid editionId, DateTime startTimeUtc, DateTime endTimeUtc, Guid? excludeTimeSlotId = null, CancellationToken ct = default);
}
