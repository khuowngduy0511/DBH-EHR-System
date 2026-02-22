using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.Notification.Service.Models.Entities;

/// <summary>
/// Entity lưu trữ device tokens cho push notifications
/// </summary>
[Table("device_tokens", Schema = "notification")]
public class DeviceToken
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// User DID sở hữu device
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
    
    /// <summary>
    /// Firebase Cloud Messaging token
    /// </summary>
    [Required]
    [Column("fcm_token")]
    [MaxLength(500)]
    public string FcmToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Tên thiết bị
    /// </summary>
    [Column("device_name")]
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// Loại thiết bị (ios/android/web)
    /// </summary>
    [Required]
    [Column("device_type")]
    [MaxLength(20)]
    public string DeviceType { get; set; } = string.Empty;
    
    /// <summary>
    /// Phiên bản OS
    /// </summary>
    [Column("os_version")]
    [MaxLength(50)]
    public string? OsVersion { get; set; }
    
    /// <summary>
    /// Phiên bản app
    /// </summary>
    [Column("app_version")]
    [MaxLength(20)]
    public string? AppVersion { get; set; }
    
    /// <summary>
    /// Token còn active không
    /// </summary>
    [Column("is_active")]
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Thời điểm đăng ký
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Thời điểm cập nhật cuối
    /// </summary>
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Lần cuối sử dụng thành công
    /// </summary>
    [Column("last_used_at")]
    public DateTime? LastUsedAt { get; set; }
}
