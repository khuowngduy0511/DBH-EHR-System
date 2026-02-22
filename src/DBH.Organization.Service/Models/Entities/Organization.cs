using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Organization.Service.Models.Enums;

namespace DBH.Organization.Service.Models.Entities;

/// <summary>
/// Tổ chức y tế (Hospital, Clinic, Lab, Pharmacy)
/// Đây là entity riêng của Organization Service
/// </summary>
[Table("organizations")]
public class Organization
{
    [Key]
    [Column("org_id")]
    public Guid OrgId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// DID của tổ chức trong blockchain
    /// Format: did:fabric:org:{identifier}
    /// </summary>
    [Column("org_did")]
    [MaxLength(255)]
    public string? OrgDid { get; set; }

    [Required]
    [Column("org_name")]
    [MaxLength(255)]
    public string OrgName { get; set; } = string.Empty;

    [Column("org_code")]
    [MaxLength(50)]
    public string? OrgCode { get; set; }

    [Column("org_type")]
    [MaxLength(30)]
    public OrganizationType OrgType { get; set; } = OrganizationType.HOSPITAL;

    /// <summary>
    /// Số giấy phép hoạt động
    /// </summary>
    [Column("license_number")]
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [Column("tax_id")]
    [MaxLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// Địa chỉ (FHIR Address format)
    /// </summary>
    [Column("address", TypeName = "jsonb")]
    public string? Address { get; set; }

    /// <summary>
    /// Thông tin liên hệ
    /// </summary>
    [Column("contact_info", TypeName = "jsonb")]
    public string? ContactInfo { get; set; }

    [Column("website")]
    [MaxLength(255)]
    public string? Website { get; set; }

    [Column("logo_url")]
    [MaxLength(500)]
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Hyperledger Fabric MSP ID
    /// </summary>
    [Column("fabric_msp_id")]
    [MaxLength(100)]
    public string? FabricMspId { get; set; }

    /// <summary>
    /// Danh sách peer nodes của org trong Fabric
    /// </summary>
    [Column("fabric_channel_peers", TypeName = "jsonb")]
    public string? FabricChannelPeers { get; set; }

    /// <summary>
    /// Fabric CA URL
    /// </summary>
    [Column("fabric_ca_url")]
    [MaxLength(500)]
    public string? FabricCaUrl { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public OrganizationStatus Status { get; set; } = OrganizationStatus.PENDING_VERIFICATION;

    [Column("verified_at")]
    public DateTime? VerifiedAt { get; set; }

    /// <summary>
    /// User ID của người verify (lưu ID vì User ở service khác)
    /// </summary>
    [Column("verified_by")]
    public Guid? VerifiedBy { get; set; }

    /// <summary>
    /// Timezone của tổ chức
    /// </summary>
    [Column("timezone")]
    [MaxLength(50)]
    public string Timezone { get; set; } = "Asia/Ho_Chi_Minh";

    /// <summary>
    /// Cài đặt bổ sung
    /// </summary>
    [Column("settings", TypeName = "jsonb")]
    public string? Settings { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation (chỉ trong Organization Service)
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
}
