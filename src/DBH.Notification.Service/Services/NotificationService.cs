using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;

namespace DBH.Notification.Service.Services;

public class NotificationService : INotificationService
{
    public Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request) => throw new NotImplementedException();
    public Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request) => throw new NotImplementedException();
    public Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize) => throw new NotImplementedException();
    public Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid) => throw new NotImplementedException();
    public Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request) => throw new NotImplementedException();
    public Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid) => throw new NotImplementedException();
    public Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId) => throw new NotImplementedException();
    public Task<int> GetUnreadCountAsync(string userDid) => throw new NotImplementedException();
    public Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request) => throw new NotImplementedException();
    public Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid) => throw new NotImplementedException();
    public Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId) => throw new NotImplementedException();
    public Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid) => throw new NotImplementedException();
    public Task<PreferencesResponse> GetPreferencesAsync(string userDid) => throw new NotImplementedException();
    public Task<ApiResponse<PreferencesResponse>> UpdatePreferencesAsync(string userDid, UpdatePreferencesRequest request) => throw new NotImplementedException();
}
