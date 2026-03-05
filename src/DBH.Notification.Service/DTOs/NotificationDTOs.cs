using DBH.Notification.Service.Models.Enums;

namespace DBH.Notification.Service.DTOs;

// ============================================================================
// Notification Request DTOs
// ============================================================================

public class SendNotificationRequest
{
    public string RecipientDid { get; set; } = string.Empty;
    public Guid? RecipientUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public NotificationChannel Channel { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ActionUrl { get; set; }
    public string? Data { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class BroadcastNotificationRequest
{
    public List<string> RecipientDids { get; set; } = new();
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

public class MarkReadRequest
{
    public List<Guid> NotificationIds { get; set; } = new();
}

// ============================================================================
// Notification Response DTOs
// ============================================================================

public class NotificationResponse
{
    public Guid Id { get; set; }
    public string RecipientDid { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
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

// ============================================================================
// DeviceToken DTOs
// ============================================================================

public class RegisterDeviceRequest
{
    public string UserDid { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
}

public class DeviceTokenResponse
{
    public Guid Id { get; set; }
    public string FcmToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ============================================================================
// Preferences DTOs
// ============================================================================

public class UpdatePreferencesRequest
{
    public bool? EhrAccessEnabled { get; set; }
    public bool? ConsentRequestEnabled { get; set; }
    public bool? EhrUpdateEnabled { get; set; }
    public bool? AppointmentReminderEnabled { get; set; }
    public bool? SecurityAlertEnabled { get; set; }
    public bool? SystemNotificationEnabled { get; set; }
    public bool? PushEnabled { get; set; }
    public bool? EmailEnabled { get; set; }
    public bool? SmsEnabled { get; set; }
    public bool? QuietHoursEnabled { get; set; }
    public int? QuietHoursStart { get; set; }
    public int? QuietHoursEnd { get; set; }
}

public class PreferencesResponse
{
    public Guid Id { get; set; }
    public string UserDid { get; set; } = string.Empty;
    public bool EhrAccessEnabled { get; set; }
    public bool ConsentRequestEnabled { get; set; }
    public bool EhrUpdateEnabled { get; set; }
    public bool AppointmentReminderEnabled { get; set; }
    public bool SecurityAlertEnabled { get; set; }
    public bool PushEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool QuietHoursEnabled { get; set; }
    public int QuietHoursStart { get; set; }
    public int QuietHoursEnd { get; set; }
}

// ============================================================================
// Common Wrappers
// ============================================================================

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true, Data = data, Message = message
    };

    public static ApiResponse<T> Fail(string message) => new()
    {
        Success = false, Message = message
    };
}

public class PagedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}
