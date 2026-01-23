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
/// User profile management endpoints.
/// </summary>
[ApiController]
[Route("api/v1/profile")]
[Produces("application/json")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthenticationService _authService;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IUserService userService,
        IAuthenticationService authService,
        IValidator<UpdateProfileRequest> updateValidator,
        ILogger<ProfileController> logger)
    {
        _userService = userService;
        _authService = authService;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(CreateError("UNAUTHORIZED", "User not authenticated."));
        }

        try
        {
            var profile = await _userService.GetProfileAsync(userId.Value, ct);
            return Ok(ApiResponse<UserProfileDto>.Success(profile));
        }
        catch (UserNotFoundException)
        {
            return NotFound(CreateError("USER_NOT_FOUND", "User not found."));
        }
    }

    /// <summary>
    /// Updates the current user's profile.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<UserProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(CreateError("UNAUTHORIZED", "User not authenticated."));
        }

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var profile = await _userService.UpdateProfileAsync(userId.Value, request, ct);
            return Ok(ApiResponse<UserProfileDto>.Success(profile));
        }
        catch (UserNotFoundException)
        {
            return NotFound(CreateError("USER_NOT_FOUND", "User not found."));
        }
    }

    /// <summary>
    /// Deletes the current user's account (GDPR erasure).
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(CreateError("UNAUTHORIZED", "User not authenticated."));
        }

        await _userService.DeleteAccountAsync(userId.Value, ct);
        return NoContent();
    }

    /// <summary>
    /// Exports the current user's data (GDPR portability).
    /// </summary>
    [HttpGet("export")]
    [ProducesResponseType(typeof(ApiResponse<UserDataExportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportData(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(CreateError("UNAUTHORIZED", "User not authenticated."));
        }

        try
        {
            var data = await _userService.ExportDataAsync(userId.Value, ct);
            return Ok(ApiResponse<UserDataExportDto>.Success(data));
        }
        catch (UserNotFoundException)
        {
            return NotFound(CreateError("USER_NOT_FOUND", "User not found."));
        }
    }

    /// <summary>
    /// Logs out from all devices by revoking all refresh tokens.
    /// </summary>
    [HttpPost("logout-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            return Unauthorized(CreateError("UNAUTHORIZED", "User not authenticated."));
        }

        await _authService.LogoutAllAsync(userId.Value, ct);
        return NoContent();
    }

    private long? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

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
