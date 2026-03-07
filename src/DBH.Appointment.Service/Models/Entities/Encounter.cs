using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.Appointment.Service.Models.Entities;

/// <summary>
/// Encounter entity — represents a clinical encounter (visit) linked to an appointment

/// </summary>
[Table("encounters")]
public class Encounter
{
    [Key]
    [Column("encounter_id")]
    public Guid EncounterId { get; set; } = Guid.NewGuid();

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
    /// Reference to the appointment that initiated this encounter
    /// </summary>
    [Required]
    [Column("appointment_id")]
    public Guid AppointmentId { get; set; }

    /// <summary>
    /// Organization where the encounter takes place
    /// </summary>
    [Required]
    [Column("org_id")]
    public Guid OrgId { get; set; }

    /// <summary>
    /// Clinical notes from the encounter
    /// </summary>
    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("AppointmentId")]
    public virtual Appointment? Appointment { get; set; }
}
