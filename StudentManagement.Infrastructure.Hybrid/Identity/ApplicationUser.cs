using Microsoft.AspNetCore.Identity;

namespace StudentManagement.Infrastructure.Hybrid.Identity;

public sealed class ApplicationUser : IdentityUser<int>
{
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}
