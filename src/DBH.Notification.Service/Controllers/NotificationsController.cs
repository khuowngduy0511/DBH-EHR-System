using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// POST /api/notifications - Gửi notification (internal use)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest request)
    {
        var result = await _notificationService.SendNotificationAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST /api/v1/notifications/broadcast - Broadcast (admin)
    /// </summary>
    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastNotificationRequest request)
    {
        var result = await _notificationService.BroadcastNotificationAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// GET /api/v1/notifications/by-user/{userDid} - List notifications của user
    /// </summary>
    [HttpGet("by-user/{userDid}")]
    [Authorize]
    public async Task<IActionResult> GetByUser(string userDid, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetNotificationsByUserAsync(userDid, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/notifications/by-user/{userDid}/unread - Unread notifications
    /// </summary>
    [HttpGet("by-user/{userDid}/unread")]
    [Authorize]
    public async Task<IActionResult> GetUnread(string userDid, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _notificationService.GetUnreadNotificationsAsync(userDid, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/notifications/by-user/{userDid}/unread-count - Đếm unread
    /// </summary>
    [HttpGet("by-user/{userDid}/unread-count")]
    [Authorize]
    public async Task<IActionResult> GetUnreadCount(string userDid)
    {
        var count = await _notificationService.GetUnreadCountAsync(userDid);
        return Ok(new { count });
    }

    /// <summary>
    /// POST /api/v1/notifications/by-user/{userDid}/mark-read - Mark as read
    /// </summary>
    [HttpPost("by-user/{userDid}/mark-read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(string userDid, [FromBody] MarkReadRequest request)
    {
        var result = await _notificationService.MarkAsReadAsync(userDid, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// POST /api/v1/notifications/by-user/{userDid}/mark-all-read - Mark all as read
    /// </summary>
    [HttpPost("by-user/{userDid}/mark-all-read")]
    [Authorize]
    public async Task<IActionResult> MarkAllAsRead(string userDid)
    {
        var result = await _notificationService.MarkAllAsReadAsync(userDid);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// DELETE /api/v1/notifications/{id} - Xóa notification
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        return result.Success ? Ok(result) : NotFound(result);
    }
}
