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
/// Organizer endpoints for edition management.
/// </summary>
[ApiController]
[Route("api/v1/organizer")]
[Produces("application/json")]
[Authorize]
public class OrganizerEditionsController : ControllerBase
{
    private readonly IEditionService _editionService;
    private readonly IValidator<CreateEditionRequest> _createValidator;
    private readonly IValidator<UpdateEditionRequest> _updateValidator;
    private readonly ILogger<OrganizerEditionsController> _logger;

    public OrganizerEditionsController(
        IEditionService editionService,
        IValidator<CreateEditionRequest> createValidator,
        IValidator<UpdateEditionRequest> updateValidator,
        ILogger<OrganizerEditionsController> logger)
    {
        _editionService = editionService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all editions for a festival.
    /// </summary>
    [HttpGet("festivals/{festivalId:long}/editions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EditionSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEditions(long festivalId, CancellationToken ct)
    {
        var editions = await _editionService.GetByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<EditionSummaryDto>>.Success(editions));
    }

    /// <summary>
    /// Gets an edition by ID.
    /// </summary>
    [HttpGet("editions/{editionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<EditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEdition(long editionId, CancellationToken ct)
    {
        try
        {
            var edition = await _editionService.GetByIdAsync(editionId, ct);
            return Ok(ApiResponse<EditionDto>.Success(edition));
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
    }

    /// <summary>
    /// Creates a new edition for a festival.
    /// </summary>
    [HttpPost("festivals/{festivalId:long}/editions")]
    [ProducesResponseType(typeof(ApiResponse<EditionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEdition(long festivalId, [FromBody] CreateEditionRequest request, CancellationToken ct)
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
            var edition = await _editionService.CreateAsync(festivalId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetEdition), new { editionId = edition.EditionId }, ApiResponse<EditionDto>.Success(edition));
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
    /// Updates an edition.
    /// </summary>
    [HttpPut("editions/{editionId:long}")]
    [ProducesResponseType(typeof(ApiResponse<EditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEdition(long editionId, [FromBody] UpdateEditionRequest request, CancellationToken ct)
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
            var edition = await _editionService.UpdateAsync(editionId, userId.Value, request, ct);
            return Ok(ApiResponse<EditionDto>.Success(edition));
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
    /// Deletes an edition.
    /// </summary>
    [HttpDelete("editions/{editionId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEdition(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _editionService.DeleteAsync(editionId, userId.Value, ct);
            return NoContent();
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
