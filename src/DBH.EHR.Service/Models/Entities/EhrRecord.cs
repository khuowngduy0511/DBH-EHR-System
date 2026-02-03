using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.EHR.Service.Models.Entities;


/// Bản ghi EHR chính 
[Table("ehr_records")]
public class EhrRecord
{
    [Key]
    [Column("ehr_id")]
    public Guid EhrId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    ///  lượt khám 
    [Column("encounter_id")]
    public Guid? EncounterId { get; set; }

    /// Bệnh viện nơi tạo EHR
    [Column("hospital_id")]
    public Guid? HospitalId { get; set; }

    [Required]
    [Column("created_by_doctor")]
    public Guid CreatedByDoctorId { get; set; }
    
    /// Version hiện tại 
    [Column("current_version")]
    public int CurrentVersion { get; set; } = 1;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<EhrVersion> Versions { get; set; } = new List<EhrVersion>();
    public virtual ICollection<EhrFile> Files { get; set; } = new List<EhrFile>();
}
