using FestGuide.Domain.Enums;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    UserType UserType);

/// <summary>
/// Request DTO for user login.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password);

/// <summary>
/// Request DTO for token refresh.
/// </summary>
public sealed record RefreshTokenRequest(
    string RefreshToken);

/// <summary>
/// Response DTO for authentication operations.
/// </summary>
public sealed record AuthResponse(
    long UserId,
    string Email,
    string DisplayName,
    string UserType,
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>
/// Request DTO for logout.
/// </summary>
public sealed record LogoutRequest(
    string RefreshToken);

/// <summary>
/// Request DTO for email verification.
/// </summary>
public sealed record VerifyEmailRequest(
    string Token);

/// <summary>
/// Request DTO for resending verification email.
/// </summary>
public sealed record ResendVerificationRequest(
    string Email);

/// <summary>
/// Request DTO for initiating password reset.
/// </summary>
public sealed record ForgotPasswordRequest(
    string Email);

/// <summary>
/// Request DTO for completing password reset.
/// </summary>
public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);

/// <summary>
/// Simple success response for operations without data.
/// </summary>
public sealed record SuccessResponse(
    string Message);
