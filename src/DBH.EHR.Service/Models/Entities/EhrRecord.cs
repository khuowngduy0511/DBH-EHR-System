using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Time;

namespace DBH.EHR.Service.Models.Entities;


/// <summary>
/// EHR Record entity — matches ERD: ehr_records table
/// Fields: ehr_id, patient_id, encounter_id, org_id, data (jsonb), created_at
/// </summary>
[Table("ehr_records")]
public class EhrRecord
{
    [Key]
    [Column("ehr_id")]
    public Guid EhrId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Encounter that created this EHR
    /// </summary>
    [Column("encounter_id")]
    public Guid? EncounterId { get; set; }

    /// <summary>
    /// Organization where EHR was created
    /// </summary>
    [Column("org_id")]
    public Guid? OrgId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTime.DatabaseNow;

    // Navigation
    public virtual ICollection<EhrVersion> Versions { get; set; } = new List<EhrVersion>();
    public virtual ICollection<EhrFile> Files { get; set; } = new List<EhrFile>();
}
