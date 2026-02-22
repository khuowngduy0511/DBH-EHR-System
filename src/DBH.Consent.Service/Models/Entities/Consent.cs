using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Consent.Service.Models.Enums;

namespace DBH.Consent.Service.Models.Entities;

/// <summary>
/// Consent record - Source of truth: Blockchain
/// Đây chỉ là cache để query nhanh
/// </summary>
[Table("consents")]
public class Consent
{
    [Key]
    [Column("consent_id")]
    public Guid ConsentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID consent trên blockchain
    /// </summary>
    [Required]
    [Column("blockchain_consent_id")]
    [MaxLength(100)]
    public string BlockchainConsentId { get; set; } = string.Empty;

    /// <summary>
    /// Patient ID (từ Auth Service)
    /// </summary>
    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    /// <summary>
    /// DID của bệnh nhân (người cấp quyền)
    /// </summary>
    [Required]
    [Column("patient_did")]
    [MaxLength(255)]
    public string PatientDid { get; set; } = string.Empty;

    /// <summary>
    /// User/Org ID được cấp quyền (từ Auth/Organization Service)
    /// </summary>
    [Required]
    [Column("grantee_id")]
    public Guid GranteeId { get; set; }

    /// <summary>
    /// DID của người được cấp quyền (Doctor/Org)
    /// </summary>
    [Required]
    [Column("grantee_did")]
    [MaxLength(255)]
    public string GranteeDid { get; set; } = string.Empty;

    [Column("grantee_type")]
    [MaxLength(30)]
    public GranteeType GranteeType { get; set; }

    /// <summary>
    /// EHR ID cụ thể (null = tất cả records của patient)
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
    /// Điều kiện bổ sung (report types được phép, etc.)
    /// </summary>
    [Column("conditions", TypeName = "jsonb")]
    public string? Conditions { get; set; }

    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("status")]
    [MaxLength(20)]
    public ConsentStatus Status { get; set; } = ConsentStatus.ACTIVE;

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoke_reason")]
    [MaxLength(500)]
    public string? RevokeReason { get; set; }

    /// <summary>
    /// Transaction hash khi grant consent
    /// </summary>
    [Column("grant_tx_hash")]
    [MaxLength(66)]
    public string? GrantTxHash { get; set; }

    /// <summary>
    /// Transaction hash khi revoke
    /// </summary>
    [Column("revoke_tx_hash")]
    [MaxLength(66)]
    public string? RevokeTxHash { get; set; }

    /// <summary>
    /// Block number trên Fabric
    /// </summary>
    [Column("blockchain_block_num")]
    public long? BlockchainBlockNum { get; set; }

    /// <summary>
    /// Thời điểm sync cuối từ blockchain
    /// </summary>
    [Column("last_synced_at")]
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
