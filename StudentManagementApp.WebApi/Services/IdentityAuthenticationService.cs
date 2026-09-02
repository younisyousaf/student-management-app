using Microsoft.AspNetCore.Identity;
using StudentManagement.Infrastructure.Hybrid.Identity;

namespace StudentManagementApp.WebApi.Services;

public sealed class IdentityAuthenticationService(UserManager<ApplicationUser> userManager, IJwtTokenService jwtTokenService) : IIdentityAuthenticationService
{
    public async Task<IdentityLoginResult> LoginAsync(string username, string password)
    {
        var user = await userManager.FindByNameAsync(username);

        if (user is null)
            return new(false, Error: "Invalid username or password.");

        if (!user.IsActive)
            return new(false, Error: "Your account is inactive.");

        if (await userManager.IsLockedOutAsync(user))
            return new(false, Error: "Your account is temporarily locked.");

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
                return new(false, Error: "Your account is temporarily locked.");

            return new(false, Error: "Invalid username or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.Generate(user, roles);

        return new(true, token);
    }
}