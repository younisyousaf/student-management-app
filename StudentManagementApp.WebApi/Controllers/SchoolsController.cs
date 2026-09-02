using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Security;
using StudentManagementApp.WebApi.DTOs.Schools;
using StudentManagementApp.WebApi.Security;

namespace StudentManagementApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class SchoolsController(
    ISchoolService schoolService, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpPost]
    [RequirePermission(
        Permissions.Schools.Create,
        PermissionScope.Platform
    )]
    public async Task<IActionResult> Create(
        CreateSchoolRequest request,
        CancellationToken cancellationToken)
    {
        var school = await schoolService.CreateAsync(
            request.Name,
            request.Code,
            request.TimeZoneId,
            cancellationToken);

        return Created($"/api/schools/{school.Id}",
            new
            {
                school.Id,
                school.Name,
                school.Code,
                school.TimeZoneId,
                school.IsActive,
                school.CreatedAt
            });
    }

    [HttpPost("{schoolId:int}/admin")]
    [RequirePermission(
    Permissions.Users.Create,
    PermissionScope.Platform)]
    public async Task<IActionResult> ProvisionAdmin(
    int schoolId,
    ProvisionSchoolAdminRequest request,
    CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        await schoolService.ProvisionAdminAsync(
            schoolId,
            request.Username,
            request.Email,
            request.Password,
            userId,
            cancellationToken);

        return Ok(new
        {
            message = "School administrator provisioned successfully."
        });
    }

    [HttpGet("permission-test")]
    [RequirePermission(Permissions.Students.Read)]
    public IActionResult TestSchoolPermission()
    {
        return Ok(new
        {
            message = "School-scoped permission works."
        });
    }

    [HttpGet("permission-denied-test")]
    [RequirePermission(
    Permissions.Schools.Create,
    PermissionScope.School)]
    public IActionResult TestDeniedPermission()
    {
        return Ok();
    }

}