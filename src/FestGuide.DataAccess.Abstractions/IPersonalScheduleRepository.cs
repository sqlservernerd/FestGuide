using FestGuide.Domain.Entities;

namespace FestGuide.DataAccess.Abstractions;

/// <summary>
/// Repository interface for PersonalSchedule data access operations.
/// </summary>
public interface IPersonalScheduleRepository
{
    /// <summary>
    /// Gets a personal schedule by its unique identifier.
    /// </summary>
    Task<PersonalSchedule?> GetByIdAsync(Guid personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user and edition.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByUserAndEditionAsync(Guid userId, Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the default personal schedule for a user and edition.
    /// </summary>
    Task<PersonalSchedule?> GetDefaultAsync(Guid userId, Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new personal schedule.
    /// </summary>
    Task<Guid> CreateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing personal schedule.
    /// </summary>
    Task UpdateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a personal schedule.
    /// </summary>
    Task DeleteAsync(Guid personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a personal schedule exists.
    /// </summary>
    Task<bool> ExistsAsync(Guid personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all entries for a personal schedule.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleEntry>> GetEntriesAsync(Guid personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets entries for multiple personal schedules in a single batch query.
    /// </summary>
    /// <param name="personalScheduleIds">The IDs of the personal schedules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary mapping schedule IDs to their entries.</returns>
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<PersonalScheduleEntry>>> GetEntriesByScheduleIdsAsync(IEnumerable<Guid> personalScheduleIds, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific entry by ID.
    /// </summary>
    Task<PersonalScheduleEntry?> GetEntryByIdAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Adds an entry to a personal schedule.
    /// </summary>
    Task<Guid> AddEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Updates an entry in a personal schedule.
    /// </summary>
    Task UpdateEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Removes an entry from a personal schedule.
    /// </summary>
    Task RemoveEntryAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Checks if an engagement is already in a personal schedule.
    /// </summary>
    Task<bool> HasEngagementAsync(Guid personalScheduleId, Guid engagementId, CancellationToken ct = default);

    /// <summary>
    /// Gets the personal schedule ID for an entry.
    /// </summary>
    Task<Guid?> GetScheduleIdForEntryAsync(Guid entryId, CancellationToken ct = default);

    /// <summary>
    /// Updates the last synced timestamp.
    /// </summary>
    Task UpdateLastSyncedAsync(Guid personalScheduleId, DateTime syncedAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for an edition (across all users).
    /// Used for sending notifications to all attendees of an edition.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByEditionAsync(Guid editionId, int limit = 1000, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Gets user IDs who have a specific engagement saved in their personal schedules.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUserIdsWithEngagementAsync(Guid engagementId, CancellationToken ct = default);
}
