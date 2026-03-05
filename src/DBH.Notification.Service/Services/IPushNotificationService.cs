namespace DBH.Notification.Service.Services;

public interface IPushNotificationService
{
    Task<bool> SendPushAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    Task<int> SendMulticastAsync(List<string> fcmTokens, string title, string body, Dictionary<string, string>? data = null);
}
