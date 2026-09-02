using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudentManagement.Core.Interfaces;
using StudentManagement.Core.Models;
using StudentManagement.Core.Security;
using StudentManagement.Infrastructure.Hybrid.Identity;
using StudentManagement.Infrastructure.Hybrid.Security;

namespace StudentManagement.Infrastructure.Hybrid.Services;

public sealed class SchoolService(
    HybridDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager) : ISchoolService
{
    public async Task<School> CreateAsync(
        string name,
        string code,
        string timeZoneId,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        var exists = await dbContext.Schools
            .AnyAsync(
                x => x.Code == normalizedCode,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException(
                $"A school with code '{normalizedCode}' already exists.");

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException(
                $"Timezone '{timeZoneId}' is invalid.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new ArgumentException(
                $"Timezone '{timeZoneId}' is invalid.");
        }

        var school = new School(
            name,
            normalizedCode,
            timeZoneId);

        dbContext.Schools.Add(school);
        await dbContext.SaveChangesAsync(cancellationToken);

        return school;
    }

    public async Task ProvisionAdminAsync(
    int schoolId,
    string username,
    string email,
    string password,
    int assignedByUserId,
    CancellationToken cancellationToken = default)
    {
        var school = await dbContext.Schools
            .SingleOrDefaultAsync(
                x => x.Id == schoolId,
                cancellationToken);

        if (school is null)
            throw new KeyNotFoundException("School not found.");

        if (!school.IsActive)
            throw new InvalidOperationException("School is inactive.");

        if (await userManager.FindByNameAsync(username) is not null)
            throw new InvalidOperationException("Username already exists.");

        if (await userManager.FindByEmailAsync(email) is not null)
            throw new InvalidOperationException("Email already exists.");

        var role = await roleManager.FindByNameAsync(SystemRoles.SchoolAdmin)
            ?? throw new InvalidOperationException("SchoolAdmin role does not exist.");

        await using var transaction =
            await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var user = new ApplicationUser
            {
                UserName = username.Trim(),
                Email = email.Trim(),
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult =
                await userManager.CreateAsync(user, password);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(
                    ", ",
                    createResult.Errors.Select(x => x.Description));

                throw new InvalidOperationException(errors);
            }

            var membership = new SchoolMembership
            {
                SchoolId = school.Id,
                UserId = user.Id,
                IsActive = true
            };

            dbContext.SchoolMemberships.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);

            dbContext.SchoolUserRoles.Add(new SchoolUserRole
            {
                SchoolMembershipId = membership.Id,
                RoleId = role.Id,
                AssignedByUserId = assignedByUserId
            });

            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

}