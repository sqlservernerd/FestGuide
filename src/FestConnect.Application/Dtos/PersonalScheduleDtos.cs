using FestConnect.Domain.Entities;

namespace FestConnect.Application.Dtos;

/// <summary>
/// Response DTO for personal schedule.
/// </summary>
public sealed record PersonalScheduleDto(
    long PersonalScheduleId,
    long UserId,
    long EditionId,
    string? Name,
    bool IsDefault,
    int EntryCount,
    DateTime? LastSyncedAtUtc,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc)
{
    public static PersonalScheduleDto FromEntity(PersonalSchedule schedule, int entryCount = 0) =>
        new(
            schedule.PersonalScheduleId,
            schedule.UserId,
            schedule.EditionId,
            schedule.Name,
            schedule.IsDefault,
            entryCount,
            schedule.LastSyncedAtUtc,
            schedule.CreatedAtUtc,
            schedule.ModifiedAtUtc);
}

/// <summary>
/// Summary DTO for personal schedule list items.
/// </summary>
public sealed record PersonalScheduleSummaryDto(
    long PersonalScheduleId,
    long EditionId,
    string? EditionName,
    string? FestivalName,
    string? Name,
    bool IsDefault,
    int EntryCount);

/// <summary>
/// Request DTO for creating a personal schedule.
/// </summary>
public sealed record CreatePersonalScheduleRequest(
    long EditionId,
    string? Name);

/// <summary>
/// Request DTO for updating a personal schedule.
/// </summary>
public sealed record UpdatePersonalScheduleRequest(
    string? Name,
    bool? IsDefault);

/// <summary>
/// Response DTO for personal schedule entry.
/// </summary>
public sealed record PersonalScheduleEntryDto(
    long PersonalScheduleEntryId,
    long PersonalScheduleId,
    long EngagementId,
    string? ArtistName,
    string? StageName,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? Notes,
    bool NotificationsEnabled,
    DateTime CreatedAtUtc)
{
    public static PersonalScheduleEntryDto FromEntity(
        PersonalScheduleEntry entry,
        string? artistName = null,
        string? stageName = null,
        DateTime? startTimeUtc = null,
        DateTime? endTimeUtc = null) =>
        new(
            entry.PersonalScheduleEntryId,
            entry.PersonalScheduleId,
            entry.EngagementId,
            artistName,
            stageName,
            startTimeUtc ?? DateTime.MinValue,
            endTimeUtc ?? DateTime.MinValue,
            entry.Notes,
            entry.NotificationsEnabled,
            entry.CreatedAtUtc);
}

/// <summary>
/// Request DTO for adding an entry to a personal schedule.
/// </summary>
public sealed record AddScheduleEntryRequest(
    long EngagementId,
    string? Notes,
    bool NotificationsEnabled = true);

/// <summary>
/// Request DTO for updating a schedule entry.
/// </summary>
public sealed record UpdateScheduleEntryRequest(
    string? Notes,
    bool? NotificationsEnabled);

/// <summary>
/// Detailed personal schedule with all entries.
/// </summary>
public sealed record PersonalScheduleDetailDto(
    long PersonalScheduleId,
    long UserId,
    long EditionId,
    string? EditionName,
    string? FestivalName,
    string? Name,
    bool IsDefault,
    DateTime? LastSyncedAtUtc,
    IReadOnlyList<PersonalScheduleEntryDto> Entries);
