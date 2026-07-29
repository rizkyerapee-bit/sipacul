using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations.Cultivation;

public sealed class CultivationSopConfigurationTests
{
    [Fact]
    public void CultivationSop_ShouldMapToExpectedTable()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSop));

        Assert.NotNull(entityType);
        Assert.Equal(
            "CultivationSops",
            entityType!.GetTableName());
    }

    [Fact]
    public void CultivationSop_ShouldHaveCompositeCommodityForeignKey()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSop))!;

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                        typeof(Commodity));

        Assert.Equal(
            new[]
            {
                nameof(CultivationSop.OrganizationId),
                nameof(CultivationSop.CommodityId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            new[]
            {
                nameof(Commodity.OrganizationId),
                nameof(Commodity.Id)
            },
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void CultivationSop_ShouldHaveUniqueNameIndex()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSop))!;

        var index =
            entityType.GetIndexes()
                .Single(candidate =>
                    candidate.GetDatabaseName() ==
                        "UX_CultivationSops_" +
                        "OrganizationId_CommodityId_Name");

        Assert.True(index.IsUnique);

        Assert.Equal(
            new[]
            {
                nameof(CultivationSop.OrganizationId),
                nameof(CultivationSop.CommodityId),
                nameof(CultivationSop.Name)
            },
            index.Properties
                .Select(property => property.Name));
    }

    [Fact]
    public void CultivationSop_StepsNavigation_ShouldUseFieldAccess()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSop))!;

        var navigation =
            entityType.FindNavigation(
                nameof(CultivationSop.Steps));

        Assert.NotNull(navigation);

        Assert.Equal(
            PropertyAccessMode.Field,
            navigation!.GetPropertyAccessMode());
    }

    [Fact]
    public void CultivationSopStep_ShouldMapToExpectedTable()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSopStep));

        Assert.NotNull(entityType);
        Assert.Equal(
            "CultivationSopSteps",
            entityType!.GetTableName());
    }

    [Fact]
    public void CultivationSopStep_ShouldHaveCompositeParentForeignKey()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSopStep))!;

        var foreignKey =
            entityType.GetForeignKeys()
                .Single(candidate =>
                    candidate.PrincipalEntityType.ClrType ==
                        typeof(CultivationSop));

        Assert.Equal(
            new[]
            {
                nameof(CultivationSopStep.OrganizationId),
                nameof(
                    CultivationSopStep.CultivationSopId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            new[]
            {
                nameof(CultivationSop.OrganizationId),
                nameof(CultivationSop.Id)
            },
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Cascade,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void CultivationSopStep_ShouldNotModelSequenceAsUniqueIndex()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSopStep))!;

        var sequenceProperties =
            new[]
            {
                nameof(CultivationSopStep.OrganizationId),
                nameof(
                    CultivationSopStep.CultivationSopId),
                nameof(CultivationSopStep.Sequence)
            };

        var sequenceIndex =
            entityType.GetIndexes()
                .SingleOrDefault(candidate =>
                    candidate.Properties
                        .Select(property => property.Name)
                        .SequenceEqual(sequenceProperties));

        Assert.Null(sequenceIndex);
    }

    [Fact]
    public void CultivationSopStep_ShouldHaveExpectedMaximumLengths()
    {
        using var dbContext = CreateDbContext();

        var entityType =
            dbContext.Model.FindEntityType(
                typeof(CultivationSopStep))!;

        var nameProperty =
            entityType.FindProperty(
                nameof(CultivationSopStep.Name));

        var descriptionProperty =
            entityType.FindProperty(
                nameof(CultivationSopStep.Description));

        Assert.NotNull(nameProperty);
        Assert.NotNull(descriptionProperty);

        Assert.Equal(
            CultivationSopStep.MaxNameLength,
            nameProperty!.GetMaxLength());

        Assert.Equal(
            CultivationSopStep.MaxDescriptionLength,
            descriptionProperty!.GetMaxLength());
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
