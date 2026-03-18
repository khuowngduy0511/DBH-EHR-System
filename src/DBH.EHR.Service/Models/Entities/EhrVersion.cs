using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
    /// Version data as JSONB (snapshot of EHR data at this version)
    /// </summary>
    [Column("data", TypeName = "jsonb")]
    public string? Data { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("EhrId")]
    public virtual EhrRecord? EhrRecord { get; set; }
}
