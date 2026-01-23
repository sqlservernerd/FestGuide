using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for authentication operations.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, CancellationToken ct = default);

    /// <summary>
    /// Authenticates a user and returns tokens.
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken ct = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken ct = default);

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    Task LogoutAsync(LogoutRequest request, CancellationToken ct = default);

    /// <summary>
    /// Logs out a user from all devices by revoking all refresh tokens.
    /// </summary>
    Task LogoutAllAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    Task<SuccessResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default);

    /// <summary>
    /// Resends the email verification link to the user.
    /// </summary>
    Task<SuccessResponse> ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default);

    /// <summary>
    /// Initiates the password reset process by sending a reset link to the user's email.
    /// </summary>
    Task<SuccessResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    /// <summary>
    /// Resets the user's password using the reset token.
    /// </summary>
    Task<SuccessResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
