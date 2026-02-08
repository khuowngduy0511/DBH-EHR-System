using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Pharmacist
{
    [Key]
    public Guid PharmacistId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? HospitalId { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
