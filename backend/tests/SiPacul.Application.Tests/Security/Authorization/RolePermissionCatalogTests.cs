using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Organizations;
using Xunit;

namespace SiPacul.Application.Tests.Security.Authorization;

public sealed class RolePermissionCatalogTests
{
    [Fact]
    public void AllPermissions_ShouldBeUnique()
    {
        Assert.Equal(
            Permissions.All.Count,
            Permissions.All
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    [Fact]
    public void AllPermissions_ShouldUseStableNaming()
    {
        Assert.NotEmpty(Permissions.All);

        Assert.All(
            Permissions.All,
            permission =>
            {
                Assert.Equal(
                    permission,
                    permission.Trim());

                Assert.Equal(
                    permission.ToLowerInvariant(),
                    permission);

                Assert.DoesNotContain(
                    ' ',
                    permission);
            });
    }

    [Fact]
    public void PermissionCount_ShouldRemainStable()
    {
        Assert.Equal(22, Permissions.All.Count);
    }

    [Fact]
    public void Owner_ShouldReceiveEveryPermission()
    {
        var permissions =
            RolePermissionCatalog.GetPermissions(
                OrganizationRole.Owner);

        Assert.Equal(
            Permissions.All,
            permissions);
    }

    [Fact]
    public void Admin_ShouldNotAssignOwner()
    {
        var permissions =
            RolePermissionCatalog.GetPermissions(
                OrganizationRole.Admin);

        Assert.DoesNotContain(
            Permissions.MembersAssignOwner,
            permissions);

        Assert.Contains(
            Permissions.MembersManage,
            permissions);

        Assert.Contains(
            Permissions.ProfitSharingFinalize,
            permissions);
    }

    [Fact]
    public void Finance_ShouldHaveFinancialButNotOperationalWrite()
    {
        var permissions =
            RolePermissionCatalog.GetPermissions(
                OrganizationRole.Finance);

        Assert.Contains(
            Permissions.FinanceWrite,
            permissions);

        Assert.Contains(
            Permissions.ProfitSharingFinalize,
            permissions);

        Assert.Contains(
            Permissions.AuditRead,
            permissions);

        Assert.DoesNotContain(
            Permissions.LandsWrite,
            permissions);

        Assert.DoesNotContain(
            Permissions.CultivationWrite,
            permissions);

        Assert.DoesNotContain(
            Permissions.MembersManage,
            permissions);
    }

    [Fact]
    public void Operator_ShouldHaveOperationalButNotFinanceAccess()
    {
        var permissions =
            RolePermissionCatalog.GetPermissions(
                OrganizationRole.Operator);

        Assert.Contains(
            Permissions.CultivationWrite,
            permissions);

        Assert.Contains(
            Permissions.HarvestWrite,
            permissions);

        Assert.Contains(
            Permissions.SalesWrite,
            permissions);

        Assert.DoesNotContain(
            Permissions.FinanceRead,
            permissions);

        Assert.DoesNotContain(
            Permissions.ProfitSharingRead,
            permissions);

        Assert.DoesNotContain(
            Permissions.AuditRead,
            permissions);
    }

    [Fact]
    public void HasPermission_WithGrantedPermission_ShouldReturnTrue()
    {
        Assert.True(
            RolePermissionCatalog.HasPermission(
                OrganizationRole.Finance,
                Permissions.FinanceWrite));
    }

    [Fact]
    public void HasPermission_WithBlankPermission_ShouldReturnFalse()
    {
        Assert.False(
            RolePermissionCatalog.HasPermission(
                OrganizationRole.Owner,
                "   "));
    }

    [Fact]
    public void GetPermissions_WithUnknownRole_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RolePermissionCatalog.GetPermissions(
                (OrganizationRole)999));
    }

    [Theory]
    [InlineData(OrganizationRole.Owner)]
    [InlineData(OrganizationRole.Admin)]
    [InlineData(OrganizationRole.Finance)]
    [InlineData(OrganizationRole.Operator)]
    public void EachRole_ShouldContainOnlyKnownUniquePermissions(
        OrganizationRole role)
    {
        var permissions =
            RolePermissionCatalog.GetPermissions(role);

        Assert.NotEmpty(permissions);

        Assert.Equal(
            permissions.Count,
            permissions
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(
            permissions,
            permission =>
                Assert.Contains(
                    permission,
                    Permissions.All));
    }
}
