using FestGuide.Application.Dtos;
using FestGuide.Application.Services;
using Microsoft.Extensions.Logging;

namespace FestGuide.Integrations.PushNotifications;

/// <summary>
/// Stub implementation of push notification provider for development/testing.
/// Replace with real Firebase/APNS implementation in production.
/// </summary>
public class StubPushNotificationProvider : IPushNotificationProvider
{
    private readonly ILogger<StubPushNotificationProvider> _logger;

    public StubPushNotificationProvider(ILogger<StubPushNotificationProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task SendAsync(string deviceToken, string platform, PushNotificationMessage message, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[STUB] Push notification sent to {Platform} device: Title='{Title}', Body='{Body}'",
            platform,
            message.Title,
            message.Body);

        // Simulate successful delivery
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SendBatchAsync(IEnumerable<(string Token, string Platform)> deviceTokens, PushNotificationMessage message, CancellationToken ct = default)
    {
        foreach (var (token, platform) in deviceTokens)
        {
            await SendAsync(token, platform, message, ct);
        }
    }
}
