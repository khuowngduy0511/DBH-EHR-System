using System.ComponentModel.DataAnnotations;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

public class Doctor
{
    [Key]
    public Guid DoctorId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;


    [MaxLength(255)]
    public string? Specialty { get; set; }

    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    [MaxLength(255)]
    public string? LicenseImage { get; set; }
    
    /// <summary>
    /// Trạng thái xác minh
    /// </summary>
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;

}
