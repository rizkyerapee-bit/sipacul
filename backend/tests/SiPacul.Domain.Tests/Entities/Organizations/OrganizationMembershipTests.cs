using SiPacul.Domain.Entities.Organizations;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Organizations;

public sealed class OrganizationMembershipTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid UserId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    [Fact]
    public void Create_WithValidData_ShouldCreateActiveMembership()
    {
        var before = DateTime.UtcNow;

        var membership =
            OrganizationMembership.Create(
                OrganizationId,
                UserId,
                OrganizationRole.Owner);

        var after = DateTime.UtcNow;

        Assert.NotEqual(Guid.Empty, membership.Id);
        Assert.Equal(
            OrganizationId,
            membership.OrganizationId);

        Assert.Equal(UserId, membership.UserId);

        Assert.Equal(
            OrganizationRole.Owner,
            membership.Role);

        Assert.Equal(
            OrganizationMembershipStatus.Active,
            membership.Status);

        Assert.True(membership.IsActive);

        Assert.InRange(
            membership.JoinedAt,
            before,
            after);

        Assert.Null(membership.SuspendedAt);
        Assert.Null(membership.UpdatedAt);
    }

    [Fact]
    public void Create_WithEmptyOrganization_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganizationMembership.Create(
                Guid.Empty,
                UserId,
                OrganizationRole.Admin));
    }

    [Fact]
    public void Create_WithEmptyUser_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            OrganizationMembership.Create(
                OrganizationId,
                Guid.Empty,
                OrganizationRole.Admin));
    }

    [Fact]
    public void Create_WithUnknownRole_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OrganizationMembership.Create(
                OrganizationId,
                UserId,
                (OrganizationRole)999));
    }

    [Fact]
    public void ChangeRole_WithDifferentRole_ShouldUpdate()
    {
        var membership = CreateMembership();

        membership.ChangeRole(
            OrganizationRole.Finance);

        Assert.Equal(
            OrganizationRole.Finance,
            membership.Role);

        Assert.NotNull(membership.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_WithSameRole_ShouldNotUpdate()
    {
        var membership = CreateMembership();

        membership.ChangeRole(
            OrganizationRole.Admin);

        Assert.Equal(
            OrganizationRole.Admin,
            membership.Role);

        Assert.Null(membership.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_WithUnknownRole_ShouldThrow()
    {
        var membership = CreateMembership();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            membership.ChangeRole(
                (OrganizationRole)999));
    }

    [Fact]
    public void Suspend_WhenActive_ShouldSuspend()
    {
        var membership = CreateMembership();

        membership.Suspend();

        Assert.Equal(
            OrganizationMembershipStatus.Suspended,
            membership.Status);

        Assert.False(membership.IsActive);
        Assert.NotNull(membership.SuspendedAt);
        Assert.NotNull(membership.UpdatedAt);
    }

    [Fact]
    public void Suspend_WhenAlreadySuspended_ShouldBeIdempotent()
    {
        var membership = CreateMembership();

        membership.Suspend();

        var suspendedAt = membership.SuspendedAt;
        var updatedAt = membership.UpdatedAt;

        membership.Suspend();

        Assert.Equal(
            suspendedAt,
            membership.SuspendedAt);

        Assert.Equal(updatedAt, membership.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenSuspended_ShouldActivate()
    {
        var membership = CreateMembership();

        membership.Suspend();
        membership.Activate();

        Assert.Equal(
            OrganizationMembershipStatus.Active,
            membership.Status);

        Assert.True(membership.IsActive);
        Assert.Null(membership.SuspendedAt);
        Assert.NotNull(membership.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ShouldBeIdempotent()
    {
        var membership = CreateMembership();

        membership.Activate();

        Assert.True(membership.IsActive);
        Assert.Null(membership.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_ShouldMakeMembershipInactive()
    {
        var membership = CreateMembership();

        membership.SoftDelete(
            UserId.ToString());

        Assert.True(membership.IsDeleted);
        Assert.False(membership.IsActive);
    }

    [Theory]
    [InlineData(1, OrganizationRole.Owner)]
    [InlineData(2, OrganizationRole.Admin)]
    [InlineData(3, OrganizationRole.Finance)]
    [InlineData(4, OrganizationRole.Operator)]
    public void OrganizationRole_ValuesShouldRemainStable(
        int expected,
        OrganizationRole role)
    {
        Assert.Equal(expected, (int)role);
    }

    [Theory]
    [InlineData(
        1,
        OrganizationMembershipStatus.Active)]
    [InlineData(
        2,
        OrganizationMembershipStatus.Suspended)]
    public void MembershipStatus_ValuesShouldRemainStable(
        int expected,
        OrganizationMembershipStatus status)
    {
        Assert.Equal(expected, (int)status);
    }

    private static OrganizationMembership
        CreateMembership()
    {
        return OrganizationMembership.Create(
            OrganizationId,
            UserId,
            OrganizationRole.Admin);
    }
}
