using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Time;

namespace DBH.EHR.Service.Models.Entities;


/// <summary>
/// EHR Version entity — matches ERD: ehr_versions table
/// Fields: version_id, ehr_id, version_number, data (jsonb), created_at
/// </summary>
[Table("ehr_versions")]
public class EhrVersion
{
    [Key]
    [Column("version_id")]
    public Guid VersionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    [Column("version_number")]
    public int VersionNumber { get; set; } = 1;

    /// <summary>
    /// CID pointing to encrypted data on IPFS
    /// </summary>
    [Column("ipfs_cid")]
    [MaxLength(255)]
    public string? IpfsCid { get; set; }

    /// <summary>
    /// Fallback: encrypted data stored directly when IPFS is unavailable
    /// </summary>
    [Column("encrypted_fallback_data")]
    public string? EncryptedFallbackData { get; set; }

    /// <summary>
    /// SHA-256 hash of the original content for integrity verification
    /// </summary>
    [Column("data_hash")]
    [MaxLength(255)]
    public string? DataHash { get; set; }

    /// <summary>
    /// AES key encrypted with patient's RSA public key — stored here for immediate availability
    /// without waiting for async blockchain commit.
    /// </summary>
    [Column("encrypted_aes_key_for_patient")]
    public string? EncryptedAesKeyForPatient { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTime.DatabaseNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
