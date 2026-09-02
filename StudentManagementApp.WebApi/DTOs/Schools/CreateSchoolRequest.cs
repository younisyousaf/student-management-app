namespace StudentManagementApp.WebApi.DTOs.Schools;

public sealed record CreateSchoolRequest(
    string Name,
    string Code,
    string TimeZoneId);
