using FluentValidation;
using FestConnect.Api.Models;
using FestConnect.Application.Dtos;
using FestConnect.Application.Services;
using FestConnect.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FestConnect.Api.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and token management.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthenticationService authService,
        IValidator<RegisterRequest> registerValidator,
        IValidator<LoginRequest> loginValidator,
        IValidator<RefreshTokenRequest> refreshValidator,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RegisterAsync(request, ipAddress, ct);
            return CreatedAtAction(nameof(Register), ApiResponse<AuthResponse>.Success(response));
        }
        catch (DuplicateException ex)
        {
            return Conflict(CreateError("DUPLICATE_EMAIL", ex.Message));
        }
    }

    /// <summary>
    /// Authenticates a user and returns access and refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.LoginAsync(request, ipAddress, ct);
            return Ok(ApiResponse<AuthResponse>.Success(response));
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(CreateError("AUTHENTICATION_FAILED", ex.Message));
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return BadRequest(CreateValidationError(validation));
        }

        try
        {
            var ipAddress = GetIpAddress();
            var response = await _authService.RefreshTokenAsync(request, ipAddress, ct);
            return Ok(ApiResponse<AuthResponse>.Success(response));
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(CreateError("INVALID_REFRESH_TOKEN", ex.Message));
        }
    }

    /// <summary>
    /// Logs out the user by revoking the refresh token.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        await _authService.LogoutAsync(request, ct);
        return NoContent();
    }

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SuccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(CreateError("VALIDATION_ERROR", "Token is required."));
        }

        try
        {
            var response = await _authService.VerifyEmailAsync(request, ct);
            return Ok(ApiResponse<SuccessResponse>.Success(response));
        }
        catch (AuthenticationException ex)
        {
            return BadRequest(CreateError("INVALID_TOKEN", ex.Message));
        }
    }

    /// <summary>
    /// Resends the email verification link to the user.
    /// </summary>
    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SuccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(CreateError("VALIDATION_ERROR", "Email is required."));
        }

        var response = await _authService.ResendVerificationEmailAsync(request, ct);
        return Ok(ApiResponse<SuccessResponse>.Success(response));
    }

    /// <summary>
    /// Initiates the password reset process by sending a reset link to the user's email.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SuccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(CreateError("VALIDATION_ERROR", "Email is required."));
        }

        var response = await _authService.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse<SuccessResponse>.Success(response));
    }

    /// <summary>
    /// Resets the user's password using the reset token.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<SuccessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(CreateError("VALIDATION_ERROR", "Token is required."));
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(CreateError("VALIDATION_ERROR", "New password is required."));
        }

        try
        {
            var response = await _authService.ResetPasswordAsync(request, ct);
            return Ok(ApiResponse<SuccessResponse>.Success(response));
        }
        catch (AuthenticationException ex)
        {
            return BadRequest(CreateError("INVALID_TOKEN", ex.Message));
        }
    }

    private string? GetIpAddress()
    {
        if (Request?.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) == true)
        {
            return forwardedFor.ToString().Split(',').FirstOrDefault()?.Trim();
        }
        return HttpContext?.Connection?.RemoteIpAddress?.ToString();
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
