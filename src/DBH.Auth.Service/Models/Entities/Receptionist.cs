using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Receptionist
{
    [Key]
    public Guid ReceptionistId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid? HospitalId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
