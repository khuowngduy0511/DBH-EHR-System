using System.ComponentModel.DataAnnotations;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

public class User
{
    [Key]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [MaxLength(255)]
    public string? FullName { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }
    public string? Gender { get; set; } 
    [MaxLength(255)]
    public string? Password { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public string? OrganizationId { get; set; } 
    public UserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? PublicKey { get; set; }
    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserCredential> Credentials { get; set; } = new List<UserCredential>();
    public UserSecurity? Security { get; set; }
    // Profiles
    public Doctor? DoctorProfile { get; set; }
    public Patient? PatientProfile { get; set; }
    public Staff? StaffProfile { get; set; }  // Gộp Nurse, Pharmacist, LabTech, Receptionist
}
