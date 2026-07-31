using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Lands;

public sealed class LandConfigurationTests
{
    [Fact]
    public void LandAndPlot_ShouldMapToExpectedTables()
    {
        using var dbContext = CreateDbContext();

        var landEntity =
            dbContext.Model.FindEntityType(
                typeof(Land));

        var plotEntity =
            dbContext.Model.FindEntityType(
                typeof(LandPlot));

        Assert.NotNull(landEntity);
        Assert.NotNull(plotEntity);

        Assert.Equal(
            "Lands",
            landEntity!.GetTableName());

        Assert.Equal(
            "LandPlots",
            plotEntity!.GetTableName());
    }

    [Fact]
    public void Land_ShouldHaveOrganizationScopedAlternateKey()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(Land))!;

        var alternateKey =
            entityType.GetKeys()
                .Single(key =>
                    key.GetName() ==
                    "AK_Lands_OrganizationId_Id");

        Assert.Equal(
            new[]
            {
                nameof(Land.OrganizationId),
                nameof(Land.Id)
            },
            alternateKey.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void LandCode_ShouldBeUniqueWithinOrganization()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(Land))!;

        var index =
            entityType.GetIndexes()
                .Single(candidate =>
                    candidate.GetDatabaseName() ==
                    "UX_Lands_OrganizationId_Code");

        Assert.True(index.IsUnique);

        Assert.Equal(
            new[]
            {
                nameof(Land.OrganizationId),
                nameof(Land.Code)
            },
            index.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void Land_ShouldReferenceOrganizationWithRestrictDelete()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(Land))!;

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                    typeof(Organization));

        Assert.Equal(
            new[]
            {
                nameof(Land.OrganizationId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void LandPlotsNavigation_ShouldUseFieldAccess()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(Land))!;

        var navigation =
            entityType.FindNavigation(
                nameof(Land.Plots));

        Assert.NotNull(navigation);

        Assert.Equal(
            PropertyAccessMode.Field,
            navigation!.GetPropertyAccessMode());
    }

    [Fact]
    public void Land_ShouldHaveExpectedLengthsAndPrecision()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(Land))!;

        Assert.Equal(
            Land.MaxCodeLength,
            entityType.FindProperty(
                nameof(Land.Code))!
                .GetMaxLength());

        Assert.Equal(
            Land.MaxNameLength,
            entityType.FindProperty(
                nameof(Land.Name))!
                .GetMaxLength());

        Assert.Equal(
            Land.MaxAddressLength,
            entityType.FindProperty(
                nameof(Land.Address))!
                .GetMaxLength());

        var totalAreaProperty =
            entityType.FindProperty(
                nameof(Land.TotalArea))!;

        Assert.Equal(
            18,
            totalAreaProperty.GetPrecision());

        Assert.Equal(
            4,
            totalAreaProperty.GetScale());

        var latitudeProperty =
            entityType.FindProperty(
                nameof(Land.Latitude))!;

        Assert.Equal(
            9,
            latitudeProperty.GetPrecision());

        Assert.Equal(
            6,
            latitudeProperty.GetScale());
    }

    [Fact]
    public void LandPlot_ShouldHaveCompositeParentForeignKey()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(LandPlot))!;

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                    typeof(Land));

        Assert.Equal(
            new[]
            {
                nameof(LandPlot.OrganizationId),
                nameof(LandPlot.LandId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            new[]
            {
                nameof(Land.OrganizationId),
                nameof(Land.Id)
            },
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Cascade,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void LandPlot_ShouldReferenceOrganizationWithRestrictDelete()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(LandPlot))!;

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                    typeof(Organization));

        Assert.Equal(
            new[]
            {
                nameof(LandPlot.OrganizationId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void LandPlotCode_ShouldBeUniqueWithinLand()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(LandPlot))!;

        var index =
            entityType.GetIndexes()
                .Single(candidate =>
                    candidate.GetDatabaseName() ==
                    "UX_LandPlots_" +
                    "OrganizationId_LandId_Code");

        Assert.True(index.IsUnique);

        Assert.Equal(
            new[]
            {
                nameof(LandPlot.OrganizationId),
                nameof(LandPlot.LandId),
                nameof(LandPlot.Code)
            },
            index.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void LandPlot_ShouldHaveExpectedLengthsAndPrecision()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(LandPlot))!;

        Assert.Equal(
            LandPlot.MaxCodeLength,
            entityType.FindProperty(
                nameof(LandPlot.Code))!
                .GetMaxLength());

        Assert.Equal(
            LandPlot.MaxNameLength,
            entityType.FindProperty(
                nameof(LandPlot.Name))!
                .GetMaxLength());

        Assert.Equal(
            LandPlot.MaxGeneralConditionLength,
            entityType.FindProperty(
                nameof(LandPlot.GeneralCondition))!
                .GetMaxLength());

        Assert.Equal(
            LandPlot.MaxNotesLength,
            entityType.FindProperty(
                nameof(LandPlot.Notes))!
                .GetMaxLength());

        var areaProperty =
            entityType.FindProperty(
                nameof(LandPlot.Area))!;

        Assert.Equal(
            18,
            areaProperty.GetPrecision());

        Assert.Equal(
            4,
            areaProperty.GetScale());
    }

    private static SiPaculDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseNpgsql(
                    "Host=localhost;" +
                    "Database=sipacul_model_tests;" +
                    "Username=test;" +
                    "Password=test")
                .Options;

        return new SiPaculDbContext(options);
    }
}
