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
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        var userId = GetCurrentUserId();
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
        var count = await _notificationService.GetUnreadCountAsync(userId, ct);
        return Ok(ApiResponse<UnreadCountDto>.Success(new UnreadCountDto(count)));
    }

    /// <summary>
    /// Marks a notification as read.
    /// </summary>
    [HttpPost("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        try
        {
            await _notificationService.MarkAsReadAsync(userId, notificationId, ct);
            return NoContent();
        }
        catch (ForbiddenException ex)
        {
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
            return BadRequest(CreateValidationError(validation));
        }

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
