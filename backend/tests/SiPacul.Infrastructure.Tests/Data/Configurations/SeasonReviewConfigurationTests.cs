using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Infrastructure.Data;
using Xunit;

namespace SiPacul.Infrastructure.Tests.Data.Configurations;

public sealed class SeasonReviewConfigurationTests
{
    [Fact]
    public void Model_ShouldConfigureSeasonReviewPersistence()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            typeof(SeasonReview));

        Assert.NotNull(entity);
        Assert.Equal("SeasonReviews", entity!.GetTableName());
        Assert.Equal(
            "date",
            entity.FindProperty(nameof(SeasonReview.ReviewDate))!
                .GetColumnType());
        Assert.Equal(
            SeasonReview.MaxFindingsLength,
            entity.FindProperty(nameof(SeasonReview.Findings))!
                .GetMaxLength());
        Assert.Equal(
            SeasonReview.MaxLessonsLearnedLength,
            entity.FindProperty(nameof(SeasonReview.LessonsLearned))!
                .GetMaxLength());
        Assert.Equal(
            SeasonReview.MaxNextSeasonRecommendationsLength,
            entity.FindProperty(
                nameof(SeasonReview.NextSeasonRecommendations))!
                .GetMaxLength());
        Assert.Null(
            entity.FindProperty(nameof(SeasonReview.IsFinalized)));
    }

    [Fact]
    public void Model_ShouldEnforceOneActiveReviewPerCycle()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            typeof(SeasonReview))!;

        var index = entity.GetIndexes().Single(candidate =>
            candidate.GetDatabaseName() ==
                "UX_SeasonReviews_" +
                "OrganizationId_CropCycleId_Active");

        Assert.True(index.IsUnique);
        Assert.Equal(
            new[]
            {
                nameof(SeasonReview.OrganizationId),
                nameof(SeasonReview.CropCycleId)
            },
            index.Properties.Select(property => property.Name));
        Assert.Equal("\"IsDeleted\" = FALSE", index.GetFilter());
    }

    [Fact]
    public void Model_ShouldUseCompositeCycleForeignKey()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(
            typeof(SeasonReview))!;

        var foreignKey = entity.GetForeignKeys().Single(candidate =>
            candidate.PrincipalEntityType.ClrType ==
                typeof(CropCycle));

        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
        Assert.Equal(
            new[]
            {
                nameof(SeasonReview.OrganizationId),
                nameof(SeasonReview.CropCycleId)
            },
            foreignKey.Properties.Select(property => property.Name));
    }

    private static SiPaculDbContext CreateContext()
    {
        var options =
            new DbContextOptionsBuilder<SiPaculDbContext>()
                .UseNpgsql(
                    "Host=localhost;Port=5432;" +
                    "Database=sipacul_model_tests;" +
                    "Username=sipacul;Password=sipacul")
                .Options;

        return new SiPaculDbContext(options);
    }
}
