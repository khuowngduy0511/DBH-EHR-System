using System.ComponentModel.DataAnnotations;
using DBH.Shared.Contracts;
using DBH.Shared.Infrastructure.Time;

namespace DBH.Auth.Service.Models.Entities;

public class Permission
{
    [Key]
    public Guid PermissionId { get; set; } = Guid.NewGuid();

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = VietnamTime.DatabaseNow;
    
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
