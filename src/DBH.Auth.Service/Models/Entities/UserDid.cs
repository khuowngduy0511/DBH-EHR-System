using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// DID (Decentralized Identifier) của người dùng cho blockchain
/// Mỗi user có thể có nhiều DID (rotation, recovery)
/// </summary>
[Table("user_dids")]
public class UserDid
{
    [Key]
    [Column("did_id")]
    public Guid DidId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// DID Format: did:fabric:{type}:{identifier}
    /// VD: did:fabric:patient:abc123
    /// </summary>
    [Required]
    [Column("did")]
    [MaxLength(255)]
    public string Did { get; set; } = string.Empty;

    /// <summary>
    /// Public key để verify signature
    /// </summary>
    [Column("public_key", TypeName = "text")]
    public string? PublicKey { get; set; }

    /// <summary>
    /// Private key (encrypted) - chỉ lưu nếu user chọn custodial wallet
    /// </summary>
    [Column("encrypted_private_key", TypeName = "text")]
    public string? EncryptedPrivateKey { get; set; }

    [Column("key_algorithm")]
    [MaxLength(50)]
    public KeyAlgorithm KeyAlgorithm { get; set; } = KeyAlgorithm.ECDSA_P256;

    /// <summary>
    /// Hyperledger Fabric enrollment ID
    /// </summary>
    [Column("fabric_enrollment_id")]
    [MaxLength(255)]
    public string? FabricEnrollmentId { get; set; }

    /// <summary>
    /// MSP Organization ID trong Fabric network
    /// </summary>
    [Column("fabric_msp_id")]
    [MaxLength(100)]
    public string? FabricMspId { get; set; }

    /// <summary>
    /// X.509 Certificate từ Fabric CA
    /// </summary>
    [Column("fabric_certificate", TypeName = "text")]
    public string? FabricCertificate { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public DidStatus Status { get; set; } = DidStatus.PENDING_ACTIVATION;

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("activated_at")]
    public DateTime? ActivatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("revoke_reason")]
    [MaxLength(500)]
    public string? RevokeReason { get; set; }

    // Navigation
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}
