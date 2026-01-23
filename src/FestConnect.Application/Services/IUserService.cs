using FestConnect.Application.Dtos;

namespace FestConnect.Application.Services;

/// <summary>
/// Service interface for user management operations.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user's profile by ID.
    /// </summary>
    Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Updates a user's profile.
    /// </summary>
    Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>
    /// Deletes a user account (GDPR erasure).
    /// </summary>
    Task DeleteAccountAsync(long userId, CancellationToken ct = default);

    /// <summary>
    /// Exports user data (GDPR portability).
    /// </summary>
    Task<UserDataExportDto> ExportDataAsync(long userId, CancellationToken ct = default);
}
