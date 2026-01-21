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
/// Organizer endpoints for venue and stage management.
/// </summary>
[ApiController]
[Route("api/v1/organizer")]
[Produces("application/json")]
[Authorize]
public class OrganizerVenuesController : ControllerBase
{
    private readonly IVenueService _venueService;
    private readonly IValidator<CreateVenueRequest> _createVenueValidator;
    private readonly IValidator<UpdateVenueRequest> _updateVenueValidator;
    private readonly IValidator<CreateStageRequest> _createStageValidator;
    private readonly IValidator<UpdateStageRequest> _updateStageValidator;
    private readonly ILogger<OrganizerVenuesController> _logger;

    public OrganizerVenuesController(
        IVenueService venueService,
        IValidator<CreateVenueRequest> createVenueValidator,
        IValidator<UpdateVenueRequest> updateVenueValidator,
        IValidator<CreateStageRequest> createStageValidator,
        IValidator<UpdateStageRequest> updateStageValidator,
        ILogger<OrganizerVenuesController> logger)
    {
        _venueService = venueService;
        _createVenueValidator = createVenueValidator;
        _updateVenueValidator = updateVenueValidator;
        _createStageValidator = createStageValidator;
        _updateStageValidator = updateStageValidator;
        _logger = logger;
    }

    #region Venue Endpoints

    /// <summary>
    /// Gets all venues for a festival.
    /// </summary>
    [HttpGet("festivals/{festivalId:guid}/venues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues(Guid festivalId, CancellationToken ct)
    {
        var venues = await _venueService.GetByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<VenueSummaryDto>>.Success(venues));
    }

    /// <summary>
    /// Gets all venues for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:guid}/venues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenuesByEdition(Guid editionId, CancellationToken ct)
    {
        var venues = await _venueService.GetByEditionAsync(editionId, ct);
        return Ok(ApiResponse<IReadOnlyList<VenueSummaryDto>>.Success(venues));
    }

    /// <summary>
    /// Gets a venue by ID.
    /// </summary>
    [HttpGet("venues/{venueId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVenue(Guid venueId, CancellationToken ct)
    {
        try
        {
            var venue = await _venueService.GetByIdAsync(venueId, ct);
            return Ok(ApiResponse<VenueDto>.Success(venue));
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
    }

    /// <summary>
    /// Creates a new venue for a festival.
    /// </summary>
    [HttpPost("festivals/{festivalId:guid}/venues")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVenue(Guid festivalId, [FromBody] CreateVenueRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createVenueValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var venue = await _venueService.CreateAsync(festivalId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetVenue), new { venueId = venue.VenueId }, ApiResponse<VenueDto>.Success(venue));
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
    /// Updates a venue.
    /// </summary>
    [HttpPut("venues/{venueId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVenue(Guid venueId, [FromBody] UpdateVenueRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateVenueValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var venue = await _venueService.UpdateAsync(venueId, userId.Value, request, ct);
            return Ok(ApiResponse<VenueDto>.Success(venue));
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Deletes a venue.
    /// </summary>
    [HttpDelete("venues/{venueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVenue(Guid venueId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _venueService.DeleteAsync(venueId, userId.Value, ct);
            return NoContent();
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Associates a venue with an edition.
    /// </summary>
    [HttpPost("editions/{editionId:guid}/venues/{venueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddVenueToEdition(Guid editionId, Guid venueId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _venueService.AddVenueToEditionAsync(editionId, venueId, userId.Value, ct);
            return NoContent();
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Removes a venue association from an edition.
    /// </summary>
    [HttpDelete("editions/{editionId:guid}/venues/{venueId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVenueFromEdition(Guid editionId, Guid venueId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _venueService.RemoveVenueFromEditionAsync(editionId, venueId, userId.Value, ct);
            return NoContent();
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    #endregion

    #region Stage Endpoints

    /// <summary>
    /// Gets all stages for a venue.
    /// </summary>
    [HttpGet("venues/{venueId:guid}/stages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StageSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStages(Guid venueId, CancellationToken ct)
    {
        var stages = await _venueService.GetStagesByVenueAsync(venueId, ct);
        return Ok(ApiResponse<IReadOnlyList<StageSummaryDto>>.Success(stages));
    }

    /// <summary>
    /// Gets a stage by ID.
    /// </summary>
    [HttpGet("stages/{stageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStage(Guid stageId, CancellationToken ct)
    {
        try
        {
            var stage = await _venueService.GetStageByIdAsync(stageId, ct);
            return Ok(ApiResponse<StageDto>.Success(stage));
        }
        catch (StageNotFoundException)
        {
            return NotFound(CreateError("STAGE_NOT_FOUND", "Stage not found."));
        }
    }

    /// <summary>
    /// Creates a new stage for a venue.
    /// </summary>
    [HttpPost("venues/{venueId:guid}/stages")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStage(Guid venueId, [FromBody] CreateStageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _createStageValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var stage = await _venueService.CreateStageAsync(venueId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetStage), new { stageId = stage.StageId }, ApiResponse<StageDto>.Success(stage));
        }
        catch (VenueNotFoundException)
        {
            return NotFound(CreateError("VENUE_NOT_FOUND", "Venue not found."));
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Updates a stage.
    /// </summary>
    [HttpPut("stages/{stageId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStage(Guid stageId, [FromBody] UpdateStageRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _updateStageValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var stage = await _venueService.UpdateStageAsync(stageId, userId.Value, request, ct);
            return Ok(ApiResponse<StageDto>.Success(stage));
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
    /// Deletes a stage.
    /// </summary>
    [HttpDelete("stages/{stageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStage(Guid stageId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _venueService.DeleteStageAsync(stageId, userId.Value, ct);
            return NoContent();
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

    #endregion

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
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
