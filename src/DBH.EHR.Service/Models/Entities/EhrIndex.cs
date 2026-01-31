using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.EHR.Service.Models.Entities;

[Table("ehr_index")]
public class EhrIndex
{
    [Key]
    [Column("record_id")]
    public Guid RecordId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    [MaxLength(100)]
    public string PatientId { get; set; } = string.Empty;

    [Required]
    [Column("owner_org")]
    [MaxLength(100)]
    public string OwnerOrg { get; set; } = string.Empty;

    /// <summary>
    /// Reference to MongoDB document _id (ObjectId as string)
    /// </summary>
    [Required]
    [Column("offchain_doc_id")]
    [MaxLength(100)]
    public string OffchainDocId { get; set; } = string.Empty;

    [Column("version")]
    public int Version { get; set; } = 1;

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public EhrStatus Status { get; set; } = EhrStatus.ACTIVE;

    [Column("record_type")]
    [MaxLength(100)]
    public string? RecordType { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public enum EhrStatus
{
    ACTIVE,
    ARCHIVED,
    DELETED
}
