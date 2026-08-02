using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Finance.ProfitSharing;

public sealed class
    ProfitSharingSettlementConfigurationTests
{
    [Fact]
    public void Settlement_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(ProfitSharingSettlement));

        Assert.NotNull(entityType);
        Assert.Equal(
            "ProfitSharingSettlements",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_ProfitSharingSettlements_Org_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    ProfitSharingSettlement.OrganizationId),
                nameof(ProfitSharingSettlement.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Allocation_ShouldUseExpectedTableAndKeys()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(ProfitSharingAllocation));

        Assert.NotNull(entityType);
        Assert.Equal(
            "ProfitSharingAllocations",
            entityType!.GetTableName());

        Assert.Equal(
            nameof(ProfitSharingAllocation.Id),
            entityType.FindPrimaryKey()!
                .Properties.Single().Name);

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_ProfitSharingAllocations_Org_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    ProfitSharingAllocation.OrganizationId),
                nameof(ProfitSharingAllocation.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void SettlementDate_ShouldUsePostgreSqlDate()
    {
        using var context = CreateContext();

        var property =
            SettlementEntity(context)
                .FindProperty(
                    nameof(
                        ProfitSharingSettlement
                            .SettlementDate))!;

        Assert.Equal(
            "date",
            property.GetColumnType());
    }

    [Fact]
    public void SettlementMoney_ShouldUseMoneyPrecision()
    {
        using var context = CreateContext();

        var entityType = SettlementEntity(context);

        var propertyNames = new[]
        {
            nameof(
                ProfitSharingSettlement.RecognizedRevenue),
            nameof(
                ProfitSharingSettlement.CollectedRevenue),
            nameof(
                ProfitSharingSettlement.ActivityResourceCost),
            nameof(
                ProfitSharingSettlement.ManualExpenseCost),
            nameof(
                ProfitSharingSettlement.TotalCultivationCost),
            nameof(ProfitSharingSettlement.NetProfit),
            nameof(
                ProfitSharingSettlement.ManagementProfitPool),
            nameof(
                ProfitSharingSettlement.CapitalProfitPool),
            nameof(
                ProfitSharingSettlement.TotalInvestorCapital),
            nameof(
                ProfitSharingSettlement.TotalPartnerCapital),
            nameof(
                ProfitSharingSettlement.TotalCapital),
            nameof(
                ProfitSharingSettlement.TotalCapitalRecovery),
            nameof(
                ProfitSharingSettlement.TotalCapitalLoss),
            nameof(
                ProfitSharingSettlement
                    .TotalInvestorProfitShare),
            nameof(
                ProfitSharingSettlement
                    .TotalPartnerProfitShare),
            nameof(
                ProfitSharingSettlement.TotalPayout)
        };

        foreach (var propertyName in propertyNames)
        {
            AssertPrecision(
                entityType,
                propertyName,
                18,
                2);
        }
    }

    [Fact]
    public void AllocationMoneyAndRatio_ShouldUseExpectedPrecision()
    {
        using var context = CreateContext();

        var entityType = AllocationEntity(context);

        var moneyPropertyNames = new[]
        {
            nameof(
                ProfitSharingAllocation.ConfirmedCapital),
            nameof(
                ProfitSharingAllocation.CapitalRecovery),
            nameof(
                ProfitSharingAllocation.CapitalLoss),
            nameof(
                ProfitSharingAllocation
                    .ManagementProfitShare),
            nameof(
                ProfitSharingAllocation.CapitalProfitShare),
            nameof(
                ProfitSharingAllocation.TotalProfitShare),
            nameof(
                ProfitSharingAllocation.TotalPayout)
        };

        foreach (var propertyName in moneyPropertyNames)
        {
            AssertPrecision(
                entityType,
                propertyName,
                18,
                2);
        }

        AssertPrecision(
            entityType,
            nameof(
                ProfitSharingAllocation.CapitalRatio),
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
            nameof(ProfitSharingSettlement.Code),
            ProfitSharingSettlement.MaxCodeLength);

        AssertLength(
            entityType,
            nameof(
                ProfitSharingSettlement
                    .ManagingPartnerCode),
            ProfitSharingSettlement
                .MaxManagingPartnerCodeLength);

        AssertLength(
            entityType,
            nameof(
                ProfitSharingSettlement
                    .ManagingPartnerName),
            ProfitSharingSettlement
                .MaxManagingPartnerNameLength);

        AssertLength(
            entityType,
            nameof(
                ProfitSharingSettlement
                    .CalculationVersion),
            ProfitSharingSettlement
                .MaxCalculationVersionLength);

        AssertLength(
            entityType,
            nameof(ProfitSharingSettlement.VoidReason),
            ProfitSharingSettlement.MaxVoidReasonLength);

        AssertLength(
            entityType,
            nameof(ProfitSharingSettlement.Notes),
            ProfitSharingSettlement.MaxNotesLength);
    }

    [Fact]
    public void Allocation_ShouldUseExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType = AllocationEntity(context);

        AssertLength(
            entityType,
            nameof(
                ProfitSharingAllocation
                    .ContributorCodeSnapshot),
            ProfitSharingAllocation
                .MaxContributorCodeLength);

        AssertLength(
            entityType,
            nameof(
                ProfitSharingAllocation
                    .ContributorNameSnapshot),
            ProfitSharingAllocation
                .MaxContributorNameLength);
    }

    [Fact]
    public void ComputedProperties_ShouldNotBeMapped()
    {
        using var context = CreateContext();

        var entityType = SettlementEntity(context);

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    ProfitSharingSettlement
                        .OutstandingReceivable)));

        Assert.Null(
            entityType.FindProperty(
                nameof(
                    ProfitSharingSettlement.IsActive)));
    }

    [Fact]
    public void Settlement_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var indexes =
            SettlementEntity(context)
                .GetIndexes()
                .ToDictionary(
                    index =>
                        index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_ProfitSharingSettlements_" +
                "Org_Cycle_Code"]
                .IsUnique);

        var activeIndex =
            indexes[
                "UX_ProfitSharingSettlements_" +
                "Org_Cycle_Active"];

        Assert.True(activeIndex.IsUnique);

        Assert.Equal(
            "\"Status\" = 2 AND \"IsDeleted\" = false",
            activeIndex.GetFilter());

        Assert.Contains(
            "IX_ProfitSharingSettlements_" +
            "Org_Cycle_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_ProfitSharingSettlements_Org_Date",
            indexes.Keys);

        Assert.Contains(
            "IX_ProfitSharingSettlements_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Allocation_ShouldUseExpectedUniqueIndexes()
    {
        using var context = CreateContext();

        var indexes =
            AllocationEntity(context)
                .GetIndexes()
                .ToDictionary(
                    index =>
                        index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_ProfitSharingAllocations_" +
                "Settlement_Contributor"]
                .IsUnique);

        Assert.True(
            indexes[
                "UX_ProfitSharingAllocations_" +
                "Settlement_Sequence"]
                .IsUnique);

        Assert.Contains(
            "IX_ProfitSharingAllocations_" +
            "Org_Contributor",
            indexes.Keys);
    }

    [Fact]
    public void Settlement_ShouldReferenceOrganizationAndCropCycle()
    {
        using var context = CreateContext();

        var entityType = SettlementEntity(context);

        var organizationForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                        typeof(Organization));

        Assert.Equal(
            new[]
            {
                nameof(
                    ProfitSharingSettlement.OrganizationId)
            },
            organizationForeignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            organizationForeignKey.DeleteBehavior);

        var cropCycleForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                        typeof(CropCycle));

        Assert.Equal(
            new[]
            {
                nameof(
                    ProfitSharingSettlement.OrganizationId),
                nameof(
                    ProfitSharingSettlement.CropCycleId)
            },
            cropCycleForeignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            },
            cropCycleForeignKey.PrincipalKey
                .Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            cropCycleForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Allocation_ShouldReferenceSettlementAndOrganization()
    {
        using var context = CreateContext();

        var entityType = AllocationEntity(context);

        var settlementForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                        typeof(ProfitSharingSettlement));

        Assert.Equal(
            new[]
            {
                nameof(
                    ProfitSharingAllocation.OrganizationId),
                nameof(
                    ProfitSharingAllocation
                        .ProfitSharingSettlementId)
            },
            settlementForeignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Cascade,
            settlementForeignKey.DeleteBehavior);

        var organizationForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType.ClrType ==
                        typeof(Organization));

        Assert.Equal(
            DeleteBehavior.Restrict,
            organizationForeignKey.DeleteBehavior);
    }

    [Fact]
    public void AllocationNavigation_ShouldUseFieldAccess()
    {
        using var context = CreateContext();

        var navigation =
            SettlementEntity(context)
                .FindNavigation(
                    nameof(
                        ProfitSharingSettlement.Allocations));

        Assert.NotNull(navigation);

        Assert.Equal(
            PropertyAccessMode.Field,
            navigation!.GetPropertyAccessMode());
    }

    [Fact]
    public void DbContext_ShouldExposeSettlementSets()
    {
        using var context = CreateContext();

        Assert.NotNull(
            context.ProfitSharingSettlements);

        Assert.NotNull(
            context.ProfitSharingAllocations);
    }

    private static IEntityType SettlementEntity(
        SiPaculDbContext context)
    {
        return context.Model.FindEntityType(
            typeof(ProfitSharingSettlement))!;
    }

    private static IEntityType AllocationEntity(
        SiPaculDbContext context)
    {
        return context.Model.FindEntityType(
            typeof(ProfitSharingAllocation))!;
    }

    private static void AssertLength(
        IEntityType entityType,
        string propertyName,
        int expectedLength)
    {
        var property =
            entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        Assert.Equal(
            expectedLength,
            property!.GetMaxLength());
    }

    private static void AssertPrecision(
        IEntityType entityType,
        string propertyName,
        int expectedPrecision,
        int expectedScale)
    {
        var property =
            entityType.FindProperty(propertyName);

        Assert.NotNull(property);

        Assert.Equal(
            expectedPrecision,
            property!.GetPrecision());

        Assert.Equal(
            expectedScale,
            property.GetScale());

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
