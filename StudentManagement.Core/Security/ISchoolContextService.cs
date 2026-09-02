using StudentManagement.Core.Models;

namespace StudentManagement.Core.Security;

public interface ISchoolContextService
{
    Task<IReadOnlyList<School>> GetAccessibleSchoolsAsync(
        int userId,
        CancellationToken cancellationToken = default);

    Task<School?> GetAccessibleSchoolAsync(
        int userId,
        int schoolId,
        CancellationToken cancellationToken = default);
}
