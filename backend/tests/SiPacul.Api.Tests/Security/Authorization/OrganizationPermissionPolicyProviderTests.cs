using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Tests.Security.Authorization;

public sealed class
    OrganizationPermissionPolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_WithPermissionPolicy_ShouldBuildPolicy()
    {
        var provider =
            CreateProvider();

        var policyName =
            OrganizationPermissionPolicies
                .CreatePolicyName(
                    Permissions.FinanceRead);

        var policy =
            await provider.GetPolicyAsync(
                policyName);

        Assert.NotNull(policy);

        Assert.Contains(
            policy!.Requirements,
            requirement =>
                requirement is
                    DenyAnonymousAuthorizationRequirement);

        var permissionRequirement =
            Assert.Single(
                policy.Requirements.OfType<
                    OrganizationPermissionRequirement>());

        Assert.Equal(
            Permissions.FinanceRead,
            permissionRequirement.Permission);
    }

    [Fact]
    public async Task GetPolicyAsync_WithSameName_ShouldReturnCachedPolicy()
    {
        var provider =
            CreateProvider();

        var policyName =
            OrganizationPermissionPolicies
                .CreatePolicyName(
                    Permissions.SalesWrite);

        var first =
            await provider.GetPolicyAsync(
                policyName);

        var second =
            await provider.GetPolicyAsync(
                policyName);

        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetPolicyAsync_WithUnknownName_ShouldUseFallback()
    {
        var options =
            new AuthorizationOptions();

        options.AddPolicy(
            "ExistingPolicy",
            policy =>
                policy.RequireClaim(
                    "existing-claim"));

        var provider =
            new OrganizationPermissionPolicyProvider(
                Options.Create(options));

        var policy =
            await provider.GetPolicyAsync(
                "ExistingPolicy");

        Assert.NotNull(policy);

        Assert.Contains(
            policy!.Requirements,
            requirement =>
                requirement is
                    ClaimsAuthorizationRequirement);
    }

    [Fact]
    public async Task GetDefaultAndFallbackPolicy_ShouldDelegate()
    {
        var options =
            new AuthorizationOptions();

        options.FallbackPolicy =
            new AuthorizationPolicyBuilder()
                .RequireClaim("fallback-claim")
                .Build();

        var provider =
            new OrganizationPermissionPolicyProvider(
                Options.Create(options));

        var defaultPolicy =
            await provider.GetDefaultPolicyAsync();

        var fallbackPolicy =
            await provider.GetFallbackPolicyAsync();

        Assert.NotNull(defaultPolicy);
        Assert.Same(
            options.FallbackPolicy,
            fallbackPolicy);
    }

    private static
        OrganizationPermissionPolicyProvider
        CreateProvider()
    {
        return new OrganizationPermissionPolicyProvider(
            Options.Create(
                new AuthorizationOptions()));
    }
}
