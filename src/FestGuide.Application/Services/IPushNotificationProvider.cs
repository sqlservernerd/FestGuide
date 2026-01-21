using FestGuide.Application.Dtos;

namespace FestGuide.Application.Services;

/// <summary>
/// Interface for push notification providers (FCM, APNS, etc.).
/// </summary>
public interface IPushNotificationProvider
{
    /// <summary>
    /// Sends a push notification to a device.
    /// </summary>
    /// <param name="deviceToken">The device token.</param>
    /// <param name="platform">The platform (ios, android, web).</param>
    /// <param name="message">The notification message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendAsync(string deviceToken, string platform, PushNotificationMessage message, CancellationToken ct = default);

    /// <summary>
    /// Sends push notifications to multiple devices.
    /// </summary>
    /// <param name="deviceTokens">The device tokens with their platforms.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SendBatchAsync(IEnumerable<(string Token, string Platform)> deviceTokens, PushNotificationMessage message, CancellationToken ct = default);
}
