using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Appointment.Service.Models.Enums;

namespace DBH.Appointment.Service.Models.Entities;

/// <summary>
/// Appointment entity — represents a scheduled appointment between a patient and a doctor
/// Matches ERD: appointments(appointment_id, patient_id, doctor_id, org_id, scheduled_at, status, created_at)
/// </summary>
[Table("appointments")]
public class Appointment
{
    [Key]
    [Column("appointment_id")]
    public Guid AppointmentId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Patient user ID (from Auth Service)
    /// </summary>
    [Required]
    [Column("patient_id")]
    public Guid PatientId { get; set; }

    /// <summary>
    /// Doctor/provider user ID (from Auth Service)
    /// </summary>
    [Required]
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Organization where the appointment takes place
    /// </summary>
    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    /// <summary>
    /// Scheduled date and time (UTC)
    /// </summary>
    [Required]
    [Column("scheduled_at")]
    public DateTime ScheduledAt { get; set; }

    /// <summary>
    /// Current appointment status
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.PENDING;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Encounter> Encounters { get; set; } = new List<Encounter>();
}
