using FestGuide.Domain.Entities;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for time slot.
/// </summary>
public sealed record TimeSlotDto(
    Guid TimeSlotId,
    Guid StageId,
    Guid EditionId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static TimeSlotDto FromEntity(TimeSlot timeSlot) =>
        new(
            timeSlot.TimeSlotId,
            timeSlot.StageId,
            timeSlot.EditionId,
            timeSlot.StartTimeUtc,
            timeSlot.EndTimeUtc,
            timeSlot.CreatedAtUtc,
            timeSlot.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating a time slot.
/// </summary>
public sealed record CreateTimeSlotRequest(
    Guid EditionId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc);

/// <summary>
/// Request DTO for updating a time slot.
/// </summary>
public sealed record UpdateTimeSlotRequest(
    DateTime? StartTimeUtc,
    DateTime? EndTimeUtc);

/// <summary>
/// Response DTO for engagement.
/// </summary>
public sealed record EngagementDto(
    Guid EngagementId,
    Guid TimeSlotId,
    Guid ArtistId,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static EngagementDto FromEntity(Engagement engagement) =>
        new(
            engagement.EngagementId,
            engagement.TimeSlotId,
            engagement.ArtistId,
            engagement.Notes,
            engagement.CreatedAtUtc,
            engagement.ModifiedAtUtc);
}

/// <summary>
/// Request DTO for creating an engagement.
/// </summary>
public sealed record CreateEngagementRequest(
    Guid ArtistId,
    string? Notes);

/// <summary>
/// Request DTO for updating an engagement.
/// </summary>
public sealed record UpdateEngagementRequest(
    Guid? ArtistId,
    string? Notes);

/// <summary>
/// Response DTO for schedule.
/// </summary>
public sealed record ScheduleDto(
    Guid ScheduleId,
    Guid EditionId,
    int Version,
    DateTime? PublishedAtUtc,
    bool IsPublished)
{
    public static ScheduleDto FromEntity(Schedule schedule) =>
        new(
            schedule.ScheduleId,
            schedule.EditionId,
            schedule.Version,
            schedule.PublishedAtUtc,
            schedule.IsPublished);
}

/// <summary>
/// Detailed schedule response including all time slots and engagements.
/// </summary>
public sealed record ScheduleDetailDto(
    Guid ScheduleId,
    Guid EditionId,
    int Version,
    DateTime? PublishedAtUtc,
    bool IsPublished,
    IReadOnlyList<ScheduleItemDto> Items);

/// <summary>
/// Combined time slot and engagement info for schedule display.
/// </summary>
public sealed record ScheduleItemDto(
    Guid TimeSlotId,
    Guid StageId,
    string StageName,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    Guid? EngagementId,
    Guid? ArtistId,
    string? ArtistName,
    string? Notes);
