using System.Security.Claims;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // POST /api/notifications - Gửi notification (internal/service)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        var result = await _notificationService.SendNotificationAsync(request);
        return Ok(result);
    }

    // POST /api/notifications/broadcast - Broadcast (admin)
    [HttpPost("broadcast")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        var result = await _notificationService.BroadcastNotificationAsync(request);
        return Ok(result);
    }

    // GET /api/notifications/me - List notifications của user hiện tại
    [HttpGet("me")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetNotificationsByUserAsync(userId, page, pageSize);
        return Ok(result);
    }

    // GET /api/notifications/me/unread - Unread notifications
    [HttpGet("me/unread")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetMyUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.GetUnreadNotificationsAsync(userId);
        return Ok(result);
    }

    // GET /api/notifications/me/unread-count - Đếm unread
    [HttpGet("me/unread-count")]
    public async Task<ActionResult<int>> GetMyUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(count);
    }

    // POST /api/notifications/me/mark-read - Mark as read
    [HttpPost("me/mark-read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkRead([FromBody] MarkReadRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAsReadAsync(userId, request);
        return Ok(result);
    }

    // POST /api/notifications/me/mark-all-read - Mark all as read
    [HttpPost("me/mark-all-read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        var result = await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(result);
    }

    // DELETE /api/notifications/{id} - Xóa notification
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(Guid id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        return Ok(result);
    }

    /// <summary>
    /// Extract userId from JWT ClaimTypes.NameIdentifier
    /// </summary>
    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }
}
