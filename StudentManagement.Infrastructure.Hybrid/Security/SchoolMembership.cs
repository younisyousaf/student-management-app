using StudentManagement.Core.Models;
using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class SchoolMembership
{
    public int Id { get; set; }
    public int SchoolId { get; set; }
    public int UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public School School { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}