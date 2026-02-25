using System.Security.Claims;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Helpers;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/device-tokens")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;

    public DeviceTokensController(IDeviceTokenService deviceTokenService)
    {
        _deviceTokenService = deviceTokenService;
    }

    // POST /api/device-tokens - Đăng ký device
    [HttpPost]
    public async Task<ActionResult<ApiResponse<DeviceTokenResponse>>> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        // Set UserDid from JWT token
        request.UserDid ??= GetCurrentUserId();

        // Auto-extract device info from User-Agent if not provided by client
        var userAgent = Request.Headers.UserAgent.ToString();
        var deviceInfo = UserAgentParser.Parse(userAgent);

        request.DeviceType ??= deviceInfo.DeviceType;
        request.DeviceName ??= deviceInfo.DeviceName;
        request.OsVersion ??= deviceInfo.OsVersion;
        request.AppVersion ??= deviceInfo.AppVersion;

        var result = await _deviceTokenService.RegisterDeviceAsync(request);
        return Ok(result);
    }

    // GET /api/device-tokens/me - List devices của user
    [HttpGet("me")]
    public async Task<ActionResult<List<DeviceTokenResponse>>> GetMyDevices()
    {
        var userId = GetCurrentUserId();
        var result = await _deviceTokenService.GetUserDevicesAsync(userId);
        return Ok(result);
    }

    // DELETE /api/device-tokens/{id} - Xóa device token
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateDevice(Guid id)
    {
        var result = await _deviceTokenService.DeactivateDeviceAsync(id);
        return Ok(result);
    }

    // DELETE /api/device-tokens/me/all - Xóa tất cả devices
    [HttpDelete("me/all")]
    public async Task<ActionResult<ApiResponse<bool>>> DeactivateAllDevices()
    {
        var userId = GetCurrentUserId();
        var result = await _deviceTokenService.DeactivateAllDevicesAsync(userId);
        return Ok(result);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
