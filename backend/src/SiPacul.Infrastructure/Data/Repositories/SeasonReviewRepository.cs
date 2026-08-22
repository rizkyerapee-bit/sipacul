using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Evaluations.SeasonReviews.Persistence;
using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SeasonReviewRepository :
    ISeasonReviewRepository
{
    private readonly SiPaculDbContext _dbContext;

    public SeasonReviewRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<SeasonReview?> GetByIdAsync(
        Guid organizationId,
        Guid reviewId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            reviewId,
            nameof(reviewId),
            "Season review");

        return _dbContext
            .Set<SeasonReview>()
            .SingleOrDefaultAsync(
                review =>
                    review.OrganizationId == organizationId &&
                    review.Id == reviewId &&
                    !review.IsDeleted,
                cancellationToken);
    }

    public Task<SeasonReview?> GetByCropCycleAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(
            organizationId,
            nameof(organizationId),
            "Organization");

        ValidateIdentifier(
            cropCycleId,
            nameof(cropCycleId),
            "Crop cycle");

        return _dbContext
            .Set<SeasonReview>()
            .SingleOrDefaultAsync(
                review =>
                    review.OrganizationId == organizationId &&
                    review.CropCycleId == cropCycleId &&
                    !review.IsDeleted,
                cancellationToken);
    }

    public async Task AddAsync(
        SeasonReview review,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(review);

        await _dbContext
            .Set<SeasonReview>()
            .AddAsync(
                review,
                cancellationToken);
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
}
