using Microsoft.Extensions.Logging;

namespace DBH.Notification.Service.Services;

public class PushNotificationService : IPushNotificationService
{
    private readonly ILogger<PushNotificationService> _logger;

    public PushNotificationService(ILogger<PushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task<bool> SendPushAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
    {
        // TODO: Implement with Firebase Admin SDK when credentials are available
        _logger.LogInformation("Push notification stub: title={Title}, token={Token}", title, fcmToken[..Math.Min(10, fcmToken.Length)] + "...");
        return Task.FromResult(true);
    }

    public Task<int> SendMulticastAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        // TODO: Implement with Firebase Admin SDK when credentials are available
        _logger.LogInformation("Multicast push stub: title={Title}, recipients={Count}", title, fcmTokens.Count);
        return Task.FromResult(fcmTokens.Count);
    }
}
