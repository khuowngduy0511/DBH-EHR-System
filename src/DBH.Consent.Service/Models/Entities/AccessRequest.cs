using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Consent.Service.Models.Enums;

namespace DBH.Consent.Service.Models.Entities;

/// <summary>
/// Yêu cầu truy cập EHR từ Doctor/Organization
/// Chờ Patient approve để tạo Consent trên blockchain
/// </summary>
[Table("access_requests")]
public class AccessRequest
{
    [Key]
    [Column("request_id")]
    public Guid RequestId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Patient ID được yêu cầu truy cập
    /// </summary>
    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Patient DID
    /// </summary>
    [Required]
    [Column("patient_did")]
    [MaxLength(255)]
    public string PatientDid { get; set; } = string.Empty;

    /// <summary>
    /// User ID của người yêu cầu (Doctor)
    /// </summary>
    [Required]
    [Column("requester_id")]
    public Guid RequesterId { get; set; }

    /// <summary>
    /// DID của người yêu cầu
    /// </summary>
    [Required]
    [Column("requester_did")]
    [MaxLength(255)]
    public string RequesterDid { get; set; } = string.Empty;

    [Column("requester_type")]
    [MaxLength(30)]
    public GranteeType RequesterType { get; set; }

    /// <summary>
    /// Organization ID (nếu request từ org)
    /// </summary>
    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// EHR ID cụ thể (null = tất cả records)
    /// </summary>
    [Column("ehr_id")]
    public Guid? EhrId { get; set; }

    [Column("permission")]
    [MaxLength(20)]
    public ConsentPermission Permission { get; set; }

    [Column("purpose")]
    [MaxLength(30)]
    public ConsentPurpose Purpose { get; set; }

    /// <summary>
    /// Lý do yêu cầu truy cập
    /// </summary>
    [Required]
    [Column("reason")]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Thời gian yêu cầu có hiệu lực
    /// </summary>
    [Column("requested_duration_days")]
    public int RequestedDurationDays { get; set; } = 30;

    [Column("status")]
    [MaxLength(20)]
    public AccessRequestStatus Status { get; set; } = AccessRequestStatus.PENDING;

    /// <summary>
    /// Consent ID nếu được approve
    /// </summary>
    [Column("consent_id")]
    public Guid? ConsentId { get; set; }

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    [Column("response_reason")]
    [MaxLength(500)]
    public string? ResponseReason { get; set; }

    /// <summary>
    /// Request hết hạn sau X giờ
    /// </summary>
    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
