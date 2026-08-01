using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Harvests;

public sealed class HarvestBatchConfigurationTests
{
    [Fact]
    public void Batch_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(HarvestBatch));

        Assert.NotNull(entityType);

        Assert.Equal(
            "HarvestBatches",
            entityType!.GetTableName());

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    !key.IsPrimaryKey() &&
                    key.GetName() ==
                        "AK_HarvestBatches_" +
                        "OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(HarvestBatch.OrganizationId),
                nameof(HarvestBatch.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Batch_ShouldMapDateAndQuantityPrecision()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(HarvestBatch))!;

        Assert.Equal(
            "date",
            entityType.FindProperty(
                nameof(HarvestBatch.HarvestDate))!
                .GetColumnType());

        AssertPrecision(
            entityType,
            nameof(HarvestBatch.GrossQuantity));

        AssertPrecision(
            entityType,
            nameof(HarvestBatch.RejectedQuantity));

        AssertPrecision(
            entityType,
            nameof(HarvestBatch.NetQuantity));
    }

    [Fact]
    public void Batch_ShouldMapExpectedTextLengths()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(HarvestBatch))!;

        Assert.Equal(
            HarvestBatch.MaxCodeLength,
            entityType.FindProperty(
                nameof(HarvestBatch.Code))!
                .GetMaxLength());

        Assert.Equal(
            HarvestBatch.MaxQualityGradeLength,
            entityType.FindProperty(
                nameof(HarvestBatch.QualityGrade))!
                .GetMaxLength());

        Assert.Equal(
            HarvestBatch.MaxStorageLocationLength,
            entityType.FindProperty(
                nameof(HarvestBatch.StorageLocation))!
                .GetMaxLength());

        Assert.Equal(
            HarvestBatch.MaxNotesLength,
            entityType.FindProperty(
                nameof(HarvestBatch.Notes))!
                .GetMaxLength());

        Assert.Equal(
            HarvestBatch.MaxCancellationReasonLength,
            entityType.FindProperty(
                nameof(HarvestBatch.CancellationReason))!
                .GetMaxLength());
    }

    [Fact]
    public void Batch_ShouldUseDraftDatabaseDefault()
    {
        using var context = CreateContext();

        var property = context.Model
            .FindEntityType(typeof(HarvestBatch))!
            .FindProperty(nameof(HarvestBatch.Status))!;

        Assert.Equal(
            HarvestBatchStatus.Draft,
            property.GetDefaultValue());
    }

    [Fact]
    public void Batch_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(HarvestBatch))!;

        var indexes = entityType.GetIndexes()
            .ToDictionary(
                index => index.GetDatabaseName()!,
                index => index);

        Assert.True(
            indexes[
                "UX_HarvestBatches_" +
                "OrganizationId_CropCycleId_Code"]
                .IsUnique);

        Assert.Contains(
            "IX_HarvestBatches_" +
            "OrganizationId_CropCycleId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_HarvestBatches_" +
            "OrganizationId_HarvestDate",
            indexes.Keys);

        Assert.Contains(
            "IX_HarvestBatches_OrganizationId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_HarvestBatches_IsDeleted",
            indexes.Keys);
    }

    [Fact]
    public void Batch_ShouldUseRestrictiveOrganizationForeignKey()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(HarvestBatch.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void Batch_ShouldUseOrganizationScopedCropCycleForeignKey()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CropCycle),
            new[]
            {
                nameof(HarvestBatch.OrganizationId),
                nameof(HarvestBatch.CropCycleId)
            },
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            });
    }

    [Fact]
    public void DbContext_ShouldExposeHarvestBatches()
    {
        using var context = CreateContext();

        Assert.NotNull(context.HarvestBatches);
    }

    private static void AssertPrecision(
        IEntityType entityType,
        string propertyName)
    {
        var property =
            entityType.FindProperty(propertyName)!;

        Assert.Equal(18, property.GetPrecision());
        Assert.Equal(4, property.GetScale());
    }

    private static void AssertForeignKey(
        SiPaculDbContext context,
        Type principalType,
        string[] expectedForeignKeyProperties,
        string[] expectedPrincipalKeyProperties)
    {
        var entityType = context.Model.FindEntityType(
            typeof(HarvestBatch));

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
            DeleteBehavior.Restrict,
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
