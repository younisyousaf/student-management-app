using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagementApp.WebApi.Services;

public interface IJwtTokenService
{
    string Generate(
        ApplicationUser user,
        IEnumerable<string> roles,
        int? schoolId = null);
}