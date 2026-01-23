using FestConnect.Domain.Entities;

namespace FestConnect.DataAccess.Abstractions;

/// <summary>
/// Repository interface for TimeSlot data access operations.
/// </summary>
public interface ITimeSlotRepository
{
    /// <summary>
    /// Gets a time slot by its unique identifier.
    /// </summary>
    Task<TimeSlot?> GetByIdAsync(long timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets multiple time slots by their unique identifiers.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByIdsAsync(IEnumerable<long> timeSlotIds, CancellationToken ct = default);

    /// <summary>
    /// Gets all time slots for a stage within an edition.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByStageAndEditionAsync(long stageId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets all time slots for an edition.
    /// </summary>
    Task<IReadOnlyList<TimeSlot>> GetByEditionAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new time slot.
    /// </summary>
    Task<long> CreateAsync(TimeSlot timeSlot, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing time slot.
    /// </summary>
    Task UpdateAsync(TimeSlot timeSlot, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a time slot.
    /// </summary>
    Task DeleteAsync(long timeSlotId, long deletedBy, CancellationToken ct = default);

    /// <summary>
    /// Checks if a time slot exists.
    /// </summary>
    Task<bool> ExistsAsync(long timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the edition ID for a time slot.
    /// </summary>
    Task<long?> GetEditionIdAsync(long timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the festival ID for a time slot (through stage/venue chain).
    /// </summary>
    Task<long?> GetFestivalIdAsync(long timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Checks for overlapping time slots on a stage.
    /// </summary>
    Task<bool> HasOverlapAsync(long stageId, long editionId, DateTime startTimeUtc, DateTime endTimeUtc, long? excludeTimeSlotId = null, CancellationToken ct = default);
}
