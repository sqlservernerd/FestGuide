using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// User service implementation.
/// </summary>
public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        return UserProfileDto.FromEntity(user);
    }

    /// <inheritdoc />
    public async Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        if (!string.IsNullOrEmpty(request.DisplayName))
        {
            user.DisplayName = request.DisplayName;
        }

        if (request.PreferredTimezoneId != null)
        {
            user.PreferredTimezoneId = request.PreferredTimezoneId;
        }

        user.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        user.ModifiedBy = userId;

        await _userRepository.UpdateAsync(user, ct);

        _logger.LogInformation("User {UserId} updated profile", userId);

        return UserProfileDto.FromEntity(user);
    }

    /// <inheritdoc />
    public async Task DeleteAccountAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        // Revoke all refresh tokens
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, ct);

        // Soft delete the user
        await _userRepository.DeleteAsync(userId, ct);

        _logger.LogInformation("User {UserId} account deleted (GDPR erasure)", userId);
    }

    /// <inheritdoc />
    public async Task<UserDataExportDto> ExportDataAsync(long userId, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new UserNotFoundException(userId);

        _logger.LogInformation("User {UserId} exported data (GDPR portability)", userId);

        return new UserDataExportDto(
            user.UserId,
            user.Email,
            user.EmailVerified,
            user.DisplayName,
            user.UserType,
            user.PreferredTimezoneId,
            user.CreatedAtUtc,
            user.ModifiedAtUtc);
    }
}
