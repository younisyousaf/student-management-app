using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Security;
using StudentManagement.Infrastructure.Hybrid.Identity;
using StudentManagementApp.WebApi.DTOs;
using StudentManagementApp.WebApi.Services;

namespace StudentManagementApp.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    IIdentityAuthenticationService authenticationService,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration,
    IJwtTokenService jwtTokenService,
    ISchoolContextService schoolContextService,
    ICurrentUserContext currentUser) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto request)
    {
        var result = await authenticationService.LoginAsync(
            request.Username,
            request.Password);

        if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Token))
            return Unauthorized(new
            {
                message = result.Error ?? "Login failed."
            });

        SetAccessTokenCookie(result.Token);

        return Ok(new
        {
            message = "Login successful.",
            token = result.Token
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return Ok(new { message = "Logged out." });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.GetUserAsync(User);

        if (user is null || !user.IsActive)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);

        return Ok(new
        {
            id = user.Id,
            username = user.UserName,
            email = user.Email,
            roles,
            schoolId = currentUser.SchoolId,
            isActive = user.IsActive
        });
    }

    [Authorize]
    [HttpGet("schools")]
    public async Task<IActionResult> GetSchools(
    CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        var schools = await schoolContextService
            .GetAccessibleSchoolsAsync(
                userId,
                cancellationToken);

        return Ok(schools.Select(school => new
        {
            school.Id,
            school.Name,
            school.Code,
            school.TimeZoneId
        }));
    }

    [Authorize]
    [HttpPost("schools/{schoolId:int}/select")]
    public async Task<IActionResult> SelectSchool(
    int schoolId,
    CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not int userId)
            return Unauthorized();

        var school = await schoolContextService
            .GetAccessibleSchoolAsync(
                userId,
                schoolId,
                cancellationToken);

        if (school is null)
            return Forbid();

        var user = await userManager
            .FindByIdAsync(userId.ToString());

        if (user is null || !user.IsActive)
            return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);

        var token = jwtTokenService.Generate(
            user,
            roles,
            school.Id);

        SetAccessTokenCookie(token);

        return Ok(new
        {
            message = "School selected successfully.",
            school = new
            {
                school.Id,
                school.Name,
                school.Code,
                school.TimeZoneId
            }
        });
    }

    private void SetAccessTokenCookie(string token)
    {
        var duration = double.Parse(
            configuration["JwtSettings:DurationInMinutes"] ?? "180");

        Response.Cookies.Append(
            "access_token",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(duration)
            });
    }

}