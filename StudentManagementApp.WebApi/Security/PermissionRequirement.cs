using Microsoft.AspNetCore.Authorization;

namespace StudentManagementApp.WebApi.Security;

public sealed class PermissionRequirement(
    string permission,
    PermissionScope scope) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
    public PermissionScope Scope { get; } = scope;
}