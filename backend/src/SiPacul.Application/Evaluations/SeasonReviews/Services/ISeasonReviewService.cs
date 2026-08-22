using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonReviews.Services;

public interface ISeasonReviewService
{
    Task<Result<SeasonReviewResponse>> CreateAsync(Guid organizationId, CreateSeasonReviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<SeasonReviewResponse>> GetByIdAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken = default);
    Task<Result<SeasonReviewResponse>> GetByCropCycleAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken = default);
    Task<Result<SeasonReviewResponse>> UpdateAsync(Guid organizationId, Guid reviewId, UpdateSeasonReviewRequest request, CancellationToken cancellationToken = default);
    Task<Result<SeasonReviewResponse>> FinalizeAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken = default);
}
