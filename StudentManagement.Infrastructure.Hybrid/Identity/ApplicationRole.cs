using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace StudentManagement.Infrastructure.Hybrid.Identity;

public sealed class ApplicationRole : IdentityRole<int>
{
    public string? Description { get; set; }
    public RoleScope Scope { get; set; }
    public bool IsSystemRole { get; set; } = true;
}