using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// Người dùng hệ thống (Patient, Doctor, Admin, etc.)
/// Đây là entity riêng của Auth Service - không share với services khác
/// </summary>
[Table("users")]
public class User
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Bcrypt hash cho đăng nhập
    /// </summary>
    [Column("password_hash")]
    [MaxLength(255)]
    public string? PasswordHash { get; set; }

    [Required]
    [Column("user_type")]
    [MaxLength(30)]
    public UserType UserType { get; set; }

    [Column("first_name")]
    [MaxLength(100)]
    public string? FirstName { get; set; }

    [Column("last_name")]
    [MaxLength(100)]
    public string? LastName { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("phone")]
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// CMND/CCCD (encrypted)
    /// </summary>
    [Column("national_id")]
    [MaxLength(255)]
    public string? NationalId { get; set; }

    [Column("gender")]
    [MaxLength(10)]
    public string? Gender { get; set; }

    [Column("address", TypeName = "jsonb")]
    public string? Address { get; set; }

    /// <summary>
    /// Tổ chức chính của user (lưu ID, không FK vì khác service)
    /// </summary>
    [Column("primary_org_id")]
    public Guid? PrimaryOrgId { get; set; }

    [Column("avatar_url")]
    [MaxLength(500)]
    public string? AvatarUrl { get; set; }

    [Column("status")]
    [MaxLength(30)]
    public UserStatus Status { get; set; } = UserStatus.PENDING_VERIFICATION;

    [Column("mfa_enabled")]
    public bool MfaEnabled { get; set; } = false;

    [Column("mfa_secret")]
    [MaxLength(255)]
    public string? MfaSecret { get; set; }

    [Column("email_verified")]
    public bool EmailVerified { get; set; } = false;

    [Column("phone_verified")]
    public bool PhoneVerified { get; set; } = false;

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties (chỉ trong Auth Service)
    public virtual ICollection<UserDid> Dids { get; set; } = new List<UserDid>();
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
