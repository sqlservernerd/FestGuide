using System.Security.Claims;
using FestGuide.DataAccess.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FestGuide.Api.Hubs;

/// <summary>
/// SignalR hub for real-time schedule updates.
/// </summary>
[Authorize]
public class ScheduleHub : Hub
{
    private readonly IPersonalScheduleRepository _personalScheduleRepository;
    private readonly IEditionRepository _editionRepository;
    private readonly ILogger<ScheduleHub> _logger;

    public ScheduleHub(
        IPersonalScheduleRepository personalScheduleRepository,
        IEditionRepository editionRepository,
        ILogger<ScheduleHub> logger)
    {
        _personalScheduleRepository = personalScheduleRepository;
        _editionRepository = editionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Joins a group for an edition to receive schedule updates.
    /// </summary>
    public async Task JoinEdition(Guid editionId)
    {
        // Verify that the edition exists before allowing access
        var edition = await _editionRepository.GetByIdAsync(editionId, Context.ConnectionAborted).ConfigureAwait(false);
        if (edition == null)
        {
            _logger.LogWarning("Connection {ConnectionId} attempted to join non-existent edition {EditionId}",
                Context.ConnectionId, editionId);
            throw new HubException("Edition not found.");
        }

        var groupName = GetEditionGroupName(editionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted).ConfigureAwait(false);

        _logger.LogInformation("Connection {ConnectionId} joined edition group {EditionId}",
            Context.ConnectionId, editionId);
    }

    /// <summary>
    /// Leaves a group for an edition.
    /// </summary>
    public async Task LeaveEdition(Guid editionId)
    {
        var groupName = GetEditionGroupName(editionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted).ConfigureAwait(false);

        _logger.LogInformation("Connection {ConnectionId} left edition group {EditionId}",
            Context.ConnectionId, editionId);
    }

    /// <summary>
    /// Joins a group for a specific personal schedule.
    /// </summary>
    public async Task JoinPersonalSchedule(Guid scheduleId)
    {
        var userId = GetCurrentUserId();

        // Verify ownership
        var schedule = await _personalScheduleRepository.GetByIdAsync(scheduleId, Context.ConnectionAborted).ConfigureAwait(false);
        if (schedule == null || schedule.UserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to join personal schedule {ScheduleId} they don't own", userId, scheduleId);
            throw new HubException("Personal schedule not found or access denied.");
        }

        var groupName = GetPersonalScheduleGroupName(scheduleId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted).ConfigureAwait(false);

        _logger.LogInformation("Connection {ConnectionId} joined personal schedule group {ScheduleId}",
            Context.ConnectionId, scheduleId);
    }

    /// <summary>
    /// Leaves a personal schedule group.
    /// </summary>
    public async Task LeavePersonalSchedule(Guid scheduleId)
    {
        var groupName = GetPersonalScheduleGroupName(scheduleId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName, Context.ConnectionAborted).ConfigureAwait(false);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }

    private Guid GetCurrentUserId()
    {
        var user = Context.User;
        if (user == null)
        {
            _logger.LogError(
                "User context is null for authorized connection {ConnectionId}",
                Context.ConnectionId);
            throw new HubException("Authenticated user context is required.");
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogError(
                "Failed to parse user id claim for connection {ConnectionId}. Claim value: {ClaimValue}",
                Context.ConnectionId,
                userIdClaim);
            throw new HubException("Invalid user identifier.");
        }

        return userId;
    }

    public static string GetEditionGroupName(Guid editionId) => $"edition-{editionId}";
    public static string GetPersonalScheduleGroupName(Guid scheduleId) => $"personal-schedule-{scheduleId}";
}
