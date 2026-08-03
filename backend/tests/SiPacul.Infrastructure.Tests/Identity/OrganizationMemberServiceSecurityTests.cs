using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Application.Organizations.Members;
using SiPacul.Application.Organizations.Members.Contracts;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Identity;

namespace SiPacul.Infrastructure.Tests.Identity;

public sealed class OrganizationMemberServiceSecurityTests
{
    [Fact]
    public async Task GetAll_ShouldReturnOnlyRequestedOrganization()
    {
        await using var harness = CreateHarness();
        var seed = await SeedTwoOrganizationsAsync(harness);

        var result = await harness.Service.GetAllAsync(
            seed.OrganizationAId);

        Assert.True(result.IsSuccess);

        var member = Assert.Single(result.Value);

        Assert.Equal(
            seed.MembershipAId,
            member.MembershipId);

        Assert.NotEqual(
            seed.MembershipBId,
            member.MembershipId);
    }

    [Fact]
    public async Task GetById_FromAnotherOrganization_ShouldBeNotFound()
    {
        await using var harness = CreateHarness();
        var seed = await SeedTwoOrganizationsAsync(harness);

        var result = await harness.Service.GetByIdAsync(
            seed.OrganizationBId,
            seed.MembershipAId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.NotFoundCode,
            result.Error.Code);
    }

    [Fact]
    public async Task ChangeRole_FromAnotherOrganization_ShouldNotMutate()
    {
        await using var harness = CreateHarness();
        var seed = await SeedTwoOrganizationsAsync(harness);

        var result = await harness.Service.ChangeRoleAsync(
            seed.OrganizationBId,
            seed.MembershipAId,
            new UpdateOrganizationMemberRoleRequest(
                OrganizationRole.Admin));

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.NotFoundCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipAId);

        Assert.Equal(
            OrganizationRole.Operator,
            persisted.Role);
    }

    [Fact]
    public async Task Suspend_FromAnotherOrganization_ShouldNotMutate()
    {
        await using var harness = CreateHarness();
        var seed = await SeedTwoOrganizationsAsync(harness);

        var result = await harness.Service.SuspendAsync(
            seed.OrganizationBId,
            seed.MembershipAId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.NotFoundCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipAId);

        Assert.Equal(
            OrganizationMembershipStatus.Active,
            persisted.Status);
    }

    [Fact]
    public async Task Activate_FromAnotherOrganization_ShouldNotMutate()
    {
        await using var harness = CreateHarness();
        var seed = await SeedTwoOrganizationsAsync(
            harness,
            suspendMembershipA: true);

        var result = await harness.Service.ActivateAsync(
            seed.OrganizationBId,
            seed.MembershipAId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.NotFoundCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipAId);

        Assert.Equal(
            OrganizationMembershipStatus.Suspended,
            persisted.Status);
    }

    [Fact]
    public async Task Create_WithOwnerRole_ShouldBeProtected()
    {
        await using var harness = CreateHarness();

        var result = await harness.Service.CreateAsync(
            Guid.NewGuid(),
            new CreateOrganizationMemberRequest(
                "second-owner@example.com",
                "StrongPass123!",
                OrganizationRole.Owner));

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.OwnerProtectedCode,
            result.Error.Code);
    }

    [Fact]
    public async Task ChangeRole_ToOwner_ShouldNotMutate()
    {
        await using var harness = CreateHarness();
        var seed = await SeedSingleMembershipAsync(
            harness,
            OrganizationRole.Admin);

        var result = await harness.Service.ChangeRoleAsync(
            seed.OrganizationId,
            seed.MembershipId,
            new UpdateOrganizationMemberRoleRequest(
                OrganizationRole.Owner));

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.OwnerProtectedCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipId);

        Assert.Equal(
            OrganizationRole.Admin,
            persisted.Role);
    }

    [Fact]
    public async Task ChangeRole_OfOwner_ShouldNotMutate()
    {
        await using var harness = CreateHarness();
        var seed = await SeedSingleMembershipAsync(
            harness,
            OrganizationRole.Owner);

        var result = await harness.Service.ChangeRoleAsync(
            seed.OrganizationId,
            seed.MembershipId,
            new UpdateOrganizationMemberRoleRequest(
                OrganizationRole.Admin));

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.OwnerProtectedCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipId);

        Assert.Equal(
            OrganizationRole.Owner,
            persisted.Role);
    }

    [Fact]
    public async Task Suspend_Owner_ShouldPreserveActiveStatus()
    {
        await using var harness = CreateHarness();
        var seed = await SeedSingleMembershipAsync(
            harness,
            OrganizationRole.Owner);

        var result = await harness.Service.SuspendAsync(
            seed.OrganizationId,
            seed.MembershipId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.OwnerProtectedCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipId);

        Assert.Equal(
            OrganizationMembershipStatus.Active,
            persisted.Status);
    }

    [Fact]
    public async Task Activate_Owner_ShouldPreserveSuspendedStatus()
    {
        await using var harness = CreateHarness();
        var seed = await SeedSingleMembershipAsync(
            harness,
            OrganizationRole.Owner,
            suspended: true);

        var result = await harness.Service.ActivateAsync(
            seed.OrganizationId,
            seed.MembershipId);

        Assert.True(result.IsFailure);
        Assert.Equal(
            OrganizationMemberErrors.OwnerProtectedCode,
            result.Error.Code);

        var persisted = await LoadMembershipAsync(
            harness,
            seed.MembershipId);

        Assert.Equal(
            OrganizationMembershipStatus.Suspended,
            persisted.Status);
    }

    private static ServiceHarness CreateHarness()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<SiPaculDbContext>(options =>
            options.UseInMemoryDatabase(
                $"sipacul-members-{Guid.NewGuid():N}"));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddEntityFrameworkStores<SiPaculDbContext>();

        services.AddScoped<OrganizationMemberService>();

        return new ServiceHarness(
            services.BuildServiceProvider());
    }

    private static async Task<TwoOrganizationSeed>
        SeedTwoOrganizationsAsync(
            ServiceHarness harness,
            bool suspendMembershipA = false)
    {
        var organizationA = Organization.Create(
            "ORG-A",
            "Organization A");

        var organizationB = Organization.Create(
            "ORG-B",
            "Organization B");

        var userA = ApplicationUser.Create(
            $"operator-a-{Guid.NewGuid():N}@example.com");

        var userB = ApplicationUser.Create(
            $"operator-b-{Guid.NewGuid():N}@example.com");

        var membershipA = OrganizationMembership.Create(
            organizationA.Id,
            userA.Id,
            OrganizationRole.Operator);

        var membershipB = OrganizationMembership.Create(
            organizationB.Id,
            userB.Id,
            OrganizationRole.Finance);

        if (suspendMembershipA)
        {
            membershipA.Suspend();
        }

        harness.DbContext.Organizations.AddRange(
            organizationA,
            organizationB);

        harness.DbContext.ApplicationUsers.AddRange(
            userA,
            userB);

        harness.DbContext.OrganizationMemberships.AddRange(
            membershipA,
            membershipB);

        await harness.DbContext.SaveChangesAsync();
        harness.DbContext.ChangeTracker.Clear();

        return new TwoOrganizationSeed(
            organizationA.Id,
            organizationB.Id,
            membershipA.Id,
            membershipB.Id);
    }

    private static async Task<SingleMembershipSeed>
        SeedSingleMembershipAsync(
            ServiceHarness harness,
            OrganizationRole role,
            bool suspended = false)
    {
        var organization = Organization.Create(
            "ORG",
            "Organization");

        var user = ApplicationUser.Create(
            $"member-{Guid.NewGuid():N}@example.com");

        var membership = OrganizationMembership.Create(
            organization.Id,
            user.Id,
            role);

        if (suspended)
        {
            membership.Suspend();
        }

        harness.DbContext.Organizations.Add(organization);
        harness.DbContext.ApplicationUsers.Add(user);
        harness.DbContext.OrganizationMemberships.Add(membership);

        await harness.DbContext.SaveChangesAsync();
        harness.DbContext.ChangeTracker.Clear();

        return new SingleMembershipSeed(
            organization.Id,
            membership.Id);
    }

    private static Task<OrganizationMembership>
        LoadMembershipAsync(
            ServiceHarness harness,
            Guid membershipId)
    {
        harness.DbContext.ChangeTracker.Clear();

        return harness.DbContext.OrganizationMemberships
            .AsNoTracking()
            .SingleAsync(candidate =>
                candidate.Id == membershipId);
    }

    private sealed class ServiceHarness : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public ServiceHarness(ServiceProvider provider)
        {
            _provider = provider;
            _scope = provider.CreateScope();

            DbContext = _scope.ServiceProvider
                .GetRequiredService<SiPaculDbContext>();

            Service = _scope.ServiceProvider
                .GetRequiredService<OrganizationMemberService>();
        }

        public SiPaculDbContext DbContext { get; }

        public OrganizationMemberService Service { get; }

        public async ValueTask DisposeAsync()
        {
            _scope.Dispose();
            await _provider.DisposeAsync();
        }
    }

    private sealed record TwoOrganizationSeed(
        Guid OrganizationAId,
        Guid OrganizationBId,
        Guid MembershipAId,
        Guid MembershipBId);

    private sealed record SingleMembershipSeed(
        Guid OrganizationId,
        Guid MembershipId);
}
