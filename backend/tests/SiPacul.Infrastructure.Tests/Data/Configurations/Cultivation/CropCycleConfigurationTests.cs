using Microsoft.EntityFrameworkCore;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Cultivation;

public sealed class CropCycleConfigurationTests
{
    [Fact]
    public void CropCycle_ShouldUseExpectedTableAndAlternateKey()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CropCycle));

        Assert.NotNull(entityType);
        Assert.Equal(
            "CropCycles",
            entityType!.GetTableName());

        var alternateKey = entityType
            .GetKeys()
            .Single(key =>
                !key.IsPrimaryKey() &&
                key.GetName() ==
                    "AK_CropCycles_OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void CropCycle_ShouldUseExpectedIndexes()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CropCycle));

        Assert.NotNull(entityType);

        var indexes = entityType!
            .GetIndexes()
            .ToDictionary(
                index => index.GetDatabaseName()!,
                index => index);

        Assert.True(
            indexes[
                "UX_CropCycles_OrganizationId_Code"]
            .IsUnique);

        Assert.Contains(
            "Status",
            indexes[
                "UX_CropCycles_" +
                "OrganizationId_LandId_" +
                "LandPlotId_InProgress"]
            .GetFilter());

        Assert.Contains(
            "IsDeleted",
            indexes[
                "UX_CropCycles_" +
                "OrganizationId_LandId_" +
                "LandPlotId_InProgress"]
            .GetFilter());

        Assert.Contains(
            "IX_CropCycles_OrganizationId_Status",
            indexes.Keys);

        Assert.Contains(
            "IX_CropCycles_" +
            "OrganizationId_CommodityId",
            indexes.Keys);

        Assert.Contains(
            "IX_CropCycles_" +
            "OrganizationId_LandId_LandPlotId",
            indexes.Keys);

        Assert.Contains(
            "IX_CropCycles_" +
            "OrganizationId_PlannedDateRange",
            indexes.Keys);
    }

    [Fact]
    public void CropCycle_ShouldMapAreaAndDates()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CropCycle));

        Assert.NotNull(entityType);

        var plantedArea = entityType!.FindProperty(
            nameof(CropCycle.PlantedArea));

        Assert.NotNull(plantedArea);
        Assert.Equal(18, plantedArea!.GetPrecision());
        Assert.Equal(4, plantedArea.GetScale());

        var dateProperties = new[]
        {
            nameof(CropCycle.PlannedStartDate),
            nameof(CropCycle.ExpectedHarvestDate),
            nameof(CropCycle.ActualStartDate),
            nameof(CropCycle.ActualHarvestDate)
        };

        foreach (var propertyName in dateProperties)
        {
            var property = entityType.FindProperty(
                propertyName);

            Assert.NotNull(property);
            Assert.Equal(
                "date",
                property!.GetColumnType());
        }
    }

    [Fact]
    public void CropCycle_ShouldReferenceOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Organization),
            new[]
            {
                nameof(CropCycle.OrganizationId)
            },
            new[]
            {
                nameof(Organization.Id)
            });
    }

    [Fact]
    public void CropCycle_ShouldReferenceCommodityByOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Commodity),
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.CommodityId)
            },
            new[]
            {
                nameof(Commodity.OrganizationId),
                nameof(Commodity.Id)
            });
    }

    [Fact]
    public void CropCycle_ShouldReferenceOptionalSopByOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(CultivationSop),
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.CultivationSopId)
            },
            new[]
            {
                nameof(CultivationSop.OrganizationId),
                nameof(CultivationSop.Id)
            });
    }

    [Fact]
    public void CropCycle_ShouldReferenceLandByOrganization()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(Land),
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.LandId)
            },
            new[]
            {
                nameof(Land.OrganizationId),
                nameof(Land.Id)
            });
    }

    [Fact]
    public void CropCycle_ShouldReferencePlotByOrganizationAndLand()
    {
        using var context = CreateContext();

        AssertForeignKey(
            context,
            typeof(LandPlot),
            new[]
            {
                nameof(CropCycle.OrganizationId),
                nameof(CropCycle.LandId),
                nameof(CropCycle.LandPlotId)
            },
            new[]
            {
                nameof(LandPlot.OrganizationId),
                nameof(LandPlot.LandId),
                nameof(LandPlot.Id)
            });
    }

    [Fact]
    public void LandPlot_ShouldExposeOrganizationLandAlternateKey()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(LandPlot));

        Assert.NotNull(entityType);

        var alternateKey = entityType!
            .GetKeys()
            .Single(key =>
                !key.IsPrimaryKey() &&
                key.GetName() ==
                    "AK_LandPlots_" +
                    "OrganizationId_LandId_Id");

        Assert.Equal(
            new[]
            {
                nameof(LandPlot.OrganizationId),
                nameof(LandPlot.LandId),
                nameof(LandPlot.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    private static void AssertForeignKey(
        SiPaculDbContext context,
        Type principalType,
        string[] expectedForeignKeyProperties,
        string[] expectedPrincipalKeyProperties)
    {
        var entityType = context.Model.FindEntityType(
            typeof(CropCycle));

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
