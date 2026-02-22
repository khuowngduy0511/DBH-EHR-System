using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Audit.Service.Models.Enums;

namespace DBH.Audit.Service.Models.Entities;

/// <summary>
/// Audit Log - Source of truth: Blockchain (audit-channel)
/// Đây chỉ là cache để query nhanh
/// </summary>
[Table("audit_logs")]
public class AuditLog
{
    [Key]
    [Column("audit_id")]
    public Guid AuditId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID audit log trên blockchain
    /// </summary>
    [Required]
    [Column("blockchain_audit_id")]
    [MaxLength(100)]
    public string BlockchainAuditId { get; set; } = string.Empty;

    /// <summary>
    /// DID của người thực hiện hành động
    /// </summary>
    [Required]
    [Column("actor_did")]
    [MaxLength(255)]
    public string ActorDid { get; set; } = string.Empty;

    /// <summary>
    /// User ID (từ Auth Service)
    /// </summary>
    [Column("actor_user_id")]
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Loại actor
    /// </summary>
    [Column("actor_type")]
    [MaxLength(30)]
    public ActorType ActorType { get; set; }

    [Column("action")]
    [MaxLength(30)]
    public AuditAction Action { get; set; }

    /// <summary>
    /// Loại đối tượng: EHR, CONSENT, FILE, USER
    /// </summary>
    [Column("target_type")]
    [MaxLength(30)]
    public TargetType TargetType { get; set; }

    /// <summary>
    /// ID của đối tượng bị tác động
    /// </summary>
    [Column("target_id")]
    public Guid? TargetId { get; set; }

    /// <summary>
    /// DID của patient liên quan
    /// </summary>
    [Column("patient_did")]
    [MaxLength(255)]
    public string? PatientDid { get; set; }

    /// <summary>
    /// Patient ID (từ Auth Service)
    /// </summary>
    [Column("patient_id")]
    public Guid? PatientId { get; set; }

    /// <summary>
    /// Consent ID đã authorize hành động này
    /// </summary>
    [Column("consent_id")]
    public Guid? ConsentId { get; set; }

    /// <summary>
    /// Organization ID
    /// </summary>
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    [Column("result")]
    [MaxLength(30)]
    public AuditResult Result { get; set; }

    /// <summary>
    /// Chi tiết bổ sung
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Error message nếu có
    /// </summary>
    [Column("error_message")]
    [MaxLength(1000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// IP address của client
    /// </summary>
    [Column("ip_address")]
    [MaxLength(100)]
    public string? IpAddress { get; set; }

    [Column("user_agent", TypeName = "text")]
    public string? UserAgent { get; set; }

    [Column("session_id")]
    [MaxLength(100)]
    public string? SessionId { get; set; }

    /// <summary>
    /// Timestamp trên blockchain
    /// </summary>
    [Column("blockchain_timestamp")]
    public DateTime BlockchainTimestamp { get; set; }

    [Column("blockchain_tx_hash")]
    [MaxLength(66)]
    public string? BlockchainTxHash { get; set; }

    [Column("blockchain_block_num")]
    public long? BlockchainBlockNum { get; set; }

    /// <summary>
    /// Thời điểm sync từ blockchain
    /// </summary>
    [Column("synced_at")]
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
