using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DBH.Auth.Service.Models.Enums;

namespace DBH.Auth.Service.Models.Entities;

/// <summary>
/// Role trong hệ thống (RBAC)
/// </summary>
[Table("roles")]
public class Role
{
    [Key]
    [Column("role_id")]
    public Guid RoleId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("role_name")]
    [MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [Column("role_code")]
    [MaxLength(30)]
    public string? RoleCode { get; set; }

    [Column("role_scope")]
    [MaxLength(30)]
    public RoleScope RoleScope { get; set; } = RoleScope.ORGANIZATION;

    /// <summary>
    /// Danh sách permissions dạng JSON array
    /// VD: ["ehr:read", "ehr:write", "patient:view"]
    /// </summary>
    [Column("permissions", TypeName = "jsonb")]
    public string? Permissions { get; set; }

    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("is_system_role")]
    public bool IsSystemRole { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
