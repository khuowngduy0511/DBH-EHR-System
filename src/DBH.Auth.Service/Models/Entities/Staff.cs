using System.ComponentModel.DataAnnotations;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// Staff - Nhân viên hỗ trợ (gộp Nurse, Pharmacist, LabTech, Receptionist)
/// </summary>
public class Staff
{
    [Key]
    public Guid StaffId { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Vai trò cụ thể: NURSE, PHARMACIST, LAB_TECH, RECEPTIONIST
    /// </summary>
    public StaffRole Role { get; set; }

    /// <summary>
    /// Hospital/Organization ID (từ Organization Service)
    /// </summary>
    public Guid? HospitalId { get; set; }

    /// <summary>
    /// Số giấy phép hành nghề (cho Pharmacist, LabTech)
    /// </summary>
    [MaxLength(100)]
    public string? LicenseNumber { get; set; }

    /// <summary>
    /// Chuyên khoa (cho Nurse)
    /// </summary>
    [MaxLength(255)]
    public string? Specialty { get; set; }

    /// <summary>
    /// Trạng thái xác minh
    /// </summary>
    public VerificationStatus VerifiedStatus { get; set; } = VerificationStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
