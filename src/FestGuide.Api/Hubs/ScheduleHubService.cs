using FestGuide.Application.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace FestGuide.Api.Hubs;

/// <summary>
/// Service for sending real-time notifications through SignalR.
/// </summary>
public interface IScheduleHubService
{
    /// <summary>
    /// Notifies all clients watching an edition about a schedule change.
    /// </summary>
    Task NotifyScheduleChangedAsync(Guid editionId, ScheduleChangeNotification change, CancellationToken ct = default);

    /// <summary>
    /// Notifies all clients watching an edition that the schedule was published.
    /// </summary>
    Task NotifySchedulePublishedAsync(Guid editionId, int version, CancellationToken ct = default);

    /// <summary>
    /// Notifies a specific user about an update to their personal schedule.
    /// </summary>
    Task NotifyPersonalScheduleUpdatedAsync(Guid scheduleId, string updateType, CancellationToken ct = default);
}

/// <summary>
/// SignalR-based implementation of real-time schedule notifications.
/// </summary>
public class ScheduleHubService : IScheduleHubService
{
    private readonly IHubContext<ScheduleHub> _hubContext;
    private readonly ILogger<ScheduleHubService> _logger;

    public ScheduleHubService(IHubContext<ScheduleHub> hubContext, ILogger<ScheduleHubService> logger)
    {
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task NotifyScheduleChangedAsync(Guid editionId, ScheduleChangeNotification change, CancellationToken ct = default)
    {
        var groupName = ScheduleHub.GetEditionGroupName(editionId);

        await _hubContext.Clients.Group(groupName).SendAsync(
            "ScheduleChanged",
            new
            {
                editionId,
                change.ChangeType,
                change.EngagementId,
                change.TimeSlotId,
                change.ArtistName,
                change.StageName,
                change.OldStartTime,
                change.NewStartTime,
                change.Message,
                timestamp = DateTime.UtcNow
            },
            ct).ConfigureAwait(false);

        _logger.LogInformation("Schedule change broadcast to edition {EditionId}: {ChangeType}",
            editionId, change.ChangeType);
    }

    /// <inheritdoc />
    public async Task NotifySchedulePublishedAsync(Guid editionId, int version, CancellationToken ct = default)
    {
        var groupName = ScheduleHub.GetEditionGroupName(editionId);

        await _hubContext.Clients.Group(groupName).SendAsync(
            "SchedulePublished",
            new
            {
                editionId,
                version,
                timestamp = DateTime.UtcNow
            },
            ct).ConfigureAwait(false);

        _logger.LogInformation("Schedule published broadcast to edition {EditionId}, version {Version}",
            editionId, version);
    }

    /// <inheritdoc />
    public async Task NotifyPersonalScheduleUpdatedAsync(Guid scheduleId, string updateType, CancellationToken ct = default)
    {
        var groupName = ScheduleHub.GetPersonalScheduleGroupName(scheduleId);

        await _hubContext.Clients.Group(groupName).SendAsync(
            "PersonalScheduleUpdated",
            new
            {
                scheduleId,
                updateType,
                timestamp = DateTime.UtcNow
            },
            ct).ConfigureAwait(false);

        _logger.LogInformation("Personal schedule update broadcast: {ScheduleId}, {UpdateType}",
            scheduleId, updateType);
    }
}
