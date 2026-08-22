namespace SiPacul.Application.Evaluations.SeasonReviews.Contracts;

public sealed record CreateSeasonReviewRequest(
    Guid CropCycleId,
    DateOnly ReviewDate,
    string Findings,
    string LessonsLearned,
    string NextSeasonRecommendations);
