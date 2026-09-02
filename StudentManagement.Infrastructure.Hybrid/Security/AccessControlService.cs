using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Security;
using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class AccessControlService(HybridDbContext dbContext) : IAccessControlService
{
    public Task<bool> HasPlatformPermissionAsync(
        int userId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return (
            from user in dbContext.Users
            join userRole in dbContext.UserRoles on user.Id equals userRole.UserId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            join rolePermission in dbContext.RolePermissions on role.Id equals rolePermission.RoleId
            join p in dbContext.Permissions on rolePermission.PermissionId equals p.Id
            where user.Id == userId
                && user.IsActive
                && role.Scope == RoleScope.Platform
                && p.Name == permission
            select userRole
        ).AnyAsync(cancellationToken);
    }

    public Task<bool> HasSchoolPermissionAsync(
        int userId,
        int schoolId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return (
            from user in dbContext.Users
            join membership in dbContext.SchoolMemberships on user.Id equals membership.UserId
            join school in dbContext.Schools on membership.SchoolId equals school.Id
            join userRole in dbContext.SchoolUserRoles on membership.Id equals userRole.SchoolMembershipId
            join role in dbContext.Roles on userRole.RoleId equals role.Id
            join rolePermission in dbContext.RolePermissions on role.Id equals rolePermission.RoleId
            join p in dbContext.Permissions on rolePermission.PermissionId equals p.Id
            where user.Id == userId
                && school.Id == schoolId
                && user.IsActive
                && school.IsActive
                && membership.IsActive
                && role.Scope == RoleScope.School
                && p.Name == permission
            select userRole
        ).AnyAsync(cancellationToken);
    }
}