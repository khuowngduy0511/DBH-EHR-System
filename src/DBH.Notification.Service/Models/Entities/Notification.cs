using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Notification.Service.Models.Enums;

namespace DBH.Notification.Service.Models.Entities;

/// <summary>
/// Entity thông báo - lưu trữ tất cả thông báo gửi cho users
/// </summary>
[Table("notifications", Schema = "notification")]
public class Notification
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // =========================================================================
    // Người nhận
    // =========================================================================
    
    /// <summary>
    /// User DID nhận thông báo
    /// </summary>
    [Required]
    [Column("recipient_did")]
    [MaxLength(200)]
    public string RecipientDid { get; set; } = string.Empty;
    
    /// <summary>
    /// User ID nhận thông báo (nếu có)
    /// </summary>
    [Column("recipient_user_id")]
    public Guid? RecipientUserId { get; set; }
    
    // =========================================================================
    // Nội dung thông báo
    // =========================================================================
    
    /// <summary>
    /// Tiêu đề thông báo
    /// </summary>
    [Required]
    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Nội dung chi tiết
    /// </summary>
    [Column("body")]
    [MaxLength(2000)]
    public string? Body { get; set; }
    
    /// <summary>
    /// Loại thông báo
    /// </summary>
    [Required]
    [Column("type")]
    [MaxLength(50)]
    public NotificationType Type { get; set; }
    
    /// <summary>
    /// Độ ưu tiên
    /// </summary>
    [Column("priority")]
    [MaxLength(20)]
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    
    // =========================================================================
    // Kênh gửi
    // =========================================================================
    
    /// <summary>
    /// Kênh gửi thông báo
    /// </summary>
    [Required]
    [Column("channel")]
    [MaxLength(20)]
    public NotificationChannel Channel { get; set; }
    
    /// <summary>
    /// Trạng thái gửi
    /// </summary>
    [Column("status")]
    [MaxLength(20)]
    public NotificationStatus Status { get; set; } = NotificationStatus.Pending;
    
    // =========================================================================
    // Metadata & Reference
    // =========================================================================
    
    /// <summary>
    /// ID của đối tượng liên quan (EHR, Consent, etc.)
    /// </summary>
    [Column("reference_id")]
    [MaxLength(100)]
    public string? ReferenceId { get; set; }
    
    /// <summary>
    /// Loại đối tượng liên quan
    /// </summary>
    [Column("reference_type")]
    [MaxLength(50)]
    public string? ReferenceType { get; set; }
    
    /// <summary>
    /// Deep link khi click notification
    /// </summary>
    [Column("action_url")]
    [MaxLength(500)]
    public string? ActionUrl { get; set; }
    
    /// <summary>
    /// Data bổ sung (JSON)
    /// </summary>
    [Column("data", TypeName = "jsonb")]
    public string? Data { get; set; }
    
    // =========================================================================
    // Timestamps
    // =========================================================================
    
    /// <summary>
    /// Thời điểm tạo
    /// </summary>
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Thời điểm gửi
    /// </summary>
    [Column("sent_at")]
    public DateTime? SentAt { get; set; }
    
    /// <summary>
    /// Thời điểm đọc
    /// </summary>
    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
    
    /// <summary>
    /// Thời điểm hết hạn (tự động xóa)
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }
    
    // =========================================================================
    // Delivery info
    // =========================================================================
    
    /// <summary>
    /// Số lần thử gửi
    /// </summary>
    [Column("retry_count")]
    public int RetryCount { get; set; } = 0;
    
    /// <summary>
    /// Lỗi khi gửi thất bại
    /// </summary>
    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Firebase message ID (nếu push notification)
    /// </summary>
    [Column("external_message_id")]
    [MaxLength(200)]
    public string? ExternalMessageId { get; set; }
}
