using SiPacul.Domain.Entities.Evaluations;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Evaluations;

public sealed class SeasonReviewTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse("20000000-0000-0000-0000-000000000001");

    private static readonly DateOnly ReviewDate =
        new(2027, 5, 20);

    [Fact]
    public void Create_WithValidValues_ShouldCreateDraft()
    {
        var review = CreateReview();

        Assert.NotEqual(Guid.Empty, review.Id);
        Assert.Equal(OrganizationId, review.OrganizationId);
        Assert.Equal(CropCycleId, review.CropCycleId);
        Assert.Equal(ReviewDate, review.ReviewDate);
        Assert.Equal("Serangan hama terlambat terdeteksi.", review.Findings);
        Assert.Equal("Inspeksi mingguan perlu disiplin.", review.LessonsLearned);
        Assert.Equal("Jadwalkan inspeksi dua kali seminggu.", review.NextSeasonRecommendations);
        Assert.Equal(SeasonReviewStatus.Draft, review.Status);
        Assert.False(review.IsFinalized);
        Assert.Null(review.FinalizedAt);
    }

    [Fact]
    public void Create_ShouldNormalizeText()
    {
        var review = SeasonReview.Create(
            OrganizationId,
            CropCycleId,
            ReviewDate,
            "  Temuan  ",
            "  Pelajaran  ",
            "  Rekomendasi  ");

        Assert.Equal("Temuan", review.Findings);
        Assert.Equal("Pelajaran", review.LessonsLearned);
        Assert.Equal("Rekomendasi", review.NextSeasonRecommendations);
    }

    [Theory]
    [InlineData(true, false, "organizationId")]
    [InlineData(false, true, "cropCycleId")]
    public void Create_WithEmptyIdentifier_ShouldThrow(
        bool emptyOrganization,
        bool emptyCropCycle,
        string parameterName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SeasonReview.Create(
                emptyOrganization ? Guid.Empty : OrganizationId,
                emptyCropCycle ? Guid.Empty : CropCycleId,
                ReviewDate,
                "Temuan",
                "Pelajaran",
                "Rekomendasi"));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Create_WithDefaultDate_ShouldThrow()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SeasonReview.Create(
                OrganizationId,
                CropCycleId,
                default,
                "Temuan",
                "Pelajaran",
                "Rekomendasi"));

        Assert.Equal("reviewDate", exception.ParamName);
    }

    [Theory]
    [InlineData("findings")]
    [InlineData("lessonsLearned")]
    [InlineData("nextSeasonRecommendations")]
    public void Create_WithBlankRequiredText_ShouldThrow(string field)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            SeasonReview.Create(
                OrganizationId,
                CropCycleId,
                ReviewDate,
                field == "findings" ? " " : "Temuan",
                field == "lessonsLearned" ? " " : "Pelajaran",
                field == "nextSeasonRecommendations" ? " " : "Rekomendasi"));

        Assert.Equal(field, exception.ParamName);
    }

    [Theory]
    [InlineData("findings")]
    [InlineData("lessonsLearned")]
    [InlineData("nextSeasonRecommendations")]
    public void Create_WithTooLongText_ShouldThrow(string field)
    {
        var value = new string('X', 4001);

        var exception = Assert.Throws<ArgumentException>(() =>
            SeasonReview.Create(
                OrganizationId,
                CropCycleId,
                ReviewDate,
                field == "findings" ? value : "Temuan",
                field == "lessonsLearned" ? value : "Pelajaran",
                field == "nextSeasonRecommendations" ? value : "Rekomendasi"));

        Assert.Equal(field, exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithChangedValues_ShouldUpdate()
    {
        var review = CreateReview();
        var date = ReviewDate.AddDays(1);

        review.UpdateDraft(
            date,
            "  Temuan baru  ",
            "  Pelajaran baru  ",
            "  Rekomendasi baru  ");

        Assert.Equal(date, review.ReviewDate);
        Assert.Equal("Temuan baru", review.Findings);
        Assert.Equal("Pelajaran baru", review.LessonsLearned);
        Assert.Equal("Rekomendasi baru", review.NextSeasonRecommendations);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_WithSameValues_ShouldNotSetUpdatedAt()
    {
        var review = CreateReview();

        review.UpdateDraft(
            ReviewDate,
            "Serangan hama terlambat terdeteksi.",
            "Inspeksi mingguan perlu disiplin.",
            "Jadwalkan inspeksi dua kali seminggu.");

        Assert.Null(review.UpdatedAt);
    }

    [Fact]
    public void FinalizeReview_ShouldMakeReviewImmutable()
    {
        var review = CreateReview();
        var before = DateTime.UtcNow;

        review.FinalizeReview();

        var after = DateTime.UtcNow;
        Assert.Equal(SeasonReviewStatus.Finalized, review.Status);
        Assert.True(review.IsFinalized);
        Assert.NotNull(review.FinalizedAt);
        Assert.InRange(review.FinalizedAt!.Value, before, after);
        Assert.NotNull(review.UpdatedAt);

        Assert.Throws<InvalidOperationException>(() =>
            review.UpdateDraft(
                ReviewDate,
                "Temuan",
                "Pelajaran",
                "Rekomendasi"));
    }

    [Fact]
    public void FinalizeReview_WhenAlreadyFinalized_ShouldThrow()
    {
        var review = CreateReview();
        review.FinalizeReview();

        Assert.Throws<InvalidOperationException>(review.FinalizeReview);
    }

    private static SeasonReview CreateReview()
    {
        return SeasonReview.Create(
            OrganizationId,
            CropCycleId,
            ReviewDate,
            "  Serangan hama terlambat terdeteksi.  ",
            "  Inspeksi mingguan perlu disiplin.  ",
            "  Jadwalkan inspeksi dua kali seminggu.  ");
    }
}
