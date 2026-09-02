using Microsoft.AspNetCore.Authorization;

namespace StudentManagementApp.WebApi.Security;

public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(
        string permission,
        PermissionScope scope = PermissionScope.School)
    {
        Policy = $"{scope.ToString().ToLowerInvariant()}:{permission}";
    }
}