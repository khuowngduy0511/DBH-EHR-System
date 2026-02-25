using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/device-tokens")]
public class DeviceTokensController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public DeviceTokensController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // POST /api/device-tokens - Đăng ký device
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var result = await _notificationService.RegisterDeviceAsync(request);
        return Ok(result);
    }

    // GET /api/device-tokens/me - List devices của user
    [HttpGet("me")]
    public async Task<ActionResult<List<DeviceTokenResponse>>> GetMyDevices()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.GetUserDevicesAsync(userDid);
        return Ok(result);
    }

    // DELETE /api/device-tokens/{id} - Xóa device token
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateDevice(Guid id)
    {
        var result = await _notificationService.DeactivateDeviceAsync(id);
        return Ok(result);
    }

    // DELETE /api/device-tokens/me/all - Xóa tất cả devices
    [HttpDelete("me/all")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateAllDevices()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.DeactivateAllDevicesAsync(userDid);
        return Ok(result);
    }
}
