namespace FestGuide.Infrastructure.Email;

/// <summary>
/// Service interface for sending emails.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email verification link to the user.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="verificationToken">The verification token.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendVerificationEmailAsync(string email, string displayName, string verificationToken, CancellationToken ct = default);

    /// <summary>
    /// Sends a password reset link to the user.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="resetToken">The password reset token.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendPasswordResetEmailAsync(string email, string displayName, string resetToken, CancellationToken ct = default);

    /// <summary>
    /// Sends a notification that the user's password has been changed.
    /// </summary>
    /// <param name="email">The recipient's email address.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendPasswordChangedNotificationAsync(string email, string displayName, CancellationToken ct = default);

    /// <summary>
    /// Sends a festival invitation email.
    /// </summary>
    /// <param name="toAddress">Recipient email address.</param>
    /// <param name="festivalName">Name of the festival.</param>
    /// <param name="inviterName">Name of the person sending the invitation.</param>
    /// <param name="role">Role being granted.</param>
    /// <param name="isNewUser">Whether the recipient needs to create an account.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendInvitationEmailAsync(string toAddress, string festivalName, string inviterName, string role, bool isNewUser, CancellationToken ct = default);
}
