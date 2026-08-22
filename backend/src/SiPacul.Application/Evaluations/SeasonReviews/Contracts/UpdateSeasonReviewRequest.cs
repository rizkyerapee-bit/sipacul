namespace SiPacul.Application.Evaluations.SeasonReviews.Contracts;

public sealed record UpdateSeasonReviewRequest(
    DateOnly ReviewDate,
    string Findings,
    string LessonsLearned,
    string NextSeasonRecommendations);
