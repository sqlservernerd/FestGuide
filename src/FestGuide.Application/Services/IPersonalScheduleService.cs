using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for personal schedule operations (attendee).
/// </summary>
public interface IPersonalScheduleService
{
    /// <summary>
    /// Gets all personal schedules for the current user.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleSummaryDto>> GetMySchedulesAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets all personal schedules for a user and edition.
    /// </summary>
    Task<IReadOnlyList<PersonalScheduleDto>> GetByEditionAsync(Guid userId, Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets a personal schedule by ID.
    /// </summary>
    Task<PersonalScheduleDto> GetByIdAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets the detailed schedule with all entries.
    /// </summary>
    Task<PersonalScheduleDetailDto> GetDetailAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new personal schedule.
    /// </summary>
    Task<PersonalScheduleDto> CreateAsync(Guid userId, CreatePersonalScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates a personal schedule.
    /// </summary>
    Task<PersonalScheduleDto> UpdateAsync(Guid personalScheduleId, Guid userId, UpdatePersonalScheduleRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a personal schedule.
    /// </summary>
    Task DeleteAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Gets or creates a default schedule for an edition.
    /// </summary>
    Task<PersonalScheduleDto> GetOrCreateDefaultAsync(Guid userId, Guid editionId, CancellationToken ct = default);

    /// <summary>
    /// Adds an entry (engagement) to a personal schedule.
    /// </summary>
    Task<PersonalScheduleEntryDto> AddEntryAsync(Guid personalScheduleId, Guid userId, AddScheduleEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an entry in a personal schedule.
    /// </summary>
    Task<PersonalScheduleEntryDto> UpdateEntryAsync(Guid entryId, Guid userId, UpdateScheduleEntryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Removes an entry from a personal schedule.
    /// </summary>
    Task RemoveEntryAsync(Guid entryId, Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Syncs the schedule and returns updated data (for offline support).
    /// </summary>
    Task<PersonalScheduleDetailDto> SyncAsync(Guid personalScheduleId, Guid userId, CancellationToken ct = default);
}
