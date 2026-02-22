using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Patient
{
    [Key]
    public Guid PatientId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateOnly? Dob { get; set; }

    [MaxLength(20)]
    public string? Gender { get; set; }

    [MaxLength(10)]
    public string? BloodType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
