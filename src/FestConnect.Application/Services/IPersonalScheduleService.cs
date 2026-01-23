using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for personal schedule operations (attendee).
/// </summary>
public interface IPersonalScheduleService
{
    /// <summary>
    /// Gets all personal schedules for the current user.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleSummaryDto>> GetMySchedulesAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user and edition.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleDto>> GetByEditionAsync(long userId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a personal schedule by ID.
    /// </summary>
    Task<PersonalScheduleDto> GetByIdAsync(long personalScheduleId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the detailed schedule with all entries.
    /// </summary>
    Task<PersonalScheduleDetailDto> GetDetailAsync(long personalScheduleId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new personal schedule.
    /// </summary>
    Task<PersonalScheduleDto> CreateAsync(long userId, CreatePersonalScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates a personal schedule.
    /// </summary>
    Task<PersonalScheduleDto> UpdateAsync(long personalScheduleId, long userId, UpdatePersonalScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a personal schedule.
    /// </summary>
    Task DeleteAsync(long personalScheduleId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates a default schedule for an edition.
    /// </summary>
    Task<PersonalScheduleDto> GetOrCreateDefaultAsync(long userId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Adds an entry (engagement) to a personal schedule.
    /// </summary>
    Task<PersonalScheduleEntryDto> AddEntryAsync(long personalScheduleId, long userId, AddScheduleEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an entry in a personal schedule.
    /// </summary>
    Task<PersonalScheduleEntryDto> UpdateEntryAsync(long entryId, long userId, UpdateScheduleEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes an entry from a personal schedule.
    /// </summary>
    Task RemoveEntryAsync(long entryId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Syncs the schedule and returns updated data (for offline support).
    /// </summary>
    Task<PersonalScheduleDetailDto> SyncAsync(long personalScheduleId, long userId, CancellationToken ct = default);
}
