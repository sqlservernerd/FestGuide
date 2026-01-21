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
/// Endpoints for managing device registrations for push notifications.
/// </summary>
[ApiController]
[Route("api/v1/devices")]
[Produces("application/json")]
[Authorize]
public class DevicesController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<RegisterDeviceRequest> _registerValidator;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(
        INotificationService notificationService,
        IValidator<RegisterDeviceRequest> registerValidator,
        ILogger<DevicesController> logger)
    {
        _notificationService = notificationService;
        _registerValidator = registerValidator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a device for push notifications.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DeviceTokenDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        var device = await _notificationService.RegisterDeviceAsync(userId.Value, request, ct);

        return CreatedAtAction(
            nameof(GetDevices),
            ApiResponse<DeviceTokenDto>.Success(device));
    }

    /// <summary>
    /// Gets all registered devices for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DeviceTokenDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDevices(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var devices = await _notificationService.GetDevicesAsync(userId.Value, ct);
        return Ok(ApiResponse<IReadOnlyList<DeviceTokenDto>>.Success(devices));
    }

    /// <summary>
    /// Unregisters a device.
    /// </summary>
    [HttpDelete("{deviceId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterDevice(Guid deviceId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        try
        {
            await _notificationService.UnregisterDeviceAsync(userId.Value, deviceId, ct);
            return NoContent();
        }
        catch (ForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Unregisters a device by its token (for logout scenarios).
    /// </summary>
    [HttpDelete("by-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnregisterDeviceByToken([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
        {
            return BadRequest(CreateError(
                "VALIDATION_ERROR",
                "The 'token' query parameter is required and must be between 1 and 256 characters long."));
        }

        await _notificationService.UnregisterDeviceByTokenAsync(token, ct);
        return NoContent();
    }

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
