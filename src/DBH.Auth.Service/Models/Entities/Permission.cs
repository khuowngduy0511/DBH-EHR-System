using System.ComponentModel.DataAnnotations;

namespace DBH.Auth.Service.Models.Entities;

public class Permission
{
    [Key]
    public Guid PermissionId { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
