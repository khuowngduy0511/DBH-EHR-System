using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Services;
using Microsoft.AspNetCore.Mvc;

namespace DBH.Notification.Service.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // POST /api/notifications - Gửi notification (internal)
    [HttpPost]
    public async Task<ActionResult<ApiResponse<NotificationResponse>>> SendNotification([FromBody] SendNotificationRequest request)
    {
        var result = await _notificationService.SendNotificationAsync(request);
        return Ok(result);
    }

    // POST /api/notifications/broadcast - Broadcast (admin)
    [HttpPost("broadcast")]
    public async Task<ActionResult<ApiResponse<int>>> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
    {
        var result = await _notificationService.BroadcastNotificationAsync(request);
        return Ok(result);
    }

    // GET /api/notifications/me - List notifications của user hiện tại
    [HttpGet("me")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetMyNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did"; 
        var result = await _notificationService.GetNotificationsByUserAsync(userDid, page, pageSize);
        return Ok(result);
    }

    // GET /api/notifications/me/unread - Unread notifications
    [HttpGet("me/unread")]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> GetMyUnreadNotifications()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.GetUnreadNotificationsAsync(userDid);
        return Ok(result);
    }

    // GET /api/notifications/me/unread-count - Đếm unread
    [HttpGet("me/unread-count")]
    public async Task<ActionResult<int>> GetMyUnreadCount()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var count = await _notificationService.GetUnreadCountAsync(userDid);
        return Ok(count);
    }

    // POST /api/notifications/me/mark-read - Mark as read
    [HttpPost("me/mark-read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkRead([FromBody] MarkReadRequest request)
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.MarkAsReadAsync(userDid, request);
        return Ok(result);
    }

    // POST /api/notifications/me/mark-all-read - Mark all as read
    [HttpPost("me/mark-all-read")]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllRead()
    {
        // TODO: Get userDid from token
        string userDid = "test-user-did";
        var result = await _notificationService.MarkAllAsReadAsync(userDid);
        return Ok(result);
    }

    // DELETE /api/notifications/{id} - Xóa notification
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteNotification(Guid id)
    {
        var result = await _notificationService.DeleteNotificationAsync(id);
        return Ok(result);
    }
}
