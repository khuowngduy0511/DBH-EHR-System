using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/v1/notifications/device-tokens")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public DeviceTokensController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// POST /api/device-tokens - Đăng ký device token
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        var result = await _notificationService.RegisterDeviceAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// GET /api/device-tokens/by-user/{userDid} - List devices của user
    /// </summary>
    [HttpGet("by-user/{userDid}")]
    public async Task<IActionResult> GetUserDevices(string userDid)
    {
        var devices = await _notificationService.GetUserDevicesAsync(userDid);
        return Ok(devices);
    }

    /// <summary>
    /// DELETE /api/device-tokens/{id} - Xóa device token
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateDevice(Guid id)
    {
        var result = await _notificationService.DeactivateDeviceAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }

    /// <summary>
    /// DELETE /api/device-tokens/by-user/{userDid}/all - Xóa tất cả devices
    /// </summary>
    [HttpDelete("by-user/{userDid}/all")]
    public async Task<IActionResult> DeactivateAll(string userDid)
    {
        var result = await _notificationService.DeactivateAllDevicesAsync(userDid);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
