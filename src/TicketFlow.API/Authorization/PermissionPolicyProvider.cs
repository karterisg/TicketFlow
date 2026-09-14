using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TicketFlow.API.Authorization;

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    public const string Prefix = "Perm:";

    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            var permissionKey = policyName[Prefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(permissionKey))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
