using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Nurse
{
    [Key]
    public Guid NurseId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? HospitalId { get; set; }

    [MaxLength(255)]
    public string? Specialty { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
