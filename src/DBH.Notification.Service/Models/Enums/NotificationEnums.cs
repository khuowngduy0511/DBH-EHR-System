namespace DBH.Notification.Service.Models.Enums;

/// <summary>
/// Loại thông báo
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Thông báo truy cập EHR
    /// </summary>
    EhrAccess,
    
    /// <summary>
    /// Yêu cầu consent
    /// </summary>
    ConsentRequest,
    
    /// <summary>
    /// Consent đã được cấp
    /// </summary>
    ConsentGranted,
    
    /// <summary>
    /// Consent bị thu hồi
    /// </summary>
    ConsentRevoked,
    
    /// <summary>
    /// Cập nhật hồ sơ bệnh án
    /// </summary>
    EhrUpdate,
    
    /// <summary>
    /// Nhắc lịch khám
    /// </summary>
    AppointmentReminder,
    
    /// <summary>
    /// Thông báo hệ thống
    /// </summary>
    System,
    
    /// <summary>
    /// Cảnh báo bảo mật
    /// </summary>
    SecurityAlert
}

/// <summary>
/// Trạng thái thông báo
/// </summary>
public enum NotificationStatus
{
    /// <summary>
    /// Đang chờ gửi
    /// </summary>
    Pending,
    
    /// <summary>
    /// Đã gửi thành công
    /// </summary>
    Sent,
    
    /// <summary>
    /// Đã đọc
    /// </summary>
    Read,
    
    /// <summary>
    /// Gửi thất bại
    /// </summary>
    Failed,
    
    /// <summary>
    /// Đã hủy
    /// </summary>
    Cancelled
}

/// <summary>
/// Kênh gửi thông báo
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Push notification (Firebase)
    /// </summary>
    Push,
    
    /// <summary>
    /// Email
    /// </summary>
    Email,
    
    /// <summary>
    /// SMS
    /// </summary>
    Sms,
    
    /// <summary>
    /// In-app notification
    /// </summary>
    InApp
}

/// <summary>
/// Độ ưu tiên thông báo
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Thấp - có thể gửi batch
    /// </summary>
    Low,
    
    /// <summary>
    /// Bình thường
    /// </summary>
    Normal,
    
    /// <summary>
    /// Cao - gửi ngay
    /// </summary>
    High,
    
    /// <summary>
    /// Khẩn cấp - bắt buộc đọc
    /// </summary>
    Urgent
}
