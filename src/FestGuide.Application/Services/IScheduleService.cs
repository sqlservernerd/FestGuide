using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Service interface for schedule operations (time slots, engagements, publishing).
/// </summary>
public interface IScheduleService
{
    /// <summary>
    /// Gets the schedule for an edition.
    /// </summary>
    Task<ScheduleDto> GetScheduleAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Gets the detailed schedule for an edition including all time slots and engagements.
    /// </summary>
    Task<ScheduleDetailDto> GetScheduleDetailAsync(long editionId, CancellationToken ct = default);

    /// <summary>
    /// Publishes a schedule, making it visible to attendees.
    /// </summary>
    Task<ScheduleDto> PublishScheduleAsync(long editionId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets a time slot by ID.
    /// </summary>
    Task<TimeSlotDto> GetTimeSlotByIdAsync(long timeSlotId, CancellationToken ct = default);

    /// <summary>
    /// Gets all time slots for a stage within an edition.
    /// </summary>
    Task<IReadOnlyList<TimeSlotDto>> GetTimeSlotsByStageAsync(long stageId, long editionId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new time slot.
    /// </summary>
    Task<TimeSlotDto> CreateTimeSlotAsync(long stageId, long userId, CreateTimeSlotRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing time slot.
    /// </summary>
    Task<TimeSlotDto> UpdateTimeSlotAsync(long timeSlotId, long userId, UpdateTimeSlotRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a time slot.
    /// </summary>
    Task DeleteTimeSlotAsync(long timeSlotId, long userId, CancellationToken ct = default);

    /// <summary>
    /// Gets an engagement by ID.
    /// </summary>
    Task<EngagementDto> GetEngagementByIdAsync(long engagementId, CancellationToken ct = default);

    /// <summary>
    /// Creates an engagement (assigns artist to time slot).
    /// </summary>
    Task<EngagementDto> CreateEngagementAsync(long timeSlotId, long userId, CreateEngagementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing engagement.
    /// </summary>
    Task<EngagementDto> UpdateEngagementAsync(long engagementId, long userId, UpdateEngagementRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes an engagement.
    /// </summary>
    Task DeleteEngagementAsync(long engagementId, long userId, CancellationToken ct = default);
}
