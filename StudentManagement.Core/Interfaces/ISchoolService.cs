using StudentManagement.Core.Models;

namespace StudentManagement.Core.Interfaces;

public interface ISchoolService
{
    Task<School> CreateAsync(
        string name,
        string code,
        string timeZoneId,
        CancellationToken cancellationToken = default);

    Task ProvisionAdminAsync(
        int schoolId,
        string username,
        string email,
        string password,
        int assignedByUserId,
        CancellationToken cancellationToken = default);
}
