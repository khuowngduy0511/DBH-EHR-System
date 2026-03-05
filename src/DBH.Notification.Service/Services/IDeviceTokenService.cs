using DBH.Notification.Service.DTOs;

namespace DBH.Notification.Service.Services;

public interface IDeviceTokenService
{
    Task<ApiResponse<DeviceTokenResponse>> RegisterDeviceAsync(RegisterDeviceRequest request);
    Task<List<DeviceTokenResponse>> GetUserDevicesAsync(string userDid);
    Task<ApiResponse<bool>> DeactivateDeviceAsync(Guid deviceTokenId);
    Task<ApiResponse<bool>> DeactivateAllDevicesAsync(string userDid);
}
