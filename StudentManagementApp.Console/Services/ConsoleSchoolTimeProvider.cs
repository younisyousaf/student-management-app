using StudentManagement.Core.Interfaces;

namespace StudentManagementSystem.Services;

public sealed class ConsoleSchoolTimeProvider(
    string timeZoneId,
    TimeProvider timeProvider) : ICurrentSchoolTimeProvider
{
    private readonly TimeZoneInfo _timeZone =
        TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

    public DateTime Today =>
        TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(),
            _timeZone).Date;
}