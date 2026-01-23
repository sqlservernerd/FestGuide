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
    [HttpGet("festivals/{festivalId:long}/venues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues(long festivalId, CancellationToken ct)
    {
        var venues = await _venueService.GetByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<VenueSummaryDto>>.Success(venues));
    }

    /// <summary>
    /// Gets all venues for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:long}/venues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenuesByEdition(long editionId, CancellationToken ct)
    {
        var venues = await _venueService.GetByEditionAsync(editionId, ct);
        return Ok(ApiResponse<IReadOnlyList<VenueSummaryDto>>.Success(venues));
    }

    /// <summary>
    /// Gets a venue by ID.
    /// </summary>
    [HttpGet("venues/{venueId:long}")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVenue(long venueId, CancellationToken ct)
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
    [HttpPost("festivals/{festivalId:long}/venues")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateVenue(long festivalId, [FromBody] CreateVenueRequest request, CancellationToken ct)
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
    [HttpPut("venues/{venueId:long}")]
    [ProducesResponseType(typeof(ApiResponse<VenueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVenue(long venueId, [FromBody] UpdateVenueRequest request, CancellationToken ct)
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
    [HttpDelete("venues/{venueId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVenue(long venueId, CancellationToken ct)
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
    [HttpPost("editions/{editionId:long}/venues/{venueId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddVenueToEdition(long editionId, long venueId, CancellationToken ct)
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
    [HttpDelete("editions/{editionId:long}/venues/{venueId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveVenueFromEdition(long editionId, long venueId, CancellationToken ct)
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
    [HttpGet("venues/{venueId:long}/stages")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<StageSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStages(long venueId, CancellationToken ct)
    {
        var stages = await _venueService.GetStagesByVenueAsync(venueId, ct);
        return Ok(ApiResponse<IReadOnlyList<StageSummaryDto>>.Success(stages));
    }

    /// <summary>
    /// Gets a stage by ID.
    /// </summary>
    [HttpGet("stages/{stageId:long}")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStage(long stageId, CancellationToken ct)
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
    [HttpPost("venues/{venueId:long}/stages")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStage(long venueId, [FromBody] CreateStageRequest request, CancellationToken ct)
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
    [HttpPut("stages/{stageId:long}")]
    [ProducesResponseType(typeof(ApiResponse<StageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStage(long stageId, [FromBody] UpdateStageRequest request, CancellationToken ct)
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
    [HttpDelete("stages/{stageId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStage(long stageId, CancellationToken ct)
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
