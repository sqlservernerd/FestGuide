using FestGuide.Domain.Entities;
using FestGuide.Domain.Enums;

namespace FestGuide.Application.Dtos;

/// <summary>
/// Response DTO for user profile.
/// </summary>
public sealed record UserProfileDto(
    long UserId,
    string Email,
    bool EmailVerified,
    string DisplayName,
    UserType UserType,
    string? PreferredTimezoneId,
    DateTime CreatedAtUtc)
{
    public static UserProfileDto FromEntity(User user) =>
        new(
            user.UserId,
            user.Email,
            user.EmailVerified,
            user.DisplayName,
            user.UserType,
            user.PreferredTimezoneId,
            user.CreatedAtUtc);
}

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public sealed record UpdateProfileRequest(
    string? DisplayName,
    string? PreferredTimezoneId);

/// <summary>
/// Response DTO for user data export (GDPR).
/// </summary>
public sealed record UserDataExportDto(
    long UserId,
    string Email,
    bool EmailVerified,
    string DisplayName,
    UserType UserType,
    string? PreferredTimezoneId,
    DateTime CreatedAtUtc,
    DateTime ModifiedAtUtc);
