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
/// Organizer endpoints for artist management.
/// </summary>
[ApiController]
[Route("api/v1/organizer")]
[Produces("application/json")]
[Authorize]
public class OrganizerArtistsController : ControllerBase
{
    private readonly IArtistService _artistService;
    private readonly IValidator<CreateArtistRequest> _createValidator;
    private readonly IValidator<UpdateArtistRequest> _updateValidator;
    private readonly ILogger<OrganizerArtistsController> _logger;

    public OrganizerArtistsController(
        IArtistService artistService,
        IValidator<CreateArtistRequest> createValidator,
        IValidator<UpdateArtistRequest> updateValidator,
        ILogger<OrganizerArtistsController> logger)
    {
        _artistService = artistService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all artists for a festival.
    /// </summary>
    [HttpGet("festivals/{festivalId:long}/artists")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArtistSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArtists(long festivalId, CancellationToken ct)
    {
        var artists = await _artistService.GetByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<ArtistSummaryDto>>.Success(artists));
    }

    /// <summary>
    /// Searches artists by name within a festival.
    /// </summary>
    [HttpGet("festivals/{festivalId:long}/artists/search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArtistSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchArtists(long festivalId, [FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var artists = await _artistService.SearchAsync(festivalId, q ?? string.Empty, limit, ct);
        return Ok(ApiResponse<IReadOnlyList<ArtistSummaryDto>>.Success(artists));
    }

    /// <summary>
    /// Gets an artist by ID.
    /// </summary>
    [HttpGet("artists/{artistId:long}")]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtist(long artistId, CancellationToken ct)
    {
        try
        {
            var artist = await _artistService.GetByIdAsync(artistId, ct);
            return Ok(ApiResponse<ArtistDto>.Success(artist));
        }
        catch (ArtistNotFoundException)
        {
            return NotFound(CreateError("ARTIST_NOT_FOUND", "Artist not found."));
        }
    }

    /// <summary>
    /// Creates a new artist for a festival.
    /// </summary>
    [HttpPost("festivals/{festivalId:long}/artists")]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateArtist(long festivalId, [FromBody] CreateArtistRequest request, CancellationToken ct)
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
            var artist = await _artistService.CreateAsync(festivalId, userId.Value, request, ct);
            return CreatedAtAction(nameof(GetArtist), new { artistId = artist.ArtistId }, ApiResponse<ArtistDto>.Success(artist));
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
    /// Updates an artist.
    /// </summary>
    [HttpPut("artists/{artistId:long}")]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateArtist(long artistId, [FromBody] UpdateArtistRequest request, CancellationToken ct)
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
            var artist = await _artistService.UpdateAsync(artistId, userId.Value, request, ct);
            return Ok(ApiResponse<ArtistDto>.Success(artist));
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
    /// Deletes an artist.
    /// </summary>
    [HttpDelete("artists/{artistId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteArtist(long artistId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _artistService.DeleteAsync(artistId, userId.Value, ct);
            return NoContent();
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
