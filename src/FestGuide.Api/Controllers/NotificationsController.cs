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
/// Endpoints for managing notifications and preferences.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : BaseApiController
{
    private readonly INotificationService _notificationService;
    private readonly IValidator<UpdateNotificationPreferenceRequest> _preferenceValidator;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        IValidator<UpdateNotificationPreferenceRequest> preferenceValidator,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _preferenceValidator = preferenceValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets notifications for the current user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        if (limit <= 0 || limit > 100)
        {
            _logger.LogWarning("Invalid limit parameter: {Limit}. Must be between 1 and 100.", limit);
            return BadRequest(CreateError("VALIDATION_ERROR", "The 'limit' parameter must be between 1 and 100."));
        }

        if (offset < 0)
        {
            _logger.LogWarning("Invalid offset parameter: {Offset}. Must be non-negative.", offset);
            return BadRequest(CreateError("VALIDATION_ERROR", "The 'offset' parameter must be non-negative."));
        }

        var userId = GetCurrentUserId();
        _logger.LogInformation("Getting notifications for user {UserId} with limit {Limit} and offset {Offset}", userId, limit, offset);
        var notifications = await _notificationService.GetNotificationsAsync(userId, limit, offset, ct);
        return Ok(ApiResponse<IReadOnlyList<NotificationDto>>.Success(notifications));
    }

    /// <summary>
    /// Gets unread notification count.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<UnreadCountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        _logger.LogDebug("Getting unread notification count for user {UserId}", userId);
        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse<UnreadCountDto>.Success(new UnreadCountDto(count)));
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPost("{notificationId:long}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(long notificationId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        try
        {
            _logger.LogInformation("Marking notification {NotificationId} as read for user {UserId}", notificationId, userId);
            await _notificationService.MarkAsReadAsync(userId, notificationId, ct);
            return NoContent();
        }
        catch (ForbiddenException ex)
        {
            _logger.LogWarning(ex, "User {UserId} forbidden from marking notification {NotificationId} as read", userId, notificationId);
            return StatusCode(StatusCodes.Status403Forbidden, CreateError("FORBIDDEN", ex.Message));
        }
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        _logger.LogInformation("Marking all notifications as read for user {UserId}", userId);
        await _notificationService.MarkAllAsReadAsync(userId, ct);
        return NoContent();
    }

    /// <summary>
    /// Gets notification preferences.
    /// </summary>
    [HttpGet("preferences")]
    [ProducesResponseType(typeof(ApiResponse<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPreferences(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        _logger.LogDebug("Getting notification preferences for user {UserId}", userId);
        var prefs = await _notificationService.GetPreferencesAsync(userId, ct);
        return Ok(ApiResponse<NotificationPreferenceDto>.Success(prefs));
    }

    /// <summary>
    /// Updates notification preferences.
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(typeof(ApiResponse<NotificationPreferenceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdatePreferences(
        [FromBody] UpdateNotificationPreferenceRequest request,
        CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var validation = await _preferenceValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Validation failed for updating notification preferences for user {UserId}", userId);
            return BadRequest(CreateValidationError(validation));
        }

        _logger.LogInformation("Updating notification preferences for user {UserId}", userId);
        var prefs = await _notificationService.UpdatePreferencesAsync(userId, request, ct);
        return Ok(ApiResponse<NotificationPreferenceDto>.Success(prefs));
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
