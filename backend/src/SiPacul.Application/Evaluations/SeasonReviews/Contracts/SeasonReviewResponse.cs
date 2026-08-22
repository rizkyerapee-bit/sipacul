using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonReviews.Contracts;

public sealed record SeasonReviewResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    DateOnly ReviewDate,
    string Findings,
    string LessonsLearned,
    string NextSeasonRecommendations,
    SeasonReviewStatus Status,
    DateTime? FinalizedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
