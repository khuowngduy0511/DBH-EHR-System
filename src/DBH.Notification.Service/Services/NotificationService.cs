using DBH.Notification.Service.Data;
using DBH.Notification.Service.DTOs;
using DBH.Notification.Service.Models.Entities;
using DBH.Notification.Service.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DBH.Notification.Service.Services;

public class NotificationService : INotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IPushNotificationService _pushService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationDbContext context,
        IPushNotificationService pushService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<ApiResponse<NotificationResponse>> SendNotificationAsync(SendNotificationRequest request)
    {
        var notification = new NotificationEntity
        {
            RecipientDid = request.RecipientDid,
            RecipientUserId = request.RecipientUserId,
            Title = request.Title,
            Body = request.Body,
            Type = request.Type,
            Priority = request.Priority,
            Channel = request.Channel,
            ReferenceId = request.ReferenceId,
            ReferenceType = request.ReferenceType,
            ActionUrl = request.ActionUrl,
            Data = request.Data,
            ExpiresAt = request.ExpiresAt,
            Status = NotificationStatus.Pending
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Try to send push notification if channel is Push
        if (request.Channel == NotificationChannel.Push)
        {
            await TrySendPushAsync(notification);
        }
        else
        {
            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Sent notification {NotificationId} to {RecipientDid}", notification.Id, notification.RecipientDid);

        return new ApiResponse<NotificationResponse>
        {
            Success = true,
            Message = "Notification sent successfully",
            Data = MapToResponse(notification)
        };
    }

    public async Task<ApiResponse<int>> BroadcastNotificationAsync(BroadcastNotificationRequest request)
    {
        var notifications = new List<NotificationEntity>();

        foreach (var recipientDid in request.RecipientDids)
        {
            var notification = new NotificationEntity
            {
                RecipientDid = recipientDid,
                Title = request.Title,
                Body = request.Body,
                Type = request.Type,
                Priority = request.Priority,
                Channel = NotificationChannel.InApp,
                Status = NotificationStatus.Sent,
                SentAt = DateTime.UtcNow
            };
            notifications.Add(notification);
        }

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Broadcast {Count} notifications", notifications.Count);

        return new ApiResponse<int>
        {
            Success = true,
            Message = $"Broadcast sent to {notifications.Count} recipients",
            Data = notifications.Count
        };
    }

    public async Task<PagedResponse<NotificationResponse>> GetNotificationsByUserAsync(string userDid, int page, int pageSize)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientDid == userDid)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<NotificationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResponse<NotificationResponse>> GetUnreadNotificationsAsync(string userDid)
    {
        var items = await _context.Notifications
            .Where(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return new PagedResponse<NotificationResponse>
        {
            Data = items.Select(MapToResponse).ToList(),
            Page = 1,
            PageSize = items.Count,
            TotalCount = items.Count
        };
    }

    public async Task<ApiResponse<int>> MarkAsReadAsync(string userDid, MarkReadRequest request)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientDid == userDid && request.NotificationIds.Contains(n.Id))
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ApiResponse<int>
        {
            Success = true,
            Message = $"{notifications.Count} notifications marked as read",
            Data = notifications.Count
        };
    }

    public async Task<ApiResponse<int>> MarkAllAsReadAsync(string userDid)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.Status = NotificationStatus.Read;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new ApiResponse<int>
        {
            Success = true,
            Message = $"{notifications.Count} notifications marked as read",
            Data = notifications.Count
        };
    }

    public async Task<ApiResponse<bool>> DeleteNotificationAsync(Guid notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Notification not found",
                Data = false
            };
        }

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Notification deleted",
            Data = true
        };
    }

    public async Task<int> GetUnreadCountAsync(string userDid)
    {
        return await _context.Notifications
            .CountAsync(n => n.RecipientDid == userDid && n.Status != NotificationStatus.Read);
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    private async Task TrySendPushAsync(NotificationEntity notification)
    {
        try
        {
            var devices = await _context.DeviceTokens
                .Where(d => d.UserDid == notification.RecipientDid && d.IsActive)
                .ToListAsync();

            if (devices.Any())
            {
                var dataDict = new Dictionary<string, string>
                {
                    ["notificationId"] = notification.Id.ToString(),
                    ["type"] = notification.Type.ToString()
                };

                if (!string.IsNullOrEmpty(notification.ActionUrl))
                    dataDict["actionUrl"] = notification.ActionUrl;

                var tokens = devices.Select(d => d.FcmToken).ToList();
                await _pushService.SendMulticastAsync(tokens, notification.Title, notification.Body ?? "", dataDict);
            }

            notification.Status = NotificationStatus.Sent;
            notification.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send push notification {NotificationId}", notification.Id);
            notification.Status = NotificationStatus.Failed;
            notification.ErrorMessage = ex.Message;
            notification.RetryCount++;
        }

        await _context.SaveChangesAsync();
    }

    private static NotificationResponse MapToResponse(NotificationEntity n) => new()
    {
        Id = n.Id,
        RecipientDid = n.RecipientDid,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type,
        Priority = n.Priority,
        Channel = n.Channel,
        Status = n.Status,
        CreatedAt = n.CreatedAt,
        SentAt = n.SentAt,
        ReadAt = n.ReadAt,
        ReferenceId = n.ReferenceId,
        ActionUrl = n.ActionUrl
    };
}
