using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Cultivation;

public sealed class CultivationActivityConfigurationTests
{
    [Fact]
    public void Activity_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivity));

        Assert.NotNull(entityType);
        Assert.Equal(
            "CultivationActivities",
            entityType!.GetTableName());

        var alternateKey = entityType
            .GetKeys()
            .Single(key =>
                !key.IsPrimaryKey() &&
                key.GetName() ==
                    "AK_CultivationActivities_" +
                    "OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    CultivationActivity.OrganizationId),
                nameof(CultivationActivity.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Resource_ShouldUseExpectedTable()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivityResource));

        Assert.NotNull(entityType);
        Assert.Equal(
            "CultivationActivityResources",
            entityType!.GetTableName());
    }

    [Fact]
    public void ActivityResourcesNavigation_ShouldUseFieldAccess()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivity))!;

        var navigation = entityType.FindNavigation(
            nameof(CultivationActivity.Resources));

        Assert.NotNull(navigation);
        Assert.Equal(
            PropertyAccessMode.Field,
            navigation!.GetPropertyAccessMode());
    }

    [Fact]
    public void Activity_ShouldMapLengthsAndDates()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivity))!;

        Assert.Equal(
            CultivationActivity.MaxCodeLength,
            entityType.FindProperty(
                nameof(CultivationActivity.Code))!
                .GetMaxLength());

        Assert.Equal(
            CultivationActivity.MaxNameLength,
            entityType.FindProperty(
                nameof(CultivationActivity.Name))!
                .GetMaxLength());

        Assert.Equal(
            CultivationActivity.MaxSopStepNameLength,
            entityType.FindProperty(
                nameof(
                    CultivationActivity
                        .SopStepNameSnapshot))!
                .GetMaxLength());

        var dateProperties = new[]
        {
            nameof(CultivationActivity.PlannedDate),
            nameof(CultivationActivity.ActualStartDate),
            nameof(
                CultivationActivity
                    .ActualCompletionDate)
        };

        foreach (var propertyName in dateProperties)
        {
            Assert.Equal(
                "date",
                entityType.FindProperty(
                    propertyName)!
                    .GetColumnType());
        }
    }

    [Fact]
    public void Activity_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivity))!;

        var indexes = entityType.GetIndexes()
            .ToDictionary(
                index => index.GetDatabaseName()!,
                index => index);

        Assert.True(
            indexes[
                "UX_CultivationActivities_" +
                "OrganizationId_CropCycleId_Code"]
            .IsUnique);

        Assert.Contains(
            "IX_CultivationActivities_" +
            "OrganizationId_CropCycleId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationActivities_" +
            "OrganizationId_PlannedDate",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationActivities_" +
            "OrganizationId_ActivityType",
            indexes.Keys);

        Assert.Contains(
            "IX_CultivationActivities_" +
            "OrganizationId_CultivationSopStepId",
            indexes.Keys);
    }

    [Fact]
    public void Activity_ShouldReferenceOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(
                    CultivationActivity.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void Activity_ShouldReferenceCropCycleByOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CropCycle),
            new[]
            {
                nameof(
                    CultivationActivity.OrganizationId),
                nameof(
                    CultivationActivity.CropCycleId)
            },
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            });
    }

    [Fact]
    public void Activity_ShouldReferenceOptionalSopByOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CultivationSop),
            new[]
            {
                nameof(
                    CultivationActivity.OrganizationId),
                nameof(
                    CultivationActivity
                        .CultivationSopId)
            },
            new[]
            {
                nameof(CultivationSop.OrganizationId),
                nameof(CultivationSop.Id)
            });
    }

    [Fact]
    public void Activity_ShouldReferenceOptionalSopStepByOrganizationAndSop()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CultivationSopStep),
            new[]
            {
                nameof(
                    CultivationActivity.OrganizationId),
                nameof(
                    CultivationActivity
                        .CultivationSopId),
                nameof(
                    CultivationActivity
                        .CultivationSopStepId)
            },
            new[]
            {
                nameof(
                    CultivationSopStep.OrganizationId),
                nameof(
                    CultivationSopStep
                        .CultivationSopId),
                nameof(CultivationSopStep.Id)
            });
    }

    [Fact]
    public void SopStep_ShouldExposeOrganizationSopAlternateKey()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationSopStep))!;

        var alternateKey = entityType.GetKeys()
            .Single(key =>
                !key.IsPrimaryKey() &&
                key.GetName() ==
                    "AK_CultivationSopSteps_" +
                    "OrganizationId_CultivationSopId_Id");

        Assert.Equal(
            new[]
            {
                nameof(
                    CultivationSopStep.OrganizationId),
                nameof(
                    CultivationSopStep
                        .CultivationSopId),
                nameof(CultivationSopStep.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Resource_ShouldMapLengthsAndPrecision()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivityResource))!;

        Assert.Equal(
            CultivationActivityResource
                .MaxDescriptionLength,
            entityType.FindProperty(
                nameof(
                    CultivationActivityResource
                        .Description))!
                .GetMaxLength());

        Assert.Equal(
            CultivationActivityResource.MaxUnitLength,
            entityType.FindProperty(
                nameof(
                    CultivationActivityResource.Unit))!
                .GetMaxLength());

        var quantity = entityType.FindProperty(
            nameof(
                CultivationActivityResource.Quantity))!;

        var unitCost = entityType.FindProperty(
            nameof(
                CultivationActivityResource.UnitCost))!;

        var totalCost = entityType.FindProperty(
            nameof(
                CultivationActivityResource.TotalCost))!;

        Assert.Equal(18, quantity.GetPrecision());
        Assert.Equal(4, quantity.GetScale());

        Assert.Equal(18, unitCost.GetPrecision());
        Assert.Equal(2, unitCost.GetScale());

        Assert.Equal(18, totalCost.GetPrecision());
        Assert.Equal(2, totalCost.GetScale());
    }

    [Fact]
    public void Resource_ShouldUseExpectedIndexesAndForeignKeys()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivityResource))!;

        var indexNames = entityType.GetIndexes()
            .Select(index =>
                index.GetDatabaseName())
            .ToArray();

        Assert.Contains(
            "IX_CultivationActivityResources_" +
            "OrganizationId_CultivationActivityId",
            indexNames);

        Assert.Contains(
            "IX_CultivationActivityResources_" +
            "OrganizationId_ResourceType",
            indexNames);

        var organizationForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType
                        .ClrType ==
                    typeof(Organization));

        Assert.Equal(
            DeleteBehavior.Restrict,
            organizationForeignKey.DeleteBehavior);

        var activityForeignKey =
            entityType.GetForeignKeys()
                .Single(foreignKey =>
                    foreignKey.PrincipalEntityType
                        .ClrType ==
                    typeof(CultivationActivity));

        Assert.Equal(
            new[]
            {
                nameof(
                    CultivationActivityResource
                        .OrganizationId),
                nameof(
                    CultivationActivityResource
                        .CultivationActivityId)
            },
            activityForeignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Cascade,
            activityForeignKey.DeleteBehavior);
    }

    private static void AssertForeignKey(
        SiPaculDbContext context,
        Type principalType,
        string[] expectedForeignKeyProperties,
        string[] expectedPrincipalKeyProperties)
    {
        var entityType = context.Model.FindEntityType(
            typeof(CultivationActivity));

        Assert.NotNull(entityType);

        var foreignKey = entityType!
            .GetForeignKeys()
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
