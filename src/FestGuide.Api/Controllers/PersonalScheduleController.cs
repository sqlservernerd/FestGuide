using System.Security.Claims;
using FluentValidation;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Endpoints for managing personal schedules (authenticated attendee).
/// </summary>
[ApiController]
[Route("api/v1/my-schedule")]
[Produces("application/json")]
[Authorize]
public class PersonalScheduleController : ControllerBase
{
    private readonly IPersonalScheduleService _scheduleService;
    private readonly IValidator<CreatePersonalScheduleRequest> _createValidator;
    private readonly IValidator<UpdatePersonalScheduleRequest> _updateValidator;
    private readonly IValidator<AddScheduleEntryRequest> _addEntryValidator;
    private readonly IValidator<UpdateScheduleEntryRequest> _updateEntryValidator;
    private readonly ILogger<PersonalScheduleController> _logger;

    public PersonalScheduleController(
        IPersonalScheduleService scheduleService,
        IValidator<CreatePersonalScheduleRequest> createValidator,
        IValidator<UpdatePersonalScheduleRequest> updateValidator,
        IValidator<AddScheduleEntryRequest> addEntryValidator,
        IValidator<UpdateScheduleEntryRequest> updateEntryValidator,
        ILogger<PersonalScheduleController> logger)
    {
        _scheduleService = scheduleService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _addEntryValidator = addEntryValidator;
        _updateEntryValidator = updateEntryValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all personal schedules for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalScheduleSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySchedules(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schedules = await _scheduleService.GetMySchedulesAsync(userId.Value, ct);
        return Ok(ApiResponse<IReadOnlyList<PersonalScheduleSummaryDto>>.Success(schedules));
    }

    /// <summary>
    /// Gets personal schedules for a specific edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PersonalScheduleDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEdition(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var schedules = await _scheduleService.GetByEditionAsync(userId.Value, editionId, ct);
        return Ok(ApiResponse<IReadOnlyList<PersonalScheduleDto>>.Success(schedules));
    }

    /// <summary>
    /// Gets a personal schedule by ID.
    /// </summary>
    [HttpGet("{scheduleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(long scheduleId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schedule = await _scheduleService.GetByIdAsync(scheduleId, userId.Value, ct);
            return Ok(ApiResponse<PersonalScheduleDto>.Success(schedule));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets detailed schedule with all entries.
    /// </summary>
    [HttpGet("{scheduleId:long}/detail")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScheduleDetail(long scheduleId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schedule = await _scheduleService.GetDetailAsync(scheduleId, userId.Value, ct);
            return Ok(ApiResponse<PersonalScheduleDetailDto>.Success(schedule));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Creates a new personal schedule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSchedule([FromBody] CreatePersonalScheduleRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var schedule = await _scheduleService.CreateAsync(userId.Value, request, ct);
            return CreatedAtAction(
                nameof(GetSchedule),
                new { scheduleId = schedule.PersonalScheduleId },
                ApiResponse<PersonalScheduleDto>.Success(schedule));
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
    }

    /// <summary>
    /// Updates a personal schedule.
    /// </summary>
    [HttpPut("{scheduleId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSchedule(long scheduleId, [FromBody] UpdatePersonalScheduleRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var schedule = await _scheduleService.UpdateAsync(scheduleId, userId.Value, request, ct);
            return Ok(ApiResponse<PersonalScheduleDto>.Success(schedule));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Deletes a personal schedule.
    /// </summary>
    [HttpDelete("{scheduleId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSchedule(long scheduleId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _scheduleService.DeleteAsync(scheduleId, userId.Value, ct);
            return NoContent();
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Gets or creates a default schedule for an edition.
    /// </summary>
    [HttpPost("editions/{editionId:long}/default")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrCreateDefault(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schedule = await _scheduleService.GetOrCreateDefaultAsync(userId.Value, editionId, ct);
            return Ok(ApiResponse<PersonalScheduleDto>.Success(schedule));
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
    }

    /// <summary>
    /// Adds an entry (performance) to a schedule.
    /// </summary>
    [HttpPost("{scheduleId:long}/entries")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleEntryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddEntry(long scheduleId, [FromBody] AddScheduleEntryRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _addEntryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var entry = await _scheduleService.AddEntryAsync(scheduleId, userId.Value, request, ct);
            return CreatedAtAction(
                nameof(GetScheduleDetail),
                new { scheduleId },
                ApiResponse<PersonalScheduleEntryDto>.Success(entry));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (EngagementNotFoundException)
        {
            return NotFound(CreateError("ENGAGEMENT_NOT_FOUND", "Performance not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
        catch (ConflictException ex)
        {
            return Conflict(CreateError("CONFLICT", ex.Message));
        }
    }

    /// <summary>
    /// Updates an entry in a schedule.
    /// </summary>
    [HttpPut("entries/{entryId:long}")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEntry(long entryId, [FromBody] UpdateScheduleEntryRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateEntryValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var entry = await _scheduleService.UpdateEntryAsync(entryId, userId.Value, request, ct);
            return Ok(ApiResponse<PersonalScheduleEntryDto>.Success(entry));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (PersonalScheduleEntryNotFoundException)
        {
            return NotFound(CreateError("ENTRY_NOT_FOUND", "Entry not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Removes an entry from a schedule.
    /// </summary>
    [HttpDelete("entries/{entryId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveEntry(long entryId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _scheduleService.RemoveEntryAsync(entryId, userId.Value, ct);
            return NoContent();
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
        }
        catch (PersonalScheduleEntryNotFoundException)
        {
            return NotFound(CreateError("ENTRY_NOT_FOUND", "Entry not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Syncs the schedule for offline support.
    /// </summary>
    [HttpPost("{scheduleId:long}/sync")]
    [ProducesResponseType(typeof(ApiResponse<PersonalScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncSchedule(long scheduleId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var schedule = await _scheduleService.SyncAsync(scheduleId, userId.Value, ct);
            return Ok(ApiResponse<PersonalScheduleDetailDto>.Success(schedule));
        }
        catch (PersonalScheduleNotFoundException)
        {
            return NotFound(CreateError("SCHEDULE_NOT_FOUND", "Personal schedule not found."));
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

    private static ApiErrorResponse CreateValidationError(FluentValidation.Results.ValidationResult validation) =>
        new(
            new ApiError(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                validation.Errors.Select(e => new ApiErrorDetail(e.PropertyName, e.ErrorMessage))),
            new ApiMetadata(DateTime.UtcNow));
}
