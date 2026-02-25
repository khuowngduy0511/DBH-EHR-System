using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/notification-preferences")]
public class PreferencesController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public PreferencesController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // GET /api/notification-preferences/me - Get preferences
    [HttpGet("me")]
    public async Task<ActionResult<PreferencesResponse>> GetMyPreferences()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.GetPreferencesAsync(userDid);
        return Ok(result);
    }

    // PUT /api/notification-preferences/me - Update preferences
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<PreferencesResponse>>> UpdateMyPreferences([FromBody] UpdatePreferencesRequest request)
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.UpdatePreferencesAsync(userDid, request);
        return Ok(result);
    }
}
