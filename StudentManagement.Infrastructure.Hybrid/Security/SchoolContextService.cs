using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Models;
using StudentManagement.Core.Security;

namespace StudentManagement.Infrastructure.Hybrid.Security;

public sealed class SchoolContextService(
    HybridDbContext dbContext) : ISchoolContextService
{
    public async Task<IReadOnlyList<School>> GetAccessibleSchoolsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from membership in dbContext.SchoolMemberships
            join school in dbContext.Schools
                on membership.SchoolId equals school.Id
            where membership.UserId == userId
                && membership.IsActive
                && school.IsActive
            orderby school.Name
            select school
        )
        .AsNoTracking()
        .ToListAsync(cancellationToken);
    }

    public Task<School?> GetAccessibleSchoolAsync(
        int userId,
        int schoolId,
        CancellationToken cancellationToken = default)
    {
        return (
            from membership in dbContext.SchoolMemberships
            join school in dbContext.Schools
                on membership.SchoolId equals school.Id
            where membership.UserId == userId
                && membership.SchoolId == schoolId
                && membership.IsActive
                && school.IsActive
            select school
        )
        .AsNoTracking()
        .SingleOrDefaultAsync(cancellationToken);
    }
}
