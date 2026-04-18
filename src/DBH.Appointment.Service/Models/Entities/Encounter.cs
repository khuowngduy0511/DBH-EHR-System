using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Shared.Infrastructure.Time;

namespace DBH.Appointment.Service.Models.Entities;

/// <summary>
/// Encounter entity — matches ERD: encounters table
/// Fields: encounter_id, patient_id, doctor_id, appointment_id, org_id, notes, created_at
/// </summary>
[Table("encounters")]
public class Encounter
{
    [Key]
    [Column("encounter_id")]
    public Guid EncounterId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [Column("appointment_id")]
    public Guid AppointmentId { get; set; }

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTime.DatabaseNow;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    // Navigation
    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }
}
