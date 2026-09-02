using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class SchoolUserRole
{
    public int Id { get; set; }
    public int SchoolMembershipId { get; set; }
    public int RoleId { get; set; }
    public int? AssignedByUserId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public SchoolMembership SchoolMembership { get; set; } = null!;
    public ApplicationRole Role { get; set; } = null!;
    public ApplicationUser? AssignedByUser { get; set; }
}