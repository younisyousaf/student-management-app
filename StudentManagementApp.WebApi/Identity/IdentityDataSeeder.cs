using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Security;
using StudentManagement.Infrastructure.Hybrid;
using StudentManagement.Infrastructure.Hybrid.Identity;
using StudentManagement.Infrastructure.Hybrid.Security;

namespace StudentManagementApp.WebApi.Identity;

public static class IdentityDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<HybridDbContext>();

        await SeedRolesAsync(roleManager);
        await SeedPermissionsAsync(dbContext);
        await SeedRolePermissionsAsync(dbContext);
        await SeedPlatformAdminAsync(userManager, configuration);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            (SystemRoles.PlatformAdmin, "SmartCampus platform administrator.", RoleScope.Platform),
            (SystemRoles.SchoolAdmin, "Administrator of a school.", RoleScope.School),
            (SystemRoles.Registrar, "Manages students and enrollments.", RoleScope.School),
            (SystemRoles.Teacher, "Manages assigned academic resources.", RoleScope.School),
            (SystemRoles.Accountant, "Manages fees and payments.", RoleScope.School),
            (SystemRoles.Viewer, "Read-only school access.", RoleScope.School)
        };

        foreach (var (name, description, scope) in roles)
        {
            if (await roleManager.RoleExistsAsync(name))
                continue;

            var result = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = name,
                Description = description,
                Scope = scope,
                IsSystemRole = true
            });

            EnsureSucceeded(result, $"creating role '{name}'");
        }
    }

    private static async Task SeedPlatformAdminAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
        var username = configuration["BootstrapAdmin:Username"];
        var email = configuration["BootstrapAdmin:Email"];
        var password = configuration["BootstrapAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
            return;

        var user = await userManager.FindByNameAsync(username);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(createResult, "creating bootstrap PlatformAdmin");
        }

        if (!await userManager.IsInRoleAsync(user, SystemRoles.PlatformAdmin))
        {
            var roleResult = await userManager.AddToRoleAsync(user, SystemRoles.PlatformAdmin);
            EnsureSucceeded(roleResult, "assigning PlatformAdmin role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
            return;

        var errors = string.Join(", ", result.Errors.Select(x => x.Description));
        throw new InvalidOperationException($"Identity failed while {operation}: {errors}");
    }

    private static async Task SeedPermissionsAsync(HybridDbContext dbContext)
    {
        var existing = await dbContext.Permissions
            .Select(x => x.Name)
            .ToHashSetAsync();

        var permissions = Permissions.All
            .Where(x => !existing.Contains(x))
            .Select(x => new Permission { Name = x });

        dbContext.Permissions.AddRange(permissions);
        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(HybridDbContext dbContext)
    {
        var roles = await dbContext.Roles.ToDictionaryAsync(x => x.Name!);
        var permissions = await dbContext.Permissions.ToDictionaryAsync(x => x.Name);

        foreach (var (roleName, permissionNames) in RolePermissionMap)
        {
            if (!roles.TryGetValue(roleName, out var role))
                continue;

            foreach (var permissionName in permissionNames)
            {
                if (!permissions.TryGetValue(permissionName, out var permission))
                    continue;

                var exists = await dbContext.RolePermissions.AnyAsync(x =>
                    x.RoleId == role.Id &&
                    x.PermissionId == permission.Id);

                if (exists)
                    continue;

                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await dbContext.SaveChangesAsync();
    }

    private static readonly Dictionary<string, string[]> RolePermissionMap = new()
    {
        [SystemRoles.PlatformAdmin] =
        [
            Permissions.Schools.Read,
            Permissions.Schools.Create,
            Permissions.Schools.Update,
            Permissions.Schools.Deactivate,
            Permissions.Users.Read,
            Permissions.Users.Create,
            Permissions.Users.Update,
            Permissions.Users.Deactivate,
            Permissions.Users.AssignRoles,
            Permissions.Dashboard.Read
        ],

            [SystemRoles.SchoolAdmin] =
        [
            Permissions.Students.Read,
            Permissions.Students.Create,
            Permissions.Students.Update,
            Permissions.Students.Delete,
            Permissions.Courses.Read,
            Permissions.Courses.Create,
            Permissions.Courses.Update,
            Permissions.Courses.Delete,
            Permissions.Enrollments.Read,
            Permissions.Enrollments.Create,
            Permissions.Enrollments.Complete,
            Permissions.Enrollments.Drop,
            Permissions.Attendance.Read,
            Permissions.Attendance.Mark,
            Permissions.Attendance.Update,
            Permissions.Fees.Read,
            Permissions.Fees.RecordPayment,
            Permissions.Users.Read,
            Permissions.Users.Create,
            Permissions.Users.Update,
            Permissions.Users.Deactivate,
            Permissions.Users.AssignRoles,
            Permissions.Schools.Read,
            Permissions.Schools.Update,
            Permissions.Settings.Read,
            Permissions.Settings.Update,
            Permissions.Copilot.Use,
            Permissions.Knowledge.Read,
            Permissions.Knowledge.Manage,
            Permissions.Dashboard.Read
        ],

            [SystemRoles.Registrar] =
        [
            Permissions.Students.Read,
            Permissions.Students.Create,
            Permissions.Students.Update,
            Permissions.Courses.Read,
            Permissions.Enrollments.Read,
            Permissions.Enrollments.Create,
            Permissions.Enrollments.Complete,
            Permissions.Enrollments.Drop,
            Permissions.Attendance.Read,
            Permissions.Fees.Read,
            Permissions.Knowledge.Read,
            Permissions.Copilot.Use,
            Permissions.Dashboard.Read
        ],

            [SystemRoles.Teacher] =
        [
            Permissions.Students.Read,
            Permissions.Courses.Read,
            Permissions.Enrollments.Read,
            Permissions.Attendance.Read,
            Permissions.Attendance.Mark,
            Permissions.Attendance.Update,
            Permissions.Knowledge.Read,
            Permissions.Copilot.Use,
            Permissions.Dashboard.Read
        ],

            [SystemRoles.Accountant] =
        [
            Permissions.Students.Read,
            Permissions.Fees.Read,
            Permissions.Fees.RecordPayment,
            Permissions.Dashboard.Read
        ],

            [SystemRoles.Viewer] =
        [
            Permissions.Students.Read,
            Permissions.Courses.Read,
            Permissions.Enrollments.Read,
            Permissions.Attendance.Read,
            Permissions.Fees.Read,
            Permissions.Knowledge.Read,
            Permissions.Dashboard.Read
        ]
    };
}