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
    Task<PersonalSchedule?> GetByIdAsync(long personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByUserAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user and edition.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByUserAndEditionAsync(long userId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the default personal schedule for a user and edition.
    /// </summary>
    Task<PersonalSchedule?> GetDefaultAsync(long userId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new personal schedule.
    /// </summary>
    Task<long> CreateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing personal schedule.
    /// </summary>
    Task UpdateAsync(PersonalSchedule personalSchedule, CancellationToken ct = default);

    /// <summary>
    /// Soft-deletes a personal schedule.
    /// </summary>
    Task DeleteAsync(long personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Checks if a personal schedule exists.
    /// </summary>
    Task<bool> ExistsAsync(long personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets all entries for a personal schedule.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleEntry>> GetEntriesAsync(long personalScheduleId, CancellationToken ct = default);

    /// <summary>
    /// Gets entries for multiple personal schedules in a single batch query.
    /// </summary>
    /// <param name="personalScheduleIds">The IDs of the personal schedules.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A dictionary mapping schedule IDs to their entries.</returns>
    Task<IReadOnlyDictionary<long, IReadOnlyList<PersonalScheduleEntry>>> GetEntriesByScheduleIdsAsync(IEnumerable<long> personalScheduleIds, CancellationToken ct = default);

    /// <summary>
    /// Gets a specific entry by ID.
    /// </summary>
    Task<PersonalScheduleEntry?> GetEntryByIdAsync(long entryId, CancellationToken ct = default);

    /// <summary>
    /// Adds an entry to a personal schedule.
    /// </summary>
    Task<long> AddEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Updates an entry in a personal schedule.
    /// </summary>
    Task UpdateEntryAsync(PersonalScheduleEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Removes an entry from a personal schedule.
    /// </summary>
    Task RemoveEntryAsync(long entryId, CancellationToken ct = default);

    /// <summary>
    /// Checks if an engagement is already in a personal schedule.
    /// </summary>
    Task<bool> HasEngagementAsync(long personalScheduleId, long engagementId, CancellationToken ct = default);

    /// <summary>
    /// Gets the personal schedule ID for an entry.
    /// </summary>
    Task<long?> GetScheduleIdForEntryAsync(long entryId, CancellationToken ct = default);

    /// <summary>
    /// Updates the last synced timestamp.
    /// </summary>
    Task UpdateLastSyncedAsync(long personalScheduleId, DateTime syncedAtUtc, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for an edition (across all users).
    /// Used for sending notifications to all attendees of an edition.
    /// </summary>
    Task<IReadOnlyList<PersonalSchedule>> GetByEditionAsync(long editionId, int limit = 1000, int offset = 0, CancellationToken ct = default);

    /// <summary>
    /// Gets user IDs who have a specific engagement saved in their personal schedules.
    /// </summary>
    Task<IReadOnlyList<long>> GetUserIdsWithEngagementAsync(long engagementId, CancellationToken ct = default);
}
