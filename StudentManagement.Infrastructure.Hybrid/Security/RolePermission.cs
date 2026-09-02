using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class RolePermission
{
    public int Id { get; set; }
    public int RoleId { get; set; }
    public int PermissionId { get; set; }

    public ApplicationRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}