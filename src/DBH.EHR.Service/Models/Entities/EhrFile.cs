using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.EHR.Service.Models.Entities;


/// <summary>
/// EHR File entity — matches ERD: ehr_files table
/// Fields: file_id, ehr_id, file_url, file_hash, created_at
/// </summary>
[Table("ehr_files")]
public class EhrFile
{
    [Key]
    [Column("file_id")]
    public Guid FileId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    /// <summary>
    /// S3/IPFS URL for the file
    /// </summary>
    [Column("file_url")]
    [MaxLength(1000)]
    public string? FileUrl { get; set; }

    /// <summary>
    /// SHA-256 hash for integrity verification
    /// </summary>
    [Column("file_hash")]
    [MaxLength(255)]
    public string? FileHash { get; set; }

    /// <summary>
    /// CID pointing to encrypted file on IPFS
    /// </summary>
    [Column("ipfs_cid")]
    [MaxLength(255)]
    public string? IpfsCid { get; set; }

    /// <summary>
    /// Fallback: encrypted file data stored directly when IPFS is unavailable
    /// </summary>
    [Column("encrypted_fallback_data")]
    public string? EncryptedFallbackData { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
