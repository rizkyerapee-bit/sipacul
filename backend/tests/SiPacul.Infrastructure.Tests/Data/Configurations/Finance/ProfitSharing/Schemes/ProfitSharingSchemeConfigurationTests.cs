using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance.ProfitSharing.Schemes;

public sealed class ProfitSharingSchemeConfigurationTests
{
    [Fact]
    public void SchemeAndChildren_ShouldUseExpectedTables()
    {
        using var context = CreateContext();

        Assert.Equal(
            "ProfitSharingSchemes",
            Entity<ProfitSharingScheme>(context).GetTableName());
        Assert.Equal(
            "ProfitSharingSchemeParticipants",
            Entity<ProfitSharingSchemeParticipant>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingSchemePriorityRules",
            Entity<ProfitSharingSchemePriorityRule>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingSchemeResidualShares",
            Entity<ProfitSharingSchemeResidualShare>(context)
                .GetTableName());
    }

    [Fact]
    public void Scheme_ShouldHaveVersionAndOpenStatusIndexes()
    {
        using var context = CreateContext();
        var entity = Entity<ProfitSharingScheme>(context);

        var versionIndex = entity.GetIndexes().Single(index =>
            index.GetDatabaseName() ==
                "UX_ProfitSharingSchemes_Org_Family_Version");

        Assert.True(versionIndex.IsUnique);

        var statusIndex = entity.GetIndexes().Single(index =>
            index.GetDatabaseName() ==
                "UX_ProfitSharingSchemes_Org_Family_OpenStatus");

        Assert.True(statusIndex.IsUnique);
        Assert.Contains(
            "Status",
            statusIndex.GetFilter(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void RateFields_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();

        AssertRatePrecision(
            Entity<ProfitSharingSchemePriorityRule>(context),
            nameof(
                ProfitSharingSchemePriorityRule.RateNumerator));
        AssertRatePrecision(
            Entity<ProfitSharingSchemePriorityRule>(context),
            nameof(
                ProfitSharingSchemePriorityRule.RateDenominator));
        AssertRatePrecision(
            Entity<ProfitSharingSchemeResidualShare>(context),
            nameof(
                ProfitSharingSchemeResidualShare.RateNumerator));
        AssertRatePrecision(
            Entity<ProfitSharingSchemeResidualShare>(context),
            nameof(
                ProfitSharingSchemeResidualShare.RateDenominator));
    }

    [Fact]
    public void ChildRelationships_ShouldCascadeFromScheme()
    {
        using var context = CreateContext();

        var childTypes = new[]
        {
            typeof(ProfitSharingSchemeParticipant),
            typeof(ProfitSharingSchemePriorityRule),
            typeof(ProfitSharingSchemeResidualShare)
        };

        foreach (var childType in childTypes)
        {
            var foreignKey = context.Model
                .FindEntityType(childType)!
                .GetForeignKeys()
                .Single(key =>
                    key.PrincipalEntityType.ClrType ==
                        typeof(ProfitSharingScheme));

            Assert.Equal(
                DeleteBehavior.Cascade,
                foreignKey.DeleteBehavior);
        }
    }

    private static IEntityType Entity<TEntity>(
        SiPaculDbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity))!;
    }

    private static void AssertRatePrecision(
        IEntityType entity,
        string propertyName)
    {
        var property = entity.FindProperty(propertyName)!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(8, property.GetScale());
        Assert.False(property.IsNullable);
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
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
