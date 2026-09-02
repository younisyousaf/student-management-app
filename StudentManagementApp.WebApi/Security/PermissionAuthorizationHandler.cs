using Microsoft.AspNetCore.Authorization;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Security;

namespace StudentManagementApp.WebApi.Security;

public sealed class PermissionAuthorizationHandler(
    ICurrentUserContext currentUser,
    IAccessControlService accessControl)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is not int userId)
            return;

        var allowed = requirement.Scope switch
        {
            PermissionScope.Platform =>
                await accessControl.HasPlatformPermissionAsync(
                    userId,
                    requirement.Permission),

            PermissionScope.School when currentUser.SchoolId is int schoolId =>
                await accessControl.HasSchoolPermissionAsync(
                    userId,
                    schoolId,
                    requirement.Permission),

            _ => false
        };

        if (allowed)
            context.Succeed(requirement);
    }
}