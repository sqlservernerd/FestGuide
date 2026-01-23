using Microsoft.Extensions.Logging;

namespace FestConnect.Infrastructure.Email;

/// <summary>
/// Development email service that logs emails to the console.
/// Replace with a real SMTP or email provider implementation in production.
/// </summary>
public class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;
    private readonly string _baseUrl;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger, string baseUrl = "https://localhost:5001")
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _baseUrl = baseUrl;
    }

    /// <inheritdoc />
    public Task SendVerificationEmailAsync(string email, string displayName, string verificationToken, CancellationToken ct = default)
    {
        var verificationUrl = $"{_baseUrl}/api/v1/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}";

        _logger.LogInformation(
            """
            ========================================
            EMAIL VERIFICATION
            ========================================
            To: {Email}
            Name: {DisplayName}
            
            Subject: Verify your FestConnect account
            
            Hi {DisplayName},
            
            Please verify your email address by clicking the link below:
            
            {VerificationUrl}
            
            This link will expire in 24 hours.
            
            If you didn't create a FestConnect account, you can ignore this email.
            ========================================
            """,
            email, displayName, displayName, verificationUrl);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordResetEmailAsync(string email, string displayName, string resetToken, CancellationToken ct = default)
    {
        var resetUrl = $"{_baseUrl}/reset-password?token={Uri.EscapeDataString(resetToken)}";

        _logger.LogInformation(
            """
            ========================================
            PASSWORD RESET
            ========================================
            To: {Email}
            Name: {DisplayName}
            
            Subject: Reset your FestConnect password
            
            Hi {DisplayName},
            
            We received a request to reset your password. Click the link below to set a new password:
            
            {ResetUrl}
            
            This link will expire in 1 hour.
            
            If you didn't request a password reset, you can ignore this email. Your password will remain unchanged.
            ========================================
            """,
            email, displayName, displayName, resetUrl);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendPasswordChangedNotificationAsync(string email, string displayName, CancellationToken ct = default)
    {
        _logger.LogInformation(
            """
            ========================================
            PASSWORD CHANGED
            ========================================
            To: {Email}
            Name: {DisplayName}
            
            Subject: Your FestConnect password has been changed
            
            Hi {DisplayName},
            
            Your password has been successfully changed.
            
            If you didn't make this change, please contact support immediately.
            ========================================
            """,
            email, displayName, displayName);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SendInvitationEmailAsync(string toAddress, string festivalName, string inviterName, string role, bool isNewUser, CancellationToken ct = default)
    {
        var accountMessage = isNewUser
            ? "You'll need to create a FestConnect account to accept this invitation."
            : "Log in to your FestConnect account to access the festival.";

        _logger.LogInformation(
            """
            ========================================
            FESTIVAL INVITATION
            ========================================
            To: {ToAddress}
            
            Subject: You've been invited to join {FestivalName} on FestConnect
            
            {InviterName} has invited you to join {FestivalName} as a team member.
            
            Your role: {Role}
            
            {AccountMessage}
            
            With FestConnect, you can:
            - Manage festival schedules and performances
            - Coordinate with your team
            - Track attendee engagement
            ========================================
            """,
            toAddress, festivalName, inviterName, festivalName, role, accountMessage);

        return Task.CompletedTask;
    }
}
