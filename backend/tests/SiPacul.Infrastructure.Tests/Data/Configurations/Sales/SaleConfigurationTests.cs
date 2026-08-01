using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Sales;

public sealed class SaleConfigurationTests
{
    [Fact]
    public void Sale_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale));

        Assert.NotNull(entityType);

        Assert.Equal(
            "Sales",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_Sales_OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(Sale.OrganizationId),
                nameof(Sale.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Sale_ShouldMapDateColumns()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        Assert.Equal(
            "date",
            entityType.FindProperty(
                nameof(Sale.SaleDate))!
                .GetColumnType());

        Assert.Equal(
            "date",
            entityType.FindProperty(
                nameof(Sale.DueDate))!
                .GetColumnType());
    }

    [Fact]
    public void Sale_ShouldMapMoneyPrecision()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        AssertPrecision(
            entityType,
            nameof(Sale.DiscountAmount),
            18,
            2);

        AssertPrecision(
            entityType,
            nameof(Sale.Subtotal),
            18,
            2);

        AssertPrecision(
            entityType,
            nameof(Sale.TotalAmount),
            18,
            2);
    }

    [Fact]
    public void Sale_ShouldMapExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        AssertLength(
            entityType,
            nameof(Sale.Code),
            Sale.MaxCodeLength);

        AssertLength(
            entityType,
            nameof(Sale.BuyerName),
            Sale.MaxBuyerNameLength);

        AssertLength(
            entityType,
            nameof(Sale.BuyerPhone),
            Sale.MaxBuyerPhoneLength);

        AssertLength(
            entityType,
            nameof(Sale.BuyerAddress),
            Sale.MaxBuyerAddressLength);

        AssertLength(
            entityType,
            nameof(Sale.CancellationReason),
            Sale.MaxCancellationReasonLength);

        AssertLength(
            entityType,
            nameof(Sale.Notes),
            Sale.MaxNotesLength);
    }

    [Fact]
    public void Sale_ShouldUseDraftDefaultAndIgnoreRevenue()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        var statusProperty =
            entityType.FindProperty(
                nameof(Sale.Status))!;

        Assert.Equal(
            SaleStatus.Draft,
            statusProperty.GetDefaultValue());

        Assert.Null(
            entityType.FindProperty(
                nameof(Sale.IsRevenue)));
    }

    [Fact]
    public void Sale_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        var indexes =
            entityType.GetIndexes()
                .ToDictionary(
                    index => index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_Sales_OrganizationId_Code"]
                .IsUnique);

        Assert.Contains(
            "IX_Sales_OrganizationId_SaleDate",
            indexes.Keys);

        Assert.Contains(
            "IX_Sales_OrganizationId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_Sales_OrganizationId_BuyerName",
            indexes.Keys);

        Assert.Contains(
            "IX_Sales_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Sale_ShouldReferenceOrganizationWithRestrict()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Sale),
            typeof(Organization),
            new[]
            {
                nameof(Sale.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            },
            DeleteBehavior.Restrict);
    }

    [Fact]
    public void Sale_ShouldOwnLinesWithScopedCascade()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(SaleLine),
            typeof(Sale),
            new[]
            {
                nameof(SaleLine.OrganizationId),
                nameof(SaleLine.SaleId)
            },
            new[]
            {
                nameof(Sale.OrganizationId),
                nameof(Sale.Id)
            },
            DeleteBehavior.Cascade);
    }

    [Fact]
    public void Sale_LinesNavigation_ShouldUseFieldAccess()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(typeof(Sale))!;

        var navigation =
            entityType.FindNavigation(
                nameof(Sale.Lines));

        Assert.NotNull(navigation);

        Assert.Equal(
            PropertyAccessMode.Field,
            navigation!.GetPropertyAccessMode());
    }

    [Fact]
    public void SaleLine_ShouldUseExpectedTableAndPrimaryKey()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SaleLine));

        Assert.NotNull(entityType);

        Assert.Equal(
            "SaleLines",
            entityType!.GetTableName());

        Assert.Equal(
            new[]
            {
                nameof(SaleLine.Id)
            },
            entityType.FindPrimaryKey()!
                .Properties
                .Select(property => property.Name));

        Assert.Equal(
            ValueGenerated.Never,
            entityType.FindProperty(
                nameof(SaleLine.Id))!
                .ValueGenerated);
    }

    [Fact]
    public void SaleLine_ShouldMapQuantityAndMoneyPrecision()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SaleLine))!;

        AssertPrecision(
            entityType,
            nameof(SaleLine.Quantity),
            18,
            4);

        AssertPrecision(
            entityType,
            nameof(SaleLine.UnitPrice),
            18,
            2);

        AssertPrecision(
            entityType,
            nameof(SaleLine.LineDiscount),
            18,
            2);

        AssertPrecision(
            entityType,
            nameof(SaleLine.LineTotal),
            18,
            2);
    }

    [Fact]
    public void SaleLine_ShouldMapExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SaleLine))!;

        AssertLength(
            entityType,
            nameof(SaleLine.HarvestBatchCodeSnapshot),
            SaleLine.MaxHarvestBatchCodeLength);

        AssertLength(
            entityType,
            nameof(SaleLine.CropCycleCodeSnapshot),
            SaleLine.MaxCropCycleCodeLength);

        AssertLength(
            entityType,
            nameof(SaleLine.CommodityCodeSnapshot),
            SaleLine.MaxCommodityCodeLength);

        AssertLength(
            entityType,
            nameof(SaleLine.CommodityNameSnapshot),
            SaleLine.MaxCommodityNameLength);

        AssertLength(
            entityType,
            nameof(SaleLine.QualityGradeSnapshot),
            SaleLine.MaxQualityGradeLength);

        AssertLength(
            entityType,
            nameof(SaleLine.Notes),
            SaleLine.MaxNotesLength);
    }

    [Fact]
    public void SaleLine_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType =
            context.Model.FindEntityType(
                typeof(SaleLine))!;

        var indexes =
            entityType.GetIndexes()
                .ToDictionary(
                    index => index.GetDatabaseName()!,
                    index => index);

        Assert.True(
            indexes[
                "UX_SaleLines_OrganizationId_" +
                "SaleId_HarvestBatchId"]
                .IsUnique);

        Assert.Contains(
            "IX_SaleLines_OrganizationId_SaleId",
            indexes.Keys);

        Assert.Contains(
            "IX_SaleLines_OrganizationId_" +
            "HarvestBatchId",
            indexes.Keys);
    }

    [Fact]
    public void SaleLine_ShouldReferenceOrganizationWithRestrict()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(SaleLine),
            typeof(Organization),
            new[]
            {
                nameof(SaleLine.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            },
            DeleteBehavior.Restrict);
    }

    [Fact]
    public void SaleLine_ShouldReferenceHarvestWithScopedRestrict()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(SaleLine),
            typeof(HarvestBatch),
            new[]
            {
                nameof(SaleLine.OrganizationId),
                nameof(SaleLine.HarvestBatchId)
            },
            new[]
            {
                nameof(HarvestBatch.OrganizationId),
                nameof(HarvestBatch.Id)
            },
            DeleteBehavior.Restrict);
    }

    [Fact]
    public void DbContext_ShouldExposeSalesAndSaleLines()
    {
        using var context = CreateContext();

        Assert.NotNull(context.Sales);
        Assert.NotNull(context.SaleLines);
    }

    private static void AssertPrecision(
        IEntityType entityType,
        string propertyName,
        int expectedPrecision,
        int expectedScale)
    {
        var property =
            entityType.FindProperty(propertyName)!;

        Assert.Equal(
            expectedPrecision,
            property.GetPrecision());

        Assert.Equal(
            expectedScale,
            property.GetScale());
    }

    private static void AssertLength(
        IEntityType entityType,
        string propertyName,
        int expectedLength)
    {
        var property =
            entityType.FindProperty(propertyName)!;

        Assert.Equal(
            expectedLength,
            property.GetMaxLength());
    }

    private static void AssertForeignKey(
        SiPaculDbContext context,
        Type dependentType,
        Type principalType,
        string[] expectedForeignKeyProperties,
        string[] expectedPrincipalKeyProperties,
        DeleteBehavior expectedDeleteBehavior)
    {
        var entityType =
            context.Model.FindEntityType(
                dependentType);

        Assert.NotNull(entityType);

        var foreignKey =
            entityType!.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                        principalType);

        Assert.Equal(
            expectedForeignKeyProperties,
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            expectedPrincipalKeyProperties,
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            expectedDeleteBehavior,
            foreignKey.DeleteBehavior);
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
