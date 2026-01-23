using System.Security.Claims;
using FluentValidation;
using FestConnect.Api.Models;
using FestConnect.Application.Dtos;
using FestConnect.Application.Services;
using FestConnect.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestConnect.Api.Controllers;

/// <summary>
/// Organizer endpoints for schedule, time slot, and engagement management.
/// </summary>
[ApiController]
[Route("api/v1/organizer")]
[Produces("application/json")]
[Authorize]
public class OrganizerScheduleController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IValidator<CreateTimeSlotRequest> _createTimeSlotValidator;
    private readonly IValidator<UpdateTimeSlotRequest> _updateTimeSlotValidator;
    private readonly IValidator<CreateEngagementRequest> _createEngagementValidator;
    private readonly IValidator<UpdateEngagementRequest> _updateEngagementValidator;
    private readonly ILogger<OrganizerScheduleController> _logger;

    public OrganizerScheduleController(
        IScheduleService scheduleService,
        IValidator<CreateTimeSlotRequest> createTimeSlotValidator,
        IValidator<UpdateTimeSlotRequest> updateTimeSlotValidator,
        IValidator<CreateEngagementRequest> createEngagementValidator,
        IValidator<UpdateEngagementRequest> updateEngagementValidator,
        ILogger<OrganizerScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _createTimeSlotValidator = createTimeSlotValidator;
        _updateTimeSlotValidator = updateTimeSlotValidator;
        _createEngagementValidator = createEngagementValidator;
        _updateEngagementValidator = updateEngagementValidator;
        _logger = logger;
    }

    #region Schedule Endpoints

    /// <summary>
    /// Gets the schedule for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/schedule")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(long editionId, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleAsync(editionId, ct);
            return Ok(ApiResponse<ScheduleDto>.Success(schedule));
        }
        catch (ScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Schedule not found."));
        }
    }

    /// <summary>
    /// Gets the detailed schedule for an edition including all time slots and engagements.
    /// </summary>
    [HttpGet("editions/{editionId:long}/schedule/detail")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduleDetail(long editionId, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleDetailAsync(editionId, ct);
            return Ok(ApiResponse<ScheduleDetailDto>.Success(schedule));
        }
        catch (ScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Schedule not found."));
        }
    }

    /// <summary>
    /// Publishes a schedule, making it visible to attendees.
    /// </summary>
    [HttpPost("editions/{editionId:long}/schedule/publish")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishSchedule(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schedule = await _scheduleService.PublishScheduleAsync(editionId, userId.Value, ct);
            return Ok(ApiResponse<ScheduleDto>.Success(schedule));
        }
        catch (ScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Schedule not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    #endregion

    #region Time Slot Endpoints

    /// <summary>
    /// Gets all time slots for a stage within an edition.
    /// </summary>
    [HttpGet("stages/{stageId:long}/editions/{editionId:long}/timeslots")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TimeSlotDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTimeSlots(long stageId, long editionId, CancellationToken ct)
    {
        var timeSlots = await _scheduleService.GetTimeSlotsByStageAsync(stageId, editionId, ct);
        return Ok(ApiResponse<IReadOnlyList<TimeSlotDto>>.Success(timeSlots));
    }

    /// <summary>
    /// Gets a time slot by ID.
    /// </summary>
    [HttpGet("timeslots/{timeSlotId:long}")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeSlot(long timeSlotId, CancellationToken ct)
    {
        try
        {
            var timeSlot = await _scheduleService.GetTimeSlotByIdAsync(timeSlotId, ct);
            return Ok(ApiResponse<TimeSlotDto>.Success(timeSlot));
        }
        catch (TimeSlotNotFoundException)
        {
            return NotFound(CreateError("TIMESLOT_NOT_FOUND", "Time slot not found."));
        }
    }

    /// <summary>
    /// Creates a new time slot for a stage.
    /// </summary>
    [HttpPost("stages/{stageId:long}/timeslots")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTimeSlot(long stageId, [FromBody] CreateTimeSlotRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createTimeSlotValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var timeSlot = await _scheduleService.CreateTimeSlotAsync(stageId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetTimeSlot), new { timeSlotId = timeSlot.TimeSlotId }, ApiResponse<TimeSlotDto>.Success(timeSlot));
        }
        catch (StageNotFoundException)
        {
            return NotFound(CreateError("STAGE_NOT_FOUND", "Stage not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Updates a time slot.
    /// </summary>
    [HttpPut("timeslots/{timeSlotId:long}")]
    [ProducesResponseType(typeof(ApiResponse<TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTimeSlot(long timeSlotId, [FromBody] UpdateTimeSlotRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateTimeSlotValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var timeSlot = await _scheduleService.UpdateTimeSlotAsync(timeSlotId, userId.Value, request, ct);
            return Ok(ApiResponse<TimeSlotDto>.Success(timeSlot));
        }
        catch (TimeSlotNotFoundException)
        {
            return NotFound(CreateError("TIMESLOT_NOT_FOUND", "Time slot not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Deletes a time slot.
    /// </summary>
    [HttpDelete("timeslots/{timeSlotId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTimeSlot(long timeSlotId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _scheduleService.DeleteTimeSlotAsync(timeSlotId, userId.Value, ct);
            return NoContent();
        }
        catch (TimeSlotNotFoundException)
        {
            return NotFound(CreateError("TIMESLOT_NOT_FOUND", "Time slot not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    #endregion

    #region Engagement Endpoints

    /// <summary>
    /// Gets an engagement by ID.
    /// </summary>
    [HttpGet("engagements/{engagementId:long}")]
    [ProducesResponseType(typeof(ApiResponse<EngagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEngagement(long engagementId, CancellationToken ct)
    {
        try
        {
            var engagement = await _scheduleService.GetEngagementByIdAsync(engagementId, ct);
            return Ok(ApiResponse<EngagementDto>.Success(engagement));
        }
        catch (EngagementNotFoundException)
        {
            return NotFound(CreateError("ENGAGEMENT_NOT_FOUND", "Engagement not found."));
        }
    }

    /// <summary>
    /// Creates an engagement (assigns artist to time slot).
    /// </summary>
    [HttpPost("timeslots/{timeSlotId:long}/engagement")]
    [ProducesResponseType(typeof(ApiResponse<EngagementDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEngagement(long timeSlotId, [FromBody] CreateEngagementRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createEngagementValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var engagement = await _scheduleService.CreateEngagementAsync(timeSlotId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetEngagement), new { engagementId = engagement.EngagementId }, ApiResponse<EngagementDto>.Success(engagement));
        }
        catch (TimeSlotNotFoundException)
        {
            return NotFound(CreateError("TIMESLOT_NOT_FOUND", "Time slot not found."));
        }
        catch (ArtistNotFoundException)
        {
            return NotFound(CreateError("ARTIST_NOT_FOUND", "Artist not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Updates an engagement.
    /// </summary>
    [HttpPut("engagements/{engagementId:long}")]
    [ProducesResponseType(typeof(ApiResponse<EngagementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEngagement(long engagementId, [FromBody] UpdateEngagementRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateEngagementValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var engagement = await _scheduleService.UpdateEngagementAsync(engagementId, userId.Value, request, ct);
            return Ok(ApiResponse<EngagementDto>.Success(engagement));
        }
        catch (EngagementNotFoundException)
        {
            return NotFound(CreateError("ENGAGEMENT_NOT_FOUND", "Engagement not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Deletes an engagement.
    /// </summary>
    [HttpDelete("engagements/{engagementId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEngagement(long engagementId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _scheduleService.DeleteEngagementAsync(engagementId, userId.Value, ct);
            return NoContent();
        }
        catch (EngagementNotFoundException)
        {
            return NotFound(CreateError("ENGAGEMENT_NOT_FOUND", "Engagement not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    #endregion

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ApiErrorResponse CreateError(string code, string message) =>
        new(new ApiError(code, message), new ApiMetadata(DateTime.UtcNow));

    private static ApiErrorResponse CreateValidationError(FluentValidation.Results.ValidationResult validation) =>
        new(
            new ApiError(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                validation.Errors.Select(e => new ApiErrorDetail(e.PropertyName, e.ErrorMessage))),
            new ApiMetadata(DateTime.UtcNow));
}
