using FestGuide.Domain.Entities;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for time slot.
/// </summary>
public sealed record TimeSlotDto(
    long TimeSlotId,
    long StageId,
    long EditionId,
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
    long EditionId,
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
    long EngagementId,
    long TimeSlotId,
    long ArtistId,
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
    long ArtistId,
    string? Notes);

/// <summary>
/// Request DTO for updating an engagement.
/// </summary>
public sealed record UpdateEngagementRequest(
    long? ArtistId,
    string? Notes);

/// <summary>
/// Response DTO for schedule.
/// </summary>
public sealed record ScheduleDto(
    long ScheduleId,
    long EditionId,
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
    long ScheduleId,
    long EditionId,
    int Version,
    DateTime? PublishedAtUtc,
    bool IsPublished,
    IReadOnlyList<ScheduleItemDto> Items);

/// <summary>
/// Combined time slot and engagement info for schedule display.
/// </summary>
public sealed record ScheduleItemDto(
    long TimeSlotId,
    long StageId,
    string StageName,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    long? EngagementId,
    long? ArtistId,
    string? ArtistName,
    string? Notes);
