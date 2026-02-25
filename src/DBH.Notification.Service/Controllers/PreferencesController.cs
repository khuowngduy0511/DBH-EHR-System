using System.Security.Claims;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/notification-preferences")]
[Authorize]
public class PreferencesController : ControllerBase
{
    private readonly IPreferencesService _preferencesService;

    public PreferencesController(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;
    }

    // GET /api/notification-preferences/me - Get preferences
    [HttpGet("me")]
    public async Task<ActionResult<PreferencesResponse>> GetMyPreferences()
    {
        var userId = GetCurrentUserId();
        var result = await _preferencesService.GetPreferencesAsync(userId);
        return Ok(result);
    }

    // PUT /api/notification-preferences/me - Update preferences
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<PreferencesResponse>>> UpdateMyPreferences([FromBody] UpdatePreferencesRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _preferencesService.UpdatePreferencesAsync(userId, request);
        return Ok(result);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
