using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Appointment.Service.Models.Enums;
using DBH.Shared.Infrastructure.Time;

namespace DBH.Appointment.Service.Models.Entities;

/// <summary>
/// Appointment entity — matches ERD: appointments table
/// Fields: appointment_id, patient_id, doctor_id, org_id, scheduled_at, status, created_at
/// </summary>
[Table("appointments")]
public class Appointment
{
    [Key]
    [Column("appointment_id")]
    public Guid AppointmentId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    [Required]
    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.PENDING;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = VietnamTime.DatabaseNow;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    public Guid? UpdatedBy { get; set; }

    // Navigation
    public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
}
