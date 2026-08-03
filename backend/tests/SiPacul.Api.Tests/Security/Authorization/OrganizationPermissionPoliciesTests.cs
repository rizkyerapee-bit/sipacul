using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Tests.Security.Authorization;

public sealed class OrganizationPermissionPoliciesTests
{
    [Fact]
    public void CreatePolicyName_WithKnownPermission_ShouldCreateName()
    {
        var policyName =
            OrganizationPermissionPolicies
                .CreatePolicyName(
                    Permissions.OrganizationsRead);

        Assert.Equal(
            OrganizationPermissionPolicies.Prefix +
                Permissions.OrganizationsRead,
            policyName);
    }

    [Fact]
    public void CreatePolicyName_WithWhitespace_ShouldTrimPermission()
    {
        var policyName =
            OrganizationPermissionPolicies
                .CreatePolicyName(
                    "  " +
                    Permissions.OrganizationsRead +
                    "  ");

        Assert.Equal(
            OrganizationPermissionPolicies.Prefix +
                Permissions.OrganizationsRead,
            policyName);
    }

    [Fact]
    public void CreatePolicyName_WithBlankPermission_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganizationPermissionPolicies
                .CreatePolicyName(" "));
    }

    [Fact]
    public void CreatePolicyName_WithUnknownPermission_ShouldThrow()
    {
        Assert.Throws<
            ArgumentOutOfRangeException>(() =>
                OrganizationPermissionPolicies
                    .CreatePolicyName(
                        "unknown.permission"));
    }

    [Fact]
    public void TryGetPermission_WithKnownPolicy_ShouldReturnPermission()
    {
        var policyName =
            OrganizationPermissionPolicies
                .CreatePolicyName(
                    Permissions.LandsRead);

        var succeeded =
            OrganizationPermissionPolicies
                .TryGetPermission(
                    policyName,
                    out var permission);

        Assert.True(succeeded);
        Assert.Equal(
            Permissions.LandsRead,
            permission);
    }

    [Fact]
    public void TryGetPermission_WithUnrelatedOrUnknownPolicy_ShouldFail()
    {
        Assert.False(
            OrganizationPermissionPolicies
                .TryGetPermission(
                    "ExistingPolicy",
                    out _));

        Assert.False(
            OrganizationPermissionPolicies
                .TryGetPermission(
                    OrganizationPermissionPolicies
                        .Prefix +
                    "unknown.permission",
                    out _));
    }
}
