using FestGuide.Application.Dtos;
using FestGuide.DataAccess.Abstractions;
using FestGuide.Domain.Entities;
using FestGuide.Domain.Exceptions;
using FestGuide.Infrastructure;
using FestGuide.Infrastructure.Email;
using FestGuide.Security;
using Microsoft.Extensions.Logging;

namespace FestGuide.Application.Services;

/// <summary>
/// Authentication service implementation.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IEmailService _emailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AuthenticationService> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private const int EmailVerificationTokenExpirationHours = 24;
    private const int PasswordResetTokenExpirationHours = 1;

    public AuthenticationService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthenticationService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _emailVerificationTokenRepository = emailVerificationTokenRepository ?? throw new ArgumentNullException(nameof(emailVerificationTokenRepository));
        _passwordResetTokenRepository = passwordResetTokenRepository ?? throw new ArgumentNullException(nameof(passwordResetTokenRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _dateTimeProvider = dateTimeProvider ?? throw new ArgumentNullException(nameof(dateTimeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress = null, CancellationToken ct = default)
    {
        // Check for existing user
        if (await _userRepository.ExistsByEmailAsync(request.Email, ct))
        {
            throw DuplicateException.UserEmail(request.Email);
        }

        var now = _dateTimeProvider.UtcNow;
        var user = new User
        {
            UserId = 0,
            Email = request.Email,
            EmailNormalized = request.Email.ToLowerInvariant(),
            EmailVerified = false,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            DisplayName = request.DisplayName,
            UserType = request.UserType,
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        };

        await _userRepository.CreateAsync(user, ct);

        // Send verification email
        await SendVerificationEmailInternalAsync(user, ct);

        _logger.LogInformation("User {UserId} registered with email {Email}", user.UserId, user.Email);

        return await GenerateAuthResponseAsync(user, ipAddress, ct);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress = null, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        if (user == null)
        {
            _logger.LogWarning("Login attempt for non-existent user {Email}", request.Email);
            throw AuthenticationException.InvalidCredentials();
        }

        // Check lockout
        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > _dateTimeProvider.UtcNow)
        {
            _logger.LogWarning("Login attempt for locked account {UserId}", user.UserId);
            throw AuthenticationException.AccountLocked(user.LockoutEndUtc.Value);
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            await HandleFailedLoginAsync(user, ct);
            throw AuthenticationException.InvalidCredentials();
        }

        // Reset failed attempts on successful login
        if (user.FailedLoginAttempts > 0)
        {
            await _userRepository.ResetLoginAttemptsAsync(user.UserId, ct);
        }

        _logger.LogInformation("User {UserId} logged in", user.UserId);

        return await GenerateAuthResponseAsync(user, ipAddress, ct);
    }

    /// <inheritdoc />
    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken == null || !storedToken.IsActive)
        {
            _logger.LogWarning("Invalid refresh token attempt");
            throw AuthenticationException.InvalidRefreshToken();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user == null || user.IsDeleted)
        {
            throw AuthenticationException.InvalidRefreshToken();
        }

        // Revoke old token and create new one (rotation)
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newTokenEntity = new RefreshToken
        {
            RefreshTokenId = 0,
            UserId = user.UserId,
            TokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken),
            ExpiresAtUtc = _jwtTokenService.GetRefreshTokenExpiration(),
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.RevokeAsync(storedToken.RefreshTokenId, newTokenEntity.RefreshTokenId, ct);
        await _refreshTokenRepository.CreateAsync(newTokenEntity, ct);

        _logger.LogInformation("Refresh token rotated for user {UserId}", user.UserId);

        return GenerateAuthResponse(user, newRefreshToken, newTokenEntity.ExpiresAtUtc);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken != null && storedToken.IsActive)
        {
            await _refreshTokenRepository.RevokeAsync(storedToken.RefreshTokenId, null, ct);
            _logger.LogInformation("User {UserId} logged out", storedToken.UserId);
        }
    }

    /// <inheritdoc />
    public async Task LogoutAllAsync(long userId, CancellationToken ct = default)
    {
        await _refreshTokenRepository.RevokeAllForUserAsync(userId, ct);
        _logger.LogInformation("All sessions revoked for user {UserId}", userId);
    }

    /// <inheritdoc />
    public async Task<SuccessResponse> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.Token);
        var storedToken = await _emailVerificationTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken == null || !storedToken.IsValid)
        {
            throw AuthenticationException.InvalidVerificationToken();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user == null || user.IsDeleted)
        {
            throw AuthenticationException.InvalidVerificationToken();
        }

        // Mark email as verified
        user.EmailVerified = true;
        user.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        // Mark token as used
        await _emailVerificationTokenRepository.MarkAsUsedAsync(storedToken.TokenId, ct);

        _logger.LogInformation("Email verified for user {UserId}", user.UserId);

        return new SuccessResponse("Email verified successfully.");
    }

    /// <inheritdoc />
    public async Task<SuccessResponse> ResendVerificationEmailAsync(ResendVerificationRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        // Always return success to prevent email enumeration
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Resend verification requested for non-existent email {Email}", request.Email);
            return new SuccessResponse("If that email exists in our system, a verification email has been sent.");
        }

        if (user.EmailVerified)
        {
            _logger.LogInformation("Resend verification requested for already verified user {UserId}", user.UserId);
            return new SuccessResponse("If that email exists in our system, a verification email has been sent.");
        }

        // Invalidate existing tokens and send new one
        await _emailVerificationTokenRepository.InvalidateAllForUserAsync(user.UserId, ct);
        await SendVerificationEmailInternalAsync(user, ct);

        _logger.LogInformation("Verification email resent for user {UserId}", user.UserId);

        return new SuccessResponse("If that email exists in our system, a verification email has been sent.");
    }

    /// <inheritdoc />
    public async Task<SuccessResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, ct);

        // Always return success to prevent email enumeration
        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Password reset requested for non-existent email {Email}", request.Email);
            return new SuccessResponse("If that email exists in our system, a password reset link has been sent.");
        }

        // Invalidate existing tokens and create new one
        await _passwordResetTokenRepository.InvalidateAllForUserAsync(user.UserId, ct);

        var now = _dateTimeProvider.UtcNow;
        var rawToken = _jwtTokenService.GenerateRefreshToken();
        var tokenEntity = new PasswordResetToken
        {
            TokenId = 0,
            UserId = user.UserId,
            TokenHash = _jwtTokenService.HashRefreshToken(rawToken),
            ExpiresAtUtc = now.AddHours(PasswordResetTokenExpirationHours),
            IsUsed = false,
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        };

        await _passwordResetTokenRepository.CreateAsync(tokenEntity, ct);
        await _emailService.SendPasswordResetEmailAsync(user.Email, user.DisplayName, rawToken, ct);

        _logger.LogInformation("Password reset email sent for user {UserId}", user.UserId);

        return new SuccessResponse("If that email exists in our system, a password reset link has been sent.");
    }

    /// <inheritdoc />
    public async Task<SuccessResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.Token);
        var storedToken = await _passwordResetTokenRepository.GetByTokenHashAsync(tokenHash, ct);

        if (storedToken == null || !storedToken.IsValid)
        {
            throw AuthenticationException.InvalidPasswordResetToken();
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId, ct);
        if (user == null || user.IsDeleted)
        {
            throw AuthenticationException.InvalidPasswordResetToken();
        }

        // Update password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.ModifiedAtUtc = _dateTimeProvider.UtcNow;
        await _userRepository.UpdateAsync(user, ct);

        // Mark token as used
        await _passwordResetTokenRepository.MarkAsUsedAsync(storedToken.TokenId, ct);

        // Revoke all refresh tokens for security
        await _refreshTokenRepository.RevokeAllForUserAsync(user.UserId, ct);

        // Send notification
        await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.DisplayName, ct);

        _logger.LogInformation("Password reset completed for user {UserId}", user.UserId);

        return new SuccessResponse("Password has been reset successfully. Please log in with your new password.");
    }

    private async Task SendVerificationEmailInternalAsync(User user, CancellationToken ct)
    {
        var now = _dateTimeProvider.UtcNow;
        var rawToken = _jwtTokenService.GenerateRefreshToken();
        var tokenEntity = new EmailVerificationToken
        {
            TokenId = 0,
            UserId = user.UserId,
            TokenHash = _jwtTokenService.HashRefreshToken(rawToken),
            ExpiresAtUtc = now.AddHours(EmailVerificationTokenExpirationHours),
            IsUsed = false,
            CreatedAtUtc = now,
            ModifiedAtUtc = now
        };

        await _emailVerificationTokenRepository.CreateAsync(tokenEntity, ct);
        await _emailService.SendVerificationEmailAsync(user.Email, user.DisplayName, rawToken, ct);
    }

    private async Task<AuthResponse> GenerateAuthResponseAsync(User user, string? ipAddress, CancellationToken ct)
    {
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenExpiry = _jwtTokenService.GetRefreshTokenExpiration();

        var tokenEntity = new RefreshToken
        {
            RefreshTokenId = 0,
            UserId = user.UserId,
            TokenHash = _jwtTokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc = refreshTokenExpiry,
            CreatedAtUtc = _dateTimeProvider.UtcNow,
            CreatedByIp = ipAddress
        };

        await _refreshTokenRepository.CreateAsync(tokenEntity, ct);

        return GenerateAuthResponse(user, refreshToken, refreshTokenExpiry);
    }

    private AuthResponse GenerateAuthResponse(User user, string refreshToken, DateTime refreshTokenExpiry)
    {
        var accessToken = _jwtTokenService.GenerateAccessToken(
            user.UserId,
            user.Email,
            user.UserType.ToString());

        return new AuthResponse(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.UserType.ToString(),
            accessToken,
            _jwtTokenService.GetAccessTokenExpiration(),
            refreshToken,
            refreshTokenExpiry);
    }

    private async Task HandleFailedLoginAsync(User user, CancellationToken ct)
    {
        var failedAttempts = user.FailedLoginAttempts + 1;
        DateTime? lockoutEnd = null;

        if (failedAttempts >= MaxFailedAttempts)
        {
            lockoutEnd = _dateTimeProvider.UtcNow.AddMinutes(LockoutMinutes);
            _logger.LogWarning("Account {UserId} locked until {LockoutEnd}", user.UserId, lockoutEnd);
        }

        await _userRepository.UpdateLoginAttemptsAsync(user.UserId, failedAttempts, lockoutEnd, ct);
    }
}
