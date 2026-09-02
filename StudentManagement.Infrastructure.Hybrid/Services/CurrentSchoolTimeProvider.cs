using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;

namespace StudentManagement.Infrastructure.Hybrid.Services;

public sealed class CurrentSchoolTimeProvider(
    HybridDbContext context,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : ICurrentSchoolTimeProvider
{
    public DateTime Today
    {
        get
        {
            var schoolId = currentUser.SchoolId
                ?? throw new InvalidOperationException(
                    "A school must be selected.");

            var timeZoneId = context.Schools
                .AsNoTracking()
                .Where(x => x.Id == schoolId && x.IsActive)
                .Select(x => x.TimeZoneId)
                .SingleOrDefault()
                ?? throw new InvalidOperationException(
                    "Current school was not found or is inactive.");

            var timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var localTime =
                TimeZoneInfo.ConvertTime(
                    timeProvider.GetUtcNow(),
                    timeZone);

            return localTime.Date;
        }
    }
}