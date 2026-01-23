using System.Security.Claims;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Endpoints for analytics and dashboard metrics.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Produces("application/json")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(
        IAnalyticsService analyticsService,
        ILogger<AnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _logger = logger;
    }

    /// <summary>
    /// Tracks an analytics event (can be called by attendees).
    /// </summary>
    [HttpPost("track")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TrackEvent([FromBody] TrackEventRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        await _analyticsService.TrackEventAsync(userId, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Gets dashboard summary for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/dashboard")]
    [ProducesResponseType(typeof(ApiResponse<EditionDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEditionDashboard(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var dashboard = await _analyticsService.GetEditionDashboardAsync(editionId, userId.Value, ct);
            return Ok(ApiResponse<EditionDashboardDto>.Success(dashboard));
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets festival-wide summary.
    /// </summary>
    [HttpGet("festivals/{festivalId:long}/summary")]
    [ProducesResponseType(typeof(ApiResponse<FestivalAnalyticsSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFestivalSummary(long festivalId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var summary = await _analyticsService.GetFestivalSummaryAsync(festivalId, userId.Value, ct);
            return Ok(ApiResponse<FestivalAnalyticsSummaryDto>.Success(summary));
        }
        catch (FestivalNotFoundException)
        {
            return NotFound(CreateError("FESTIVAL_NOT_FOUND", "Festival not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets top artists for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/artists")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArtistAnalyticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopArtists(long editionId, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var artists = await _analyticsService.GetTopArtistsAsync(editionId, userId.Value, limit, ct);
            return Ok(ApiResponse<IReadOnlyList<ArtistAnalyticsDto>>.Success(artists));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets top engagements for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/engagements")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EngagementAnalyticsDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTopEngagements(long editionId, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var engagements = await _analyticsService.GetTopEngagementsAsync(editionId, userId.Value, limit, ct);
            return Ok(ApiResponse<IReadOnlyList<EngagementAnalyticsDto>>.Success(engagements));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets event timeline for charts.
    /// </summary>
    [HttpGet("editions/{editionId:long}/timeline")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimelineDataPointDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventTimeline(
        long editionId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        [FromQuery] string? eventType = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var request = new TimelineRequest(fromUtc, toUtc, eventType);
            var timeline = await _analyticsService.GetEventTimelineAsync(editionId, userId.Value, request, ct);
            return Ok(ApiResponse<IReadOnlyList<TimelineDataPointDto>>.Success(timeline));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets daily active users for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/daily-active-users")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DailyActiveUsersDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDailyActiveUsers(
        long editionId,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var dau = await _analyticsService.GetDailyActiveUsersAsync(editionId, userId.Value, fromUtc, toUtc, ct);
            return Ok(ApiResponse<IReadOnlyList<DailyActiveUsersDto>>.Success(dau));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets platform distribution for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/platforms")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PlatformDistributionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlatformDistribution(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var platforms = await _analyticsService.GetPlatformDistributionAsync(editionId, userId.Value, ct);
            return Ok(ApiResponse<IReadOnlyList<PlatformDistributionDto>>.Success(platforms));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets event type distribution for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/event-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EventTypeDistributionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEventTypeDistribution(
        long editionId,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var distribution = await _analyticsService.GetEventTypeDistributionAsync(editionId, userId.Value, fromUtc, toUtc, ct);
            return Ok(ApiResponse<IReadOnlyList<EventTypeDistributionDto>>.Success(distribution));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ApiErrorResponse CreateError(string code, string message) =>
        new(new ApiError(code, message), new ApiMetadata(DateTime.UtcNow));
}
