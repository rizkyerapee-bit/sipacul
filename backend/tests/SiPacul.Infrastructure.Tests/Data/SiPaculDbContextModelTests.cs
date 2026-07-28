using Microsoft.EntityFrameworkCore;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data;

public sealed class SiPaculDbContextModelTests
{
    [Fact]
    public void Model_ShouldContainExpectedEntities()
    {
        using var context = CreateContext();

        Assert.NotNull(
            context.Model.FindEntityType(
                typeof(Organization)));

        Assert.NotNull(
            context.Model.FindEntityType(
                typeof(CommodityCategory)));

        Assert.NotNull(
            context.Model.FindEntityType(
                typeof(Commodity)));
    }

    [Fact]
    public void OrganizationCode_ShouldHaveUniqueIndex()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(Organization));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .Single(candidate =>
                candidate.Properties
                    .Select(property => property.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(Organization.Code)
                        }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void CommodityCategoryName_ShouldBeUniqueWithinOrganization()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CommodityCategory));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .Single(candidate =>
                candidate.Properties
                    .Select(property => property.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(
                                CommodityCategory.OrganizationId),
                            nameof(
                                CommodityCategory.Name)
                        }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void CommodityCode_ShouldBeUniqueWithinOrganization()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(Commodity));

        Assert.NotNull(entityType);

        var index = entityType!
            .GetIndexes()
            .Single(candidate =>
                candidate.Properties
                    .Select(property => property.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(Commodity.OrganizationId),
                            nameof(Commodity.Code)
                        }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public void CommodityCategory_ShouldReferenceOrganization()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(CommodityCategory));

        Assert.NotNull(entityType);

        var foreignKey = entityType!
            .GetForeignKeys()
            .Single(candidate =>
                candidate.PrincipalEntityType.ClrType ==
                typeof(Organization));

        Assert.Equal(
            new[]
            {
                nameof(CommodityCategory.OrganizationId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Commodity_ShouldUseCompositeForeignKeyToCategory()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(Commodity));

        Assert.NotNull(entityType);

        var foreignKey = entityType!
            .GetForeignKeys()
            .Single(candidate =>
                candidate.PrincipalEntityType.ClrType ==
                typeof(CommodityCategory));

        Assert.Equal(
            new[]
            {
                nameof(Commodity.OrganizationId),
                nameof(Commodity.CommodityCategoryId)
            },
            foreignKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            new[]
            {
                nameof(CommodityCategory.OrganizationId),
                nameof(CommodityCategory.Id)
            },
            foreignKey.PrincipalKey.Properties
                .Select(property => property.Name));

        Assert.Equal(
            DeleteBehavior.Restrict,
            foreignKey.DeleteBehavior);
    }

    [Fact]
    public void CommodityCode_ShouldUseStringConversionAndMaximumLength()
    {
        using var context = CreateContext();

        var entityType = context.Model.FindEntityType(
            typeof(Commodity));

        Assert.NotNull(entityType);

        var property = entityType!.FindProperty(
            nameof(Commodity.Code));

        Assert.NotNull(property);

        Assert.Equal(
            CommodityCode.MaxLength,
            property!.GetMaxLength());

        Assert.NotNull(
            property.GetValueConverter());
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
