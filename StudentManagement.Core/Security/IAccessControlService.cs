namespace StudentManagement.Core.Security;

public interface IAccessControlService
{
    Task<bool> HasPlatformPermissionAsync(int userId, string permission, CancellationToken cancellationToken = default);
    Task<bool> HasSchoolPermissionAsync(int userId, int schoolId, string permission, CancellationToken cancellationToken = default);
}