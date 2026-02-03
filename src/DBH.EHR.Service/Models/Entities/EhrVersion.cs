using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.EHR.Service.Models.Enums;

namespace DBH.EHR.Service.Models.Entities;


/// Version EHR với hash + blockchain 
[Table("ehr_versions")]
public class EhrVersion
{
    [Key]
    [Column("version_id")]
    public Guid VersionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("ehr_id")]
    public Guid EhrId { get; set; }

    [Column("version")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// SHA-256 hash 
    /// </summary>
    [Column("file_hash")]
    [MaxLength(255)]
    public string? FileHash { get; set; }

    /// <summary>
    /// User thực hiện thay đổi
    /// </summary>
    [Column("changed_by")]
    public Guid? ChangedBy { get; set; }

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    [Column("change_reason")]
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Link đến version trước
    /// </summary>
    [Column("previous_version_id")]
    public Guid? PreviousVersionId { get; set; }

    [Column("blockchain_tx_hash")]
    [MaxLength(255)]
    public string? BlockchainTxHash { get; set; }

    [Column("tx_status")]
    public TxStatus? TxStatus { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }

    [ForeignKey("PreviousVersionId")]
    public virtual EhrVersion? PreviousVersion { get; set; }

    public virtual ICollection<EhrFile> Files { get; set; } = new List<EhrFile>();
}
