using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace SiPacul.Api.Security.Authorization;

public sealed class OrganizationPermissionPolicyProvider :
    IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider
        _fallbackPolicyProvider;

    private readonly ConcurrentDictionary<
        string,
        AuthorizationPolicy> _policies =
            new(StringComparer.Ordinal);

    public OrganizationPermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fallbackPolicyProvider =
            new DefaultAuthorizationPolicyProvider(
                options);
    }

    public Task<AuthorizationPolicy?>
        GetPolicyAsync(
            string policyName)
    {
        if (!OrganizationPermissionPolicies
            .TryGetPermission(
                policyName,
                out var permission))
        {
            return _fallbackPolicyProvider
                .GetPolicyAsync(policyName);
        }

        var policy =
            _policies.GetOrAdd(
                policyName,
                _ =>
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddRequirements(
                            new OrganizationPermissionRequirement(
                                permission))
                        .Build());

        return Task.FromResult<
            AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy>
        GetDefaultPolicyAsync()
    {
        return _fallbackPolicyProvider
            .GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?>
        GetFallbackPolicyAsync()
    {
        return _fallbackPolicyProvider
            .GetFallbackPolicyAsync();
    }
}
