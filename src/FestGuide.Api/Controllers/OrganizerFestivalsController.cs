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
/// Organizer endpoints for festival management.
/// </summary>
[ApiController]
[Route("api/v1/organizer/festivals")]
[Produces("application/json")]
[Authorize]
public class OrganizerFestivalsController : ControllerBase
{
    private readonly IFestivalService _festivalService;
    private readonly IValidator<CreateFestivalRequest> _createValidator;
    private readonly IValidator<UpdateFestivalRequest> _updateValidator;
    private readonly IValidator<TransferOwnershipRequest> _transferValidator;
    private readonly ILogger<OrganizerFestivalsController> _logger;

    public OrganizerFestivalsController(
        IFestivalService festivalService,
        IValidator<CreateFestivalRequest> createValidator,
        IValidator<UpdateFestivalRequest> updateValidator,
        IValidator<TransferOwnershipRequest> transferValidator,
        ILogger<OrganizerFestivalsController> logger)
    {
        _festivalService = festivalService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _transferValidator = transferValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all festivals the current user has access to.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FestivalSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyFestivals(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var festivals = await _festivalService.GetMyFestivalsAsync(userId.Value, ct);
        return Ok(ApiResponse<IReadOnlyList<FestivalSummaryDto>>.Success(festivals));
    }

    /// <summary>
    /// Gets a festival by ID.
    /// </summary>
    [HttpGet("{festivalId:long}")]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFestival(long festivalId, CancellationToken ct)
    {
        try
        {
            var festival = await _festivalService.GetByIdAsync(festivalId, ct);
            return Ok(ApiResponse<FestivalDto>.Success(festival));
        }
        catch (FestivalNotFoundException)
        {
            return NotFound(CreateError("FESTIVAL_NOT_FOUND", "Festival not found."));
        }
    }

    /// <summary>
    /// Creates a new festival.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateFestival([FromBody] CreateFestivalRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        var festival = await _festivalService.CreateAsync(userId.Value, request, ct);
        return CreatedAtAction(nameof(GetFestival), new { festivalId = festival.FestivalId }, ApiResponse<FestivalDto>.Success(festival));
    }

    /// <summary>
    /// Updates a festival.
    /// </summary>
    [HttpPut("{festivalId:long}")]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateFestival(long festivalId, [FromBody] UpdateFestivalRequest request, CancellationToken ct)
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
            var festival = await _festivalService.UpdateAsync(festivalId, userId.Value, request, ct);
            return Ok(ApiResponse<FestivalDto>.Success(festival));
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
    /// Deletes a festival.
    /// </summary>
    [HttpDelete("{festivalId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFestival(long festivalId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _festivalService.DeleteAsync(festivalId, userId.Value, ct);
            return NoContent();
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
    /// Transfers festival ownership to another user.
    /// </summary>
    [HttpPost("{festivalId:long}/transfer-ownership")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferOwnership(long festivalId, [FromBody] TransferOwnershipRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _transferValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            await _festivalService.TransferOwnershipAsync(festivalId, userId.Value, request, ct);
            return NoContent();
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
