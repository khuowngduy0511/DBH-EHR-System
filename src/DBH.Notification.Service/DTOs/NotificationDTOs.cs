using DBH.Notification.Service.Models.Enums;

namespace DBH.Notification.Service.DTOs;

// Gửi notification
public class SendNotificationRequest
{
    public string RecipientDid { get; set; }
    public Guid? RecipientUserId { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationChannel Channel { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ActionUrl { get; set; }
    public string? Data { get; set; }  // JSON
    public DateTime? ExpiresAt { get; set; }
}

// Gửi broadcast
public class BroadcastNotificationRequest
{
    public List<string> RecipientDids { get; set; } 
    public string Title { get; set; }
    public string Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
}

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string RecipientDid { get; set; }
    public string Title { get; set; }
    public string? Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; }
    public NotificationChannel Channel { get; set; }
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? ReferenceId { get; set; }
    public string? ActionUrl { get; set; }
}

public class MarkReadRequest
{
    public List<Guid> NotificationIds { get; set; }
}

// =============================================================================
// Common Response
// =============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
