using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance.ProfitSharing.WaterfallSettlements;

public sealed class ProfitSharingWaterfallSettlementConfigurationTests
{
    [Fact]
    public void Settlement_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();
        var entityType = SettlementEntity(context);

        Assert.Equal(
            "ProfitSharingWaterfallSettlements",
            entityType.GetTableName());

        var alternateKey = entityType.GetKeys().Single(key =>
            !key.IsPrimaryKey() &&
            key.GetName() ==
                "AK_ProfitSharingWaterfallSettlements_Org_Id");

        Assert.Equal(
            new[]
            {
                nameof(ProfitSharingWaterfallSettlement.OrganizationId),
                nameof(ProfitSharingWaterfallSettlement.Id)
            },
            alternateKey.Properties.Select(property => property.Name));
    }

    [Fact]
    public void Children_ShouldUseExpectedTables()
    {
        using var context = CreateContext();

        Assert.Equal(
            "ProfitSharingWaterfallPriorityAllocations",
            Entity<ProfitSharingWaterfallPriorityAllocation>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingWaterfallParticipantAllocations",
            Entity<ProfitSharingWaterfallParticipantAllocation>(context)
                .GetTableName());
        Assert.Equal(
            "ProfitSharingWaterfallResidualShares",
            Entity<ProfitSharingWaterfallResidualShareSnapshot>(context)
                .GetTableName());
    }

    [Fact]
    public void SettlementDate_ShouldUsePostgreSqlDate()
    {
        using var context = CreateContext();

        var property = SettlementEntity(context).FindProperty(
            nameof(ProfitSharingWaterfallSettlement.SettlementDate));

        Assert.NotNull(property);
        Assert.Equal("date", property!.GetColumnType());
    }

    [Fact]
    public void SettlementMoney_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();
        var entityType = SettlementEntity(context);

        var propertyNames = new[]
        {
            nameof(ProfitSharingWaterfallSettlement.RecognizedRevenue),
            nameof(ProfitSharingWaterfallSettlement.CollectedRevenue),
            nameof(ProfitSharingWaterfallSettlement.OutstandingReceivable),
            nameof(ProfitSharingWaterfallSettlement.ActivityResourceCost),
            nameof(ProfitSharingWaterfallSettlement.ManualExpenseCost),
            nameof(ProfitSharingWaterfallSettlement.TotalCultivationCost),
            nameof(ProfitSharingWaterfallSettlement.NetProfit),
            nameof(ProfitSharingWaterfallSettlement.ConfirmedInvestorCapital),
            nameof(ProfitSharingWaterfallSettlement.ConfirmedPartnerCapital),
            nameof(ProfitSharingWaterfallSettlement.TotalConfirmedCapital),
            nameof(ProfitSharingWaterfallSettlement.TotalCapital),
            nameof(ProfitSharingWaterfallSettlement.TotalCapitalRecovery),
            nameof(ProfitSharingWaterfallSettlement.TotalCapitalLoss),
            nameof(ProfitSharingWaterfallSettlement.TotalManagementProfitShare),
            nameof(ProfitSharingWaterfallSettlement.TotalReturnOnCapitalProfitShare),
            nameof(ProfitSharingWaterfallSettlement.TotalPriorityProfitShare),
            nameof(ProfitSharingWaterfallSettlement.TotalResidualProfitShare),
            nameof(ProfitSharingWaterfallSettlement.TotalProfitShare),
            nameof(ProfitSharingWaterfallSettlement.TotalPayout)
        };

        foreach (var propertyName in propertyNames)
        {
            AssertPrecision(entityType, propertyName, 18, 2);
        }
    }

    [Fact]
    public void SettlementQuantity_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();

        AssertPrecision(
            SettlementEntity(context),
            nameof(
                ProfitSharingWaterfallSettlement.AvailableHarvestQuantity),
            18,
            4);
    }

    [Fact]
    public void ParticipantMoneyAndRatio_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();
        var entityType =
            Entity<ProfitSharingWaterfallParticipantAllocation>(context);

        var moneyPropertyNames = new[]
        {
            nameof(ProfitSharingWaterfallParticipantAllocation.ConfirmedCapital),
            nameof(ProfitSharingWaterfallParticipantAllocation.CapitalRecovery),
            nameof(ProfitSharingWaterfallParticipantAllocation.CapitalLoss),
            nameof(ProfitSharingWaterfallParticipantAllocation.ManagementProfitShare),
            nameof(ProfitSharingWaterfallParticipantAllocation.ReturnOnCapitalProfitShare),
            nameof(ProfitSharingWaterfallParticipantAllocation.ResidualProfitShare),
            nameof(ProfitSharingWaterfallParticipantAllocation.TotalProfitShare),
            nameof(ProfitSharingWaterfallParticipantAllocation.TotalPayout)
        };

        foreach (var propertyName in moneyPropertyNames)
        {
            AssertPrecision(entityType, propertyName, 18, 2);
        }

        AssertPrecision(
            entityType,
            nameof(ProfitSharingWaterfallParticipantAllocation.CapitalRatio),
            18,
            8);
    }

    [Fact]
    public void PriorityAndResidualRates_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();
        var priority =
            Entity<ProfitSharingWaterfallPriorityAllocation>(context);
        var residual =
            Entity<ProfitSharingWaterfallResidualShareSnapshot>(context);

        AssertPrecision(
            priority,
            nameof(ProfitSharingWaterfallPriorityAllocation.RateNumerator),
            18,
            8);
        AssertPrecision(
            priority,
            nameof(ProfitSharingWaterfallPriorityAllocation.RateDenominator),
            18,
            8);
        AssertPrecision(
            residual,
            nameof(ProfitSharingWaterfallResidualShareSnapshot.RateNumerator),
            18,
            8);
        AssertPrecision(
            residual,
            nameof(ProfitSharingWaterfallResidualShareSnapshot.RateDenominator),
            18,
            8);
    }

    [Fact]
    public void Settlement_ShouldUseExpectedTextLengths()
    {
        using var context = CreateContext();
        var entityType = SettlementEntity(context);

        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.Code),
            ProfitSharingWaterfallSettlement.MaxCodeLength);
        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.SchemeCodeSnapshot),
            ProfitSharingWaterfallSettlement.MaxSchemeCodeLength);
        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.SchemeNameSnapshot),
            ProfitSharingWaterfallSettlement.MaxSchemeNameLength);
        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.CalculationVersion),
            ProfitSharingWaterfallSettlement.MaxCalculationVersionLength);
        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.Notes),
            ProfitSharingWaterfallSettlement.MaxNotesLength);
        AssertLength(
            entityType,
            nameof(ProfitSharingWaterfallSettlement.VoidReason),
            ProfitSharingWaterfallSettlement.MaxVoidReasonLength);
    }

    [Fact]
    public void Settlement_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();
        var indexes = SettlementEntity(context)
            .GetIndexes()
            .ToDictionary(index => index.GetDatabaseName()!, index => index);

        Assert.True(
            indexes[
                "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Code"]
                .IsUnique);

        var activeIndex =
            indexes[
                "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Active"];

        Assert.True(activeIndex.IsUnique);
        Assert.Equal(
            "\"Status\" = 1 AND \"IsDeleted\" = false",
            activeIndex.GetFilter());
        Assert.Contains(
            "IX_ProfitSharingWaterfallSettlements_Org_Cycle_Status",
            indexes.Keys);
        Assert.Contains(
            "IX_ProfitSharingWaterfallSettlements_Org_Date",
            indexes.Keys);
    }

    [Fact]
    public void Children_ShouldUseUniqueSnapshotIndexes()
    {
        using var context = CreateContext();

        var priorityIndexes =
            Entity<ProfitSharingWaterfallPriorityAllocation>(context)
                .GetIndexes()
                .ToDictionary(
                    index => index.GetDatabaseName()!,
                    index => index);
        var participantIndexes =
            Entity<ProfitSharingWaterfallParticipantAllocation>(context)
                .GetIndexes()
                .ToDictionary(
                    index => index.GetDatabaseName()!,
                    index => index);
        var residualIndexes =
            Entity<ProfitSharingWaterfallResidualShareSnapshot>(context)
                .GetIndexes()
                .ToDictionary(
                    index => index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            priorityIndexes[
                "UX_PSWaterfallPriorityAlloc_Settlement_Rule"]
                .IsUnique);
        Assert.True(
            priorityIndexes[
                "UX_PSWaterfallPriorityAlloc_Settlement_Sequence"]
                .IsUnique);
        Assert.True(
            participantIndexes[
                "UX_PSWaterfallParticipantAlloc_Settlement_Participant"]
                .IsUnique);
        Assert.True(
            participantIndexes[
                "UX_PSWaterfallParticipantAlloc_Settlement_Sequence"]
                .IsUnique);
        Assert.True(
            residualIndexes[
                "UX_ProfitSharingWaterfallResidualShares_" +
                "Settlement_Recipient"]
                .IsUnique);
        Assert.True(
            residualIndexes[
                "UX_ProfitSharingWaterfallResidualShares_" +
                "Settlement_Sequence"]
                .IsUnique);
    }

    [Fact]
    public void Settlement_ShouldReferenceImmutableSources()
    {
        using var context = CreateContext();
        var foreignKeys = SettlementEntity(context).GetForeignKeys().ToArray();

        Assert.Contains(
            foreignKeys,
            key =>
                key.PrincipalEntityType.ClrType == typeof(Organization) &&
                key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            foreignKeys,
            key =>
                key.PrincipalEntityType.ClrType == typeof(CropCycle) &&
                key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            foreignKeys,
            key =>
                key.PrincipalEntityType.ClrType ==
                    typeof(ProfitSharingSchemeAssignment) &&
                key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            foreignKeys,
            key =>
                key.PrincipalEntityType.ClrType ==
                    typeof(ProfitSharingScheme) &&
                key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Children_ShouldCascadeOnlyFromSettlement()
    {
        using var context = CreateContext();

        var childTypes = new[]
        {
            typeof(ProfitSharingWaterfallPriorityAllocation),
            typeof(ProfitSharingWaterfallParticipantAllocation),
            typeof(ProfitSharingWaterfallResidualShareSnapshot)
        };

        foreach (var childType in childTypes)
        {
            var foreignKey = context.Model.FindEntityType(childType)!
                .GetForeignKeys()
                .Single();

            Assert.Equal(
                typeof(ProfitSharingWaterfallSettlement),
                foreignKey.PrincipalEntityType.ClrType);
            Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
        }
    }

    [Fact]
    public void ComputedActiveState_ShouldNotBeMapped()
    {
        using var context = CreateContext();

        Assert.Null(
            SettlementEntity(context).FindProperty(
                nameof(ProfitSharingWaterfallSettlement.IsActive)));
    }

    private static IEntityType SettlementEntity(
        SiPaculDbContext context)
    {
        return Entity<ProfitSharingWaterfallSettlement>(context);
    }

    private static IEntityType Entity<TEntity>(
        SiPaculDbContext context)
    {
        return context.Model.FindEntityType(typeof(TEntity))!;
    }

    private static void AssertPrecision(
        IEntityType entityType,
        string propertyName,
        int precision,
        int scale)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(precision, property!.GetPrecision());
        Assert.Equal(scale, property.GetScale());
    }

    private static void AssertLength(
        IEntityType entityType,
        string propertyName,
        int maximumLength)
    {
        var property = entityType.FindProperty(propertyName);

        Assert.NotNull(property);
        Assert.Equal(maximumLength, property!.GetMaxLength());
    }

    private static SiPaculDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SiPaculDbContext>()
            .UseNpgsql(
                "Host=localhost;" +
                "Port=5432;" +
                "Database=sipacul_waterfall_model_tests;" +
                "Username=sipacul;" +
                "Password=sipacul")
            .Options;

        return new SiPaculDbContext(options);
    }
}
