using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// Gán role cho user trong scope cụ thể
/// </summary>
[Table("user_roles")]
public class UserRole
{
    [Key]
    [Column("user_role_id")]
    public Guid UserRoleId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    /// <summary>
    /// Scope: Organization ID (null = global role)
    /// Lưu ID thay vì FK vì Organization ở service khác
    /// </summary>
    [Column("org_id")]
    public Guid? OrgId { get; set; }

    /// <summary>
    /// Scope: Department ID (null = organization-wide)
    /// Lưu ID thay vì FK vì Department ở service khác
    /// </summary>
    [Column("department_id")]
    public Guid? DepartmentId { get; set; }

    [Column("granted_by")]
    public Guid? GrantedBy { get; set; }

    [Column("granted_at")]
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Navigation (chỉ trong Auth Service)
    [ForeignKey("UserId")]
    public virtual User? User { get; set; }

    [ForeignKey("RoleId")]
    public virtual Role? Role { get; set; }
}
