using DBH.Notification.Service.DTOs;

namespace DBH.Notification.Service.Services;

public interface INotificationService
{
    Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request);
    Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request);
    Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize);
    Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid);
    Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request);
    Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid);
    Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId);
    Task<int> GetUnreadCountAsync(string userDid);
}
