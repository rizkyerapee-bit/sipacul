using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonReviews.Mappings;

public static class SeasonReviewMappings
{
    public static SeasonReviewResponse ToResponse(this SeasonReview review)
    {
        ArgumentNullException.ThrowIfNull(review);

        return new SeasonReviewResponse(
            review.Id,
            review.OrganizationId,
            review.CropCycleId,
            review.ReviewDate,
            review.Findings,
            review.LessonsLearned,
            review.NextSeasonRecommendations,
            review.Status,
            review.FinalizedAt,
            review.CreatedAt,
            review.UpdatedAt);
    }
}
