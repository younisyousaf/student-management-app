using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace StudentManagementApp.WebApi.Security;

public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallbackProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallbackProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _fallbackProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _fallbackProvider.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var parts = policyName.Split(':', 2);

        if (parts.Length != 2)
            return _fallbackProvider.GetPolicyAsync(policyName);

        var scope = parts[0].ToLowerInvariant() switch
        {
            "platform" => PermissionScope.Platform,
            "school" => PermissionScope.School,
            _ => (PermissionScope?)null
        };

        if (scope is null || string.IsNullOrWhiteSpace(parts[1]))
            return _fallbackProvider.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(
                new PermissionRequirement(parts[1], scope.Value))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
