using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FestGuide.Api.Hubs;

/// <summary>
/// SignalR hub for real-time schedule updates.
/// </summary>
[Authorize]
public class ScheduleHub : Hub
{
    private readonly ILogger<ScheduleHub> _logger;

    public ScheduleHub(ILogger<ScheduleHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Joins a group for an edition to receive schedule updates.
    /// </summary>
    public async Task JoinEdition(Guid editionId)
    {
        var groupName = GetEditionGroupName(editionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("Connection {ConnectionId} joined edition group {EditionId}",
            Context.ConnectionId, editionId);
    }

    /// <summary>
    /// Leaves a group for an edition.
    /// </summary>
    public async Task LeaveEdition(Guid editionId)
    {
        var groupName = GetEditionGroupName(editionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("Connection {ConnectionId} left edition group {EditionId}",
            Context.ConnectionId, editionId);
    }

    /// <summary>
    /// Joins a group for a specific personal schedule.
    /// </summary>
    public async Task JoinPersonalSchedule(Guid scheduleId)
    {
        var groupName = GetPersonalScheduleGroupName(scheduleId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation("Connection {ConnectionId} joined personal schedule group {ScheduleId}",
            Context.ConnectionId, scheduleId);
    }

    /// <summary>
    /// Leaves a personal schedule group.
    /// </summary>
    public async Task LeavePersonalSchedule(Guid scheduleId)
    {
        var groupName = GetPersonalScheduleGroupName(scheduleId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public static string GetEditionGroupName(Guid editionId) => $"edition-{editionId}";
    public static string GetPersonalScheduleGroupName(Guid scheduleId) => $"personal-schedule-{scheduleId}";
}
