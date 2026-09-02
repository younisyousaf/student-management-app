using System.Security.Claims;
using StudentManagement.Core.Interfaces;
using StudentManagementApp.WebApi.Security;

namespace StudentManagementApp.WebApi.Services;

public sealed class CurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
    private ClaimsPrincipal? User => accessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public int? UserId =>
        int.TryParse(User?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;

    public string? Username =>
        User?.FindFirstValue(ClaimTypes.Name);

    public string? Email =>
        User?.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        User?.FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
        ?? [];

    public int? SchoolId =>
        int.TryParse(User?.FindFirstValue(SmartCampusClaimTypes.SchoolId), out var id)
            ? id
            : null;
}