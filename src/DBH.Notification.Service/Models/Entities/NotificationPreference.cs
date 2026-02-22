using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Notification.Service.Models.Enums;

namespace DBH.Notification.Service.Models.Entities;

/// <summary>
/// Cài đặt nhận thông báo của user
/// </summary>
[Table("notification_preferences", Schema = "notification")]
public class NotificationPreference
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User DID
    /// </summary>
    [Required]
    [Column("user_did")]
    [MaxLength(200)]
    public string UserDid { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID (nếu có)
    /// </summary>
    [Column("user_id")]
    public Guid? UserId { get; set; }
    
    // =========================================================================
    // Notification Types Preferences
    // =========================================================================
    
    /// <summary>
    /// Nhận thông báo truy cập EHR
    /// </summary>
    [Column("ehr_access_enabled")]
    public bool EhrAccessEnabled { get; set; } = true;
    
    /// <summary>
    /// Nhận thông báo yêu cầu consent
    /// </summary>
    [Column("consent_request_enabled")]
    public bool ConsentRequestEnabled { get; set; } = true;
    
    /// <summary>
    /// Nhận thông báo cập nhật hồ sơ
    /// </summary>
    [Column("ehr_update_enabled")]
    public bool EhrUpdateEnabled { get; set; } = true;
    
    /// <summary>
    /// Nhận nhắc lịch hẹn
    /// </summary>
    [Column("appointment_reminder_enabled")]
    public bool AppointmentReminderEnabled { get; set; } = true;
    
    /// <summary>
    /// Nhận cảnh báo bảo mật
    /// </summary>
    [Column("security_alert_enabled")]
    public bool SecurityAlertEnabled { get; set; } = true;
    
    /// <summary>
    /// Nhận thông báo hệ thống
    /// </summary>
    [Column("system_notification_enabled")]
    public bool SystemNotificationEnabled { get; set; } = true;
    
    // =========================================================================
    // Channel Preferences
    // =========================================================================
    
    /// <summary>
    /// Cho phép push notification
    /// </summary>
    [Column("push_enabled")]
    public bool PushEnabled { get; set; } = true;
    
    /// <summary>
    /// Cho phép email
    /// </summary>
    [Column("email_enabled")]
    public bool EmailEnabled { get; set; } = true;
    
    /// <summary>
    /// Cho phép SMS
    /// </summary>
    [Column("sms_enabled")]
    public bool SmsEnabled { get; set; } = false;
    
    /// <summary>
    /// Cho phép in-app notifications
    /// </summary>
    [Column("in_app_enabled")]
    public bool InAppEnabled { get; set; } = true;
    
    // =========================================================================
    // Quiet Hours
    // =========================================================================
    
    /// <summary>
    /// Bật chế độ không làm phiền
    /// </summary>
    [Column("quiet_hours_enabled")]
    public bool QuietHoursEnabled { get; set; } = false;
    
    /// <summary>
    /// Giờ bắt đầu không làm phiền (0-23)
    /// </summary>
    [Column("quiet_hours_start")]
    public int QuietHoursStart { get; set; } = 22;
    
    /// <summary>
    /// Giờ kết thúc không làm phiền (0-23)
    /// </summary>
    [Column("quiet_hours_end")]
    public int QuietHoursEnd { get; set; } = 7;
    
    /// <summary>
    /// Timezone của user
    /// </summary>
    [Column("timezone")]
    [MaxLength(50)]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";
    
    // =========================================================================
    // Timestamps
    // =========================================================================
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
