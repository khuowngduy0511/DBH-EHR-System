using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Doctor
{
    [Key]
    public Guid DoctorId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? HospitalId { get; set; }

    [MaxLength(255)]
    public string? Specialty { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(255)]
    public string? LicenseImage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
