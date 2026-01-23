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
/// Endpoints for managing device registrations for push notifications.
/// </summary>
[ApiController]
[Route("api/v1/devices")]
[Produces("application/json")]
[Authorize]
public class DevicesController : BaseApiController
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
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation failed for registering device for user {UserId}", userId);
            return BadRequest(CreateValidationError(validation));
        }

        _logger.LogInformation("Registering device for user {UserId} with platform {Platform}", userId, request.Platform);
        var device = await _notificationService.RegisterDeviceAsync(userId, request, ct);

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
        _logger.LogDebug("Getting devices for user {UserId}", userId);
        var devices = await _notificationService.GetDevicesAsync(userId, ct);
        return Ok(ApiResponse<IReadOnlyList<DeviceTokenDto>>.Success(devices));
    }

    /// <summary>
    /// Unregisters a device.
    /// </summary>
    [HttpDelete("{deviceId:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnregisterDevice(long deviceId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        try
        {
            _logger.LogInformation("Unregistering device {DeviceId} for user {UserId}", deviceId, userId);
            await _notificationService.UnregisterDeviceAsync(userId, deviceId, ct);
            return NoContent();
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "User {UserId} forbidden from unregistering device {DeviceId}", userId, deviceId);
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Unregisters a device by its token. Requires authentication.
    /// </summary>
    /// <remarks>
    /// This endpoint requires the user to be authenticated to unregister a device.
    /// This ensures only authorized users can deactivate device tokens, following a security-first approach.
    /// </remarks>
    [HttpDelete("by-token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UnregisterDeviceByToken([FromQuery] string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256)
        {
            _logger.LogWarning("Invalid token parameter for unregister device by token");
            return BadRequest(CreateError(
                "VALIDATION_ERROR",
                "The 'token' query parameter is required and must be between 1 and 256 characters long."));
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("Unregistering device by token for user {UserId}", userId);
        await _notificationService.UnregisterDeviceByTokenAsync(userId, token, ct);
        return NoContent();
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
