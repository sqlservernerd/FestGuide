using System.Security.Claims;
using FestGuide.Api.Models;
using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using FestGuide.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestGuide.Api.Controllers;

/// <summary>
/// Endpoints for report generation and data export.
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IExportService _exportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        IExportService exportService,
        ILogger<ReportsController> logger)
    {
        _exportService = exportService;
        _logger = logger;
    }

    /// <summary>
    /// Exports edition data in CSV format.
    /// </summary>
    [HttpPost("editions/{editionId:long}/export")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportEditionData(
        long editionId,
        [FromBody] ExportRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _exportService.ExportEditionDataAsync(editionId, userId.Value, request, ct);
            return File(result.Data, result.ContentType, result.FileName);
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
    /// Exports schedule as CSV.
    /// </summary>
    [HttpGet("editions/{editionId:long}/schedule.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportScheduleCsv(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _exportService.ExportScheduleCsvAsync(editionId, userId.Value, ct);
            return File(result.Data, result.ContentType, result.FileName);
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
    /// Exports artist list as CSV.
    /// </summary>
    [HttpGet("editions/{editionId:long}/artists.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportArtistsCsv(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _exportService.ExportArtistsCsvAsync(editionId, userId.Value, ct);
            return File(result.Data, result.ContentType, result.FileName);
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
    /// Exports analytics summary as CSV.
    /// </summary>
    [HttpGet("editions/{editionId:long}/analytics.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAnalyticsCsv(
        long editionId,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _exportService.ExportAnalyticsCsvAsync(editionId, userId.Value, fromUtc, toUtc, ct);
            return File(result.Data, result.ContentType, result.FileName);
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
    /// Exports attendee saves (which engagements users have saved) as CSV.
    /// </summary>
    [HttpGet("editions/{editionId:long}/attendee-saves.csv")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportAttendeeSavesCsv(long editionId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            var result = await _exportService.ExportAttendeeSavesCsvAsync(editionId, userId.Value, ct);
            return File(result.Data, result.ContentType, result.FileName);
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
}
