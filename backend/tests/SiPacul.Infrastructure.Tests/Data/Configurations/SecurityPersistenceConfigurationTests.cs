using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure;
using SiPacul.Infrastructure.Data;
using SiPacul.Infrastructure.Identity;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations;

public sealed class
    SecurityPersistenceConfigurationTests
{
    [Fact]
    public void DbContext_ShouldUseIdentityUserContext()
    {
        Assert.True(
            typeof(
                IdentityUserContext<
                    ApplicationUser,
                    Guid>)
                .IsAssignableFrom(
                    typeof(SiPaculDbContext)));
    }

    [Fact]
    public void ApplicationUser_ShouldUseUsersTableAndGuidKey()
    {
        using var context = CreateContext();

        var entity =
            context.Model.FindEntityType(
                typeof(ApplicationUser));

        Assert.NotNull(entity);
        Assert.Equal("Users", entity!.GetTableName());

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey!.Properties);

        Assert.Equal(
            nameof(ApplicationUser.Id),
            primaryKey.Properties[0].Name);

        Assert.Equal(
            typeof(Guid),
            primaryKey.Properties[0].ClrType);
    }

    [Theory]
    [InlineData(
        typeof(IdentityUserClaim<Guid>),
        "UserClaims")]
    [InlineData(
        typeof(IdentityUserLogin<Guid>),
        "UserLogins")]
    [InlineData(
        typeof(IdentityUserToken<Guid>),
        "UserTokens")]

    public void IdentitySupportEntity_ShouldUseExpectedTable(
        Type entityType,
        string expectedTable)
    {
        using var context = CreateContext();

        var entity =
            context.Model.FindEntityType(entityType);

        Assert.NotNull(entity);

        Assert.Equal(
            expectedTable,
            entity!.GetTableName());
    }

    [Fact]
    public void PasskeyPersistence_ShouldRemainDeferred()
    {
        using var context = CreateContext();

        Assert.Null(
            context.Model.FindEntityType(
                typeof(IdentityUserPasskey<Guid>)));
    }

    [Fact]
    public void GlobalRoleEntities_ShouldNotExist()
    {
        using var context = CreateContext();

        Assert.Null(
            context.Model.FindEntityType(
                typeof(IdentityRole<Guid>)));

        Assert.Null(
            context.Model.FindEntityType(
                typeof(IdentityUserRole<Guid>)));

        Assert.Null(
            context.Model.FindEntityType(
                typeof(IdentityRoleClaim<Guid>)));
    }

    [Fact]
    public void Membership_ShouldUseExpectedTableAndPrimaryKey()
    {
        using var context = CreateContext();

        var entity = MembershipEntity(context);

        Assert.Equal(
            "OrganizationMemberships",
            entity.GetTableName());

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Single(primaryKey!.Properties);

        Assert.Equal(
            nameof(OrganizationMembership.Id),
            primaryKey.Properties[0].Name);
    }

    [Fact]
    public void Membership_ShouldHaveAlternateOrganizationKey()
    {
        using var context = CreateContext();

        var entity = MembershipEntity(context);

        var key =
            entity.GetKeys()
                .Single(candidate =>
                    candidate.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(
                            new[]
                            {
                                nameof(
                                    OrganizationMembership
                                        .OrganizationId),
                                nameof(
                                    OrganizationMembership.Id)
                            }));

        Assert.Equal(
            "AK_OrganizationMemberships_Org_Id",
            key.GetName());
    }

    [Fact]
    public void Membership_ShouldHaveUniqueOrganizationUserIndex()
    {
        using var context = CreateContext();

        var index =
            MembershipEntity(context)
                .GetIndexes()
                .Single(candidate =>
                    candidate.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(
                            new[]
                            {
                                nameof(
                                    OrganizationMembership
                                        .OrganizationId),
                                nameof(
                                    OrganizationMembership
                                        .UserId)
                            }));

        Assert.True(index.IsUnique);

        Assert.Equal(
            "UX_OrganizationMemberships_Org_User",
            index.GetDatabaseName());
    }

    [Fact]
    public void Membership_ShouldHaveRestrictForeignKeys()
    {
        using var context = CreateContext();

        var foreignKeys =
            MembershipEntity(context)
                .GetForeignKeys()
                .ToArray();

        Assert.Equal(2, foreignKeys.Length);

        Assert.All(
            foreignKeys,
            foreignKey =>
                Assert.Equal(
                    DeleteBehavior.Restrict,
                    foreignKey.DeleteBehavior));

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.PrincipalEntityType.ClrType ==
                    typeof(Organization));

        Assert.Contains(
            foreignKeys,
            foreignKey =>
                foreignKey.PrincipalEntityType.ClrType ==
                    typeof(ApplicationUser));
    }

    [Fact]
    public void Membership_ShouldConfigureStatusAndDates()
    {
        using var context = CreateContext();

        var entity = MembershipEntity(context);

        Assert.False(
            entity.FindProperty(
                nameof(
                    OrganizationMembership.Role))!
                .IsNullable);

        Assert.False(
            entity.FindProperty(
                nameof(
                    OrganizationMembership.Status))!
                .IsNullable);

        Assert.False(
            entity.FindProperty(
                nameof(
                    OrganizationMembership.JoinedAt))!
                .IsNullable);

        Assert.True(
            entity.FindProperty(
                nameof(
                    OrganizationMembership.SuspendedAt))!
                .IsNullable);
    }

    [Fact]
    public void DbContext_ShouldExposeSecuritySets()
    {
        using var context = CreateContext();

        Assert.NotNull(context.ApplicationUsers);

        Assert.NotNull(
            context.OrganizationMemberships);
    }

    [Fact]
    public void AddInfrastructure_ShouldRegisterIdentityUserStore()
    {
        var services = new ServiceCollection();

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        [
                            "ConnectionStrings:" +
                            "DefaultConnection"
                        ] =
                            "Host=localhost;" +
                            "Port=5432;" +
                            "Database=sipacul_tests;" +
                            "Username=sipacul;" +
                            "Password=sipacul"
                    })
                .Build();

        services.AddInfrastructure(configuration);

        var descriptor =
            services.Single(service =>
                service.ServiceType ==
                    typeof(
                        IUserStore<ApplicationUser>));

        Assert.Equal(
            ServiceLifetime.Scoped,
            descriptor.Lifetime);
    }

    private static IEntityType MembershipEntity(
        SiPaculDbContext context)
    {
        return context.Model.FindEntityType(
            typeof(OrganizationMembership))!;
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<
                SiPaculDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Port=5432;" +
                    "Database=sipacul_model_tests;" +
                    "Username=sipacul;" +
                    "Password=sipacul")
                .Options;

        return new SiPaculDbContext(options);
    }
}
