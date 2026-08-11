using Microsoft.Extensions.Configuration;
using StudentManagement.AI.Services;

public sealed class ApplicationDateTime : IApplicationDateTime
{
    private readonly TimeZoneInfo _timeZone;

    public ApplicationDateTime(IConfiguration configuration)
    {
        var timeZoneId =
            configuration["Application:TimeZoneId"]
            ?? throw new InvalidOperationException(
                "Application timezone is not configured.");

        _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    public DateTime Today
    {
        get
        {
            var localTime =
                TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    _timeZone);

            return localTime.Date;
        }
    }
}