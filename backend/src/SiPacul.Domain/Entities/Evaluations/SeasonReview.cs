using SiPacul.Domain.Common.Base;
using SiPacul.Domain.Common.Interfaces;

namespace SiPacul.Domain.Entities.Evaluations;

public sealed class SeasonReview :
    AggregateRoot,
    IOrganizationOwned
{
    public const int MaxFindingsLength = 4000;

    public const int MaxLessonsLearnedLength = 4000;

    public const int MaxNextSeasonRecommendationsLength = 4000;

    private SeasonReview()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Guid CropCycleId { get; private set; }

    public DateOnly ReviewDate { get; private set; }

    public string Findings { get; private set; } = string.Empty;

    public string LessonsLearned { get; private set; } = string.Empty;

    public string NextSeasonRecommendations { get; private set; } =
        string.Empty;

    public SeasonReviewStatus Status { get; private set; } =
        SeasonReviewStatus.Draft;

    public DateTime? FinalizedAt { get; private set; }

    public bool IsFinalized => Status == SeasonReviewStatus.Finalized;

    public static SeasonReview Create(
        Guid organizationId,
        Guid cropCycleId,
        DateOnly reviewDate,
        string findings,
        string lessonsLearned,
        string nextSeasonRecommendations)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            cropCycleId,
            nameof(cropCycleId),
            "Crop cycle");

        ValidateReviewDate(reviewDate);

        return new SeasonReview
        {
            OrganizationId = organizationId,
            CropCycleId = cropCycleId,
            ReviewDate = reviewDate,
            Findings = NormalizeRequiredText(
                findings,
                MaxFindingsLength,
                nameof(findings),
                "Season findings"),
            LessonsLearned = NormalizeRequiredText(
                lessonsLearned,
                MaxLessonsLearnedLength,
                nameof(lessonsLearned),
                "Lessons learned"),
            NextSeasonRecommendations = NormalizeRequiredText(
                nextSeasonRecommendations,
                MaxNextSeasonRecommendationsLength,
                nameof(nextSeasonRecommendations),
                "Next-season recommendations"),
            Status = SeasonReviewStatus.Draft
        };
    }

    public void UpdateDraft(
        DateOnly reviewDate,
        string findings,
        string lessonsLearned,
        string nextSeasonRecommendations)
    {
        EnsureDraft(
            "Only a draft season review can be updated.");

        ValidateReviewDate(reviewDate);

        var normalizedFindings = NormalizeRequiredText(
            findings,
            MaxFindingsLength,
            nameof(findings),
            "Season findings");

        var normalizedLessons = NormalizeRequiredText(
            lessonsLearned,
            MaxLessonsLearnedLength,
            nameof(lessonsLearned),
            "Lessons learned");

        var normalizedRecommendations = NormalizeRequiredText(
            nextSeasonRecommendations,
            MaxNextSeasonRecommendationsLength,
            nameof(nextSeasonRecommendations),
            "Next-season recommendations");

        if (ReviewDate == reviewDate &&
            Findings == normalizedFindings &&
            LessonsLearned == normalizedLessons &&
            NextSeasonRecommendations == normalizedRecommendations)
        {
            return;
        }

        ReviewDate = reviewDate;
        Findings = normalizedFindings;
        LessonsLearned = normalizedLessons;
        NextSeasonRecommendations = normalizedRecommendations;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FinalizeReview()
    {
        EnsureDraft(
            "Only a draft season review can be finalized.");

        Status = SeasonReviewStatus.Finalized;
        FinalizedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureDraft(string message)
    {
        if (Status != SeasonReviewStatus.Draft)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName,
        string displayName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }

    private static void ValidateReviewDate(DateOnly reviewDate)
    {
        if (reviewDate == default)
        {
            throw new ArgumentException(
                "Review date must be provided.",
                nameof(reviewDate));
        }
    }

    private static string NormalizeRequiredText(
        string value,
        int maximumLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} cannot be empty.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
