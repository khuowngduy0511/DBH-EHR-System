using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/v1/notifications/preferences")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public PreferencesController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// GET /api/notification-preferences/by-user/{userDid} - Get preferences
    /// </summary>
    [HttpGet("by-user/{userDid}")]
    public async Task<IActionResult> GetPreferences(string userDid)
    {
        var result = await _notificationService.GetPreferencesAsync(userDid);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/notification-preferences/by-user/{userDid} - Update preferences
    /// </summary>
    [HttpPut("by-user/{userDid}")]
    public async Task<IActionResult> UpdatePreferences(string userDid, [FromBody] UpdatePreferencesRequest request)
    {
        var result = await _notificationService.UpdatePreferencesAsync(userDid, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
