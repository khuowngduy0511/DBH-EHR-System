namespace DBH.Notification.Service.Services;

public class PushNotificationService : IPushNotificationService
{
    public Task<bool> SendPushAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null) => throw new NotImplementedException();
    public Task<int> SendMulticastAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null) => throw new NotImplementedException();
}
