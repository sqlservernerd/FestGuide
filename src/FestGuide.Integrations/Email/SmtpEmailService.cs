using System.Text.Encodings.Web;
using FestGuide.Infrastructure;
using FestGuide.Infrastructure.Email;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

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

        // Validate required SMTP settings at startup when email sending is enabled
        if (_options.Enabled)
        {
            ValidateConfiguration();
        }
    }

    /// <summary>
    /// Validates SMTP configuration settings at startup to fail fast if misconfigured.
    /// </summary>
    private void ValidateConfiguration()
    {
        var missingSettings = new List<string>();

        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            missingSettings.Add("Host");
        }

        if (string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            missingSettings.Add("FromAddress");
        }

        if (string.IsNullOrWhiteSpace(_options.Username))
        {
            missingSettings.Add("Username");
        }

        if (string.IsNullOrWhiteSpace(_options.Password))
        {
            missingSettings.Add("Password");
        }

        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            missingSettings.Add("BaseUrl");
        }
        else if (!Uri.TryCreate(_options.BaseUrl, UriKind.Absolute, out _))
        {
            _logger.LogError("BaseUrl '{BaseUrl}' is not a valid absolute URI", _options.BaseUrl);
            throw new InvalidOperationException($"BaseUrl '{_options.BaseUrl}' is not a valid absolute URI.");
        }

        if (missingSettings.Count > 0)
        {
            var errorMessage = $"SMTP email sending is enabled but the following required settings are not configured: {string.Join(", ", missingSettings)}. " +
                "Configure these values using user secrets, environment variables, or a secure configuration provider.";
            
            _logger.LogError(errorMessage);
            throw new InvalidOperationException(errorMessage);
        }
    }

    /// <inheritdoc />
    public async Task SendVerificationEmailAsync(string email, string displayName, string verificationToken, CancellationToken ct = default)
    {
        var subject = "Verify your FestGuide account";
        var verificationUrl = BuildUrl($"verify-email?token={Uri.EscapeDataString(verificationToken)}");
        var encodedDisplayName = HtmlEncoder.Default.Encode(displayName);
        var htmlBody = BuildEmailTemplate(
            "Verify Your Email",
            $@"<p>Hi {encodedDisplayName},</p>
               <p>Please verify your email address by clicking the button below:</p>
               <p><a href=""{verificationUrl}"" style=""background-color: #6366f1; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;"">Verify Email</a></p>
               <p>This link will expire in 24 hours.</p>
               <p>If you didn't create a FestGuide account, you can ignore this email.</p>");

        await SendEmailAsync(email, subject, htmlBody, null, ct);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(string email, string displayName, string resetToken, CancellationToken ct = default)
    {
        var subject = "Reset your FestGuide password";
        var resetUrl = BuildUrl($"reset-password?token={Uri.EscapeDataString(resetToken)}");
        var encodedDisplayName = HtmlEncoder.Default.Encode(displayName);
        var htmlBody = BuildEmailTemplate(
            "Reset Your Password",
            $@"<p>Hi {encodedDisplayName},</p>
               <p>We received a request to reset your password. Click the button below to set a new password:</p>
               <p><a href=""{resetUrl}"" style=""background-color: #6366f1; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;"">Reset Password</a></p>
               <p>This link will expire in 1 hour.</p>
               <p>If you didn't request a password reset, you can ignore this email.</p>");

        await SendEmailAsync(email, subject, htmlBody, null, ct);
    }

    /// <inheritdoc />
    public async Task SendPasswordChangedNotificationAsync(string email, string displayName, CancellationToken ct = default)
    {
        var subject = "Your FestGuide password has been changed";
        var encodedDisplayName = HtmlEncoder.Default.Encode(displayName);
        var htmlBody = BuildEmailTemplate(
            "Password Changed",
            $@"<p>Hi {encodedDisplayName},</p>
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

        var encodedFestivalName = HtmlEncoder.Default.Encode(festivalName);
        var encodedInviterName = HtmlEncoder.Default.Encode(inviterName);
        var encodedRole = HtmlEncoder.Default.Encode(role);

        var htmlBody = BuildEmailTemplate(
            "You're Invited!",
            $@"<p><strong>{encodedInviterName}</strong> has invited you to join <strong>{encodedFestivalName}</strong> as a team member.</p>
               <p>Your role: <span style=""display: inline-block; background-color: #6366f1; color: white; padding: 4px 12px; border-radius: 4px; font-size: 14px;"">{encodedRole}</span></p>
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

        try
        {
            using var message = new MimeMessage();
            
            var fromAddress = string.IsNullOrWhiteSpace(_options.FromAddress)
                ? _options.Username
                : _options.FromAddress;

            message.From.Add(new MailboxAddress(_options.FromName, fromAddress));
            message.To.Add(new MailboxAddress(string.Empty, toAddress));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = plainTextBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            
            await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, ct);
            
            if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email sent successfully to {ToAddress} with subject {Subject}", toAddress, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToAddress} with subject {Subject}", toAddress, subject);
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

    private string BuildUrl(string path)
    {
        // BaseUrl is already validated at startup if email is enabled,
        // but we keep this check as a defensive measure
        var baseUrl = _options.BaseUrl?.TrimEnd('/');
        
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "BaseUrl is not configured in SmtpOptions. This should have been caught during service initialization.");
        }

        return $"{baseUrl}/{path.TrimStart('/')}";
    }
}
