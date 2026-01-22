using System.Net;
using System.Net.Mail;
using FestGuide.Infrastructure;
using FestGuide.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FestGuide.Integrations.Email;

/// <summary>
/// SMTP-based email service implementation.
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SendVerificationEmailAsync(string email, string displayName, string verificationToken, CancellationToken ct = default)
    {
        var subject = "Verify your FestGuide account";
        var htmlBody = BuildEmailTemplate(
            "Verify Your Email",
            $@"<p>Hi {displayName},</p>
               <p>Please verify your email address by clicking the button below:</p>
               <p><a href=""#"" style=""background-color: #6366f1; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;"">Verify Email</a></p>
               <p>This link will expire in 24 hours.</p>
               <p>If you didn't create a FestGuide account, you can ignore this email.</p>");

        await SendEmailAsync(email, subject, htmlBody, null, ct);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string displayName, string resetToken, CancellationToken ct = default)
    {
        var subject = "Reset your FestGuide password";
        var htmlBody = BuildEmailTemplate(
            "Reset Your Password",
            $@"<p>Hi {displayName},</p>
               <p>We received a request to reset your password. Click the button below to set a new password:</p>
               <p><a href=""#"" style=""background-color: #6366f1; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;"">Reset Password</a></p>
               <p>This link will expire in 1 hour.</p>
               <p>If you didn't request a password reset, you can ignore this email.</p>");

        await SendEmailAsync(email, subject, htmlBody, null, ct);
    }

    /// <inheritdoc />
    public async Task SendPasswordChangedNotificationAsync(string email, string displayName, CancellationToken ct = default)
    {
        var subject = "Your FestGuide password has been changed";
        var htmlBody = BuildEmailTemplate(
            "Password Changed",
            $@"<p>Hi {displayName},</p>
               <p>Your password has been successfully changed.</p>
               <p>If you didn't make this change, please contact support immediately.</p>");

        await SendEmailAsync(email, subject, htmlBody, null, ct);
    }

    /// <inheritdoc />
    public async Task SendInvitationEmailAsync(string toAddress, string festivalName, string inviterName, string role, bool isNewUser, CancellationToken ct = default)
    {
        var subject = $"You've been invited to join {festivalName} on FestGuide";

        var registerMessage = isNewUser
            ? "<p>To accept this invitation, you'll need to create a FestGuide account first. Once registered, you'll have access to the festival.</p>"
            : "<p>Log in to your FestGuide account to access the festival.</p>";

        var htmlBody = BuildEmailTemplate(
            "You're Invited!",
            $@"<p><strong>{inviterName}</strong> has invited you to join <strong>{festivalName}</strong> as a team member.</p>
               <p>Your role: <span style=""display: inline-block; background-color: #6366f1; color: white; padding: 4px 12px; border-radius: 4px; font-size: 14px;"">{role}</span></p>
               {registerMessage}
               <p>With FestGuide, you can:</p>
               <ul>
                   <li>Manage festival schedules and performances</li>
                   <li>Coordinate with your team</li>
                   <li>Track attendee engagement</li>
               </ul>");

        await SendEmailAsync(toAddress, subject, htmlBody, null, ct);
    }

    private async Task SendEmailAsync(string toAddress, string subject, string htmlBody, string? plainTextBody, CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            _logger.LogDebug("Email sending is disabled. Would have sent email to {ToAddress} with subject: {Subject}", toAddress, subject);
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.Username))
        {
            _logger.LogWarning("SMTP is not configured. Skipping email to {ToAddress}", toAddress);
            return;
        }

        try
        {
            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = _options.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress)
                ? _options.Username
                : _options.FromAddress;

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(new MailAddress(toAddress));

            await client.SendMailAsync(message, ct);

            _logger.LogInformation("Email sent successfully to {ToAddress} with subject: {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject: {Subject}", toAddress, subject);
            throw;
        }
    }

    private static string BuildEmailTemplate(string title, string content)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #6366f1; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background-color: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .footer {{ margin-top: 20px; font-size: 12px; color: #6b7280; text-align: center; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>🎵 FestGuide</h1>
        </div>
        <div class=""content"">
            <h2>{title}</h2>
            {content}
        </div>
        <div class=""footer"">
            <p>This email was sent by FestGuide. If you didn't expect this email, you can safely ignore it.</p>
        </div>
    </div>
</body>
</html>";
    }
}
