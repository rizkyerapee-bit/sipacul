using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance.ProfitSharing.Assignments;

public sealed class
    ProfitSharingSchemeAssignmentConfigurationTests
{
    [Fact]
    public void AssignmentAndChildren_ShouldUseExpectedTables()
    {
        using var context = CreateContext();

        Assert.Equal(
            "ProfitSharingSchemeAssignments",
            Entity<ProfitSharingSchemeAssignment>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingSchemeAssignmentParticipants",
            Entity<ProfitSharingSchemeAssignmentParticipant>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingSchemeAssignmentPriorityRules",
            Entity<ProfitSharingSchemeAssignmentPriorityRule>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingSchemeAssignmentResidualShares",
            Entity<ProfitSharingSchemeAssignmentResidualShare>(context)
                .GetTableName());
    }

    [Fact]
    public void Assignment_ShouldBeUniquePerCropCycle()
    {
        using var context = CreateContext();
        var entity =
            Entity<ProfitSharingSchemeAssignment>(context);

        var index = entity.GetIndexes().Single(candidate =>
            candidate.GetDatabaseName() ==
                "UX_ProfitSharingSchemeAssignments_Org_Cycle");

        Assert.True(index.IsUnique);
        Assert.Contains(
            "IsDeleted",
            index.GetFilter(),
            StringComparison.Ordinal);
        Assert.Equal(
            new[]
            {
                nameof(ProfitSharingSchemeAssignment.OrganizationId),
                nameof(ProfitSharingSchemeAssignment.CropCycleId)
            },
            index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Assignment_ShouldReferenceCycleAndSourceScheme()
    {
        using var context = CreateContext();
        var entity =
            Entity<ProfitSharingSchemeAssignment>(context);

        var principalTypes = entity.GetForeignKeys()
            .Select(key => key.PrincipalEntityType.ClrType)
            .ToArray();

        Assert.Contains(typeof(CropCycle), principalTypes);
        Assert.Contains(typeof(ProfitSharingScheme), principalTypes);
    }

    [Fact]
    public void SnapshotChildren_ShouldCascadeAndPreserveRatePrecision()
    {
        using var context = CreateContext();

        var rule = Entity<
            ProfitSharingSchemeAssignmentPriorityRule>(context);
        var share = Entity<
            ProfitSharingSchemeAssignmentResidualShare>(context);

        AssertRatePrecision(
            rule,
            nameof(
                ProfitSharingSchemeAssignmentPriorityRule
                    .RateNumerator));
        AssertRatePrecision(
            rule,
            nameof(
                ProfitSharingSchemeAssignmentPriorityRule
                    .RateDenominator));
        AssertRatePrecision(
            share,
            nameof(
                ProfitSharingSchemeAssignmentResidualShare
                    .RateNumerator));
        AssertRatePrecision(
            share,
            nameof(
                ProfitSharingSchemeAssignmentResidualShare
                    .RateDenominator));

        foreach (var childType in new[]
        {
            typeof(ProfitSharingSchemeAssignmentParticipant),
            typeof(ProfitSharingSchemeAssignmentPriorityRule),
            typeof(ProfitSharingSchemeAssignmentResidualShare)
        })
        {
            var foreignKey = context.Model
                .FindEntityType(childType)!
                .GetForeignKeys()
                .Single(key =>
                    key.PrincipalEntityType.ClrType ==
                        typeof(ProfitSharingSchemeAssignment));

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
