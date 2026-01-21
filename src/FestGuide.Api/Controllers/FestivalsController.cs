using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Public endpoints for festival discovery (attendee-facing).
/// </summary>
[ApiController]
[Route("api/v1/festivals")]
[Produces("application/json")]
public class FestivalsController : ControllerBase
{
    private readonly IFestivalService _festivalService;
    private readonly IEditionService _editionService;
    private readonly IScheduleService _scheduleService;
    private readonly ILogger<FestivalsController> _logger;

    public FestivalsController(
        IFestivalService festivalService,
        IEditionService editionService,
        IScheduleService scheduleService,
        ILogger<FestivalsController> logger)
    {
        _festivalService = festivalService;
        _editionService = editionService;
        _scheduleService = scheduleService;
        _logger = logger;
    }

    /// <summary>
    /// Search for festivals by name.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<FestivalSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Ok(ApiResponse<IReadOnlyList<FestivalSummaryDto>>.Success(Array.Empty<FestivalSummaryDto>()));
        }

        var festivals = await _festivalService.SearchAsync(q, null, limit, ct);
        return Ok(ApiResponse<IReadOnlyList<FestivalSummaryDto>>.Success(festivals));
    }

    /// <summary>
    /// Gets a festival by ID (public view).
    /// </summary>
    [HttpGet("{festivalId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<FestivalDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFestival(Guid festivalId, CancellationToken ct)
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
    /// Gets all published editions for a festival.
    /// </summary>
    [HttpGet("{festivalId:guid}/editions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EditionSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEditions(Guid festivalId, CancellationToken ct)
    {
        var editions = await _editionService.GetPublishedByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<EditionSummaryDto>>.Success(editions));
    }

    /// <summary>
    /// Gets an edition by ID (public view).
    /// </summary>
    [HttpGet("editions/{editionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EditionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEdition(Guid editionId, CancellationToken ct)
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
    /// Gets the published schedule for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:guid}/schedule")]
    [ProducesResponseType(typeof(ApiResponse<ScheduleDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSchedule(Guid editionId, CancellationToken ct)
    {
        try
        {
            var schedule = await _scheduleService.GetScheduleDetailAsync(editionId, ct);
            return Ok(ApiResponse<ScheduleDetailDto>.Success(schedule));
        }
        catch (EditionNotFoundException)
        {
            return NotFound(CreateError("EDITION_NOT_FOUND", "Edition not found."));
        }
    }

    /// <summary>
    /// Gets venues for an edition.
    /// </summary>
    [HttpGet("editions/{editionId:guid}/venues")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<VenueSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVenues(Guid editionId, [FromServices] IVenueService venueService, CancellationToken ct)
    {
        var venues = await venueService.GetByEditionAsync(editionId, ct);
        return Ok(ApiResponse<IReadOnlyList<VenueSummaryDto>>.Success(venues));
    }

    /// <summary>
    /// Gets artists for a festival.
    /// </summary>
    [HttpGet("{festivalId:guid}/artists")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ArtistSummaryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArtists(Guid festivalId, [FromServices] IArtistService artistService, CancellationToken ct)
    {
        var artists = await artistService.GetByFestivalAsync(festivalId, ct);
        return Ok(ApiResponse<IReadOnlyList<ArtistSummaryDto>>.Success(artists));
    }

    /// <summary>
    /// Gets a specific artist.
    /// </summary>
    [HttpGet("artists/{artistId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ArtistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetArtist(Guid artistId, [FromServices] IArtistService artistService, CancellationToken ct)
    {
        try
        {
            var artist = await artistService.GetByIdAsync(artistId, ct);
            return Ok(ApiResponse<ArtistDto>.Success(artist));
        }
        catch (ArtistNotFoundException)
        {
            return NotFound(CreateError("ARTIST_NOT_FOUND", "Artist not found."));
        }
    }

    private static ApiErrorResponse CreateError(string code, string message) =>
        new(new ApiError(code, message), new ApiMetadata(DateTime.UtcNow));
}
