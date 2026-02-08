namespace DBH.Auth.Service.Models.Entities;

public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
