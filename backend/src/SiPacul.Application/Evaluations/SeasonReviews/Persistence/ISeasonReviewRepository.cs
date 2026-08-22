using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonReviews.Persistence;

public interface ISeasonReviewRepository
{
    Task<SeasonReview?> GetByIdAsync(
        Guid organizationId,
        Guid reviewId,
        CancellationToken cancellationToken = default);

    Task<SeasonReview?> GetByCropCycleAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SeasonReview review,
        CancellationToken cancellationToken = default);
}
