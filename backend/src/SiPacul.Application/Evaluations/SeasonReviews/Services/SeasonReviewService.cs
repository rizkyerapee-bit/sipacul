using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Application.Evaluations.SeasonReviews.Mappings;
using SiPacul.Application.Evaluations.SeasonReviews.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Evaluations;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Evaluations.SeasonReviews.Services;

public sealed class SeasonReviewService : ISeasonReviewService
{
    private readonly ISeasonReviewRepository _reviews;
    private readonly ICropCycleRepository _cropCycles;
    private readonly IUnitOfWork _unitOfWork;

    public SeasonReviewService(ISeasonReviewRepository reviews, ICropCycleRepository cropCycles, IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(reviews);
        ArgumentNullException.ThrowIfNull(cropCycles);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        _reviews = reviews;
        _cropCycles = cropCycles;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SeasonReviewResponse>> CreateAsync(Guid organizationId, CreateSeasonReviewRequest request, CancellationToken cancellationToken = default)
    {
        var error = ValidateOrganization(organizationId);
        if (error is not null) return Result<SeasonReviewResponse>.Failure(error);
        if (request is null) return Failure("Season review request cannot be null.");
        if (request.CropCycleId == Guid.Empty) return Failure("Crop cycle identifier cannot be empty.");

        var cycle = await _cropCycles.GetByIdAsync(organizationId, request.CropCycleId, cancellationToken);
        if (cycle is null) return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.CropCycleNotFound(organizationId, request.CropCycleId));
        if (cycle.Status is not (CropCycleStatus.Completed or CropCycleStatus.Cancelled))
            return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.CropCycleNotTerminal(request.CropCycleId));
        if (await _reviews.GetByCropCycleAsync(organizationId, request.CropCycleId, cancellationToken) is not null)
            return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.AlreadyExists(request.CropCycleId));

        SeasonReview review;
        try
        {
            review = SeasonReview.Create(organizationId, request.CropCycleId, request.ReviewDate,
                request.Findings, request.LessonsLearned, request.NextSeasonRecommendations);
        }
        catch (ArgumentException exception) { return Failure(exception.Message); }

        await _reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonReviewResponse>.Success(review.ToResponse());
    }

    public async Task<Result<SeasonReviewResponse>> GetByIdAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var error = ValidateIds(organizationId, reviewId);
        if (error is not null) return Result<SeasonReviewResponse>.Failure(error);
        var review = await _reviews.GetByIdAsync(organizationId, reviewId, cancellationToken);
        return review is null
            ? Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.NotFound(organizationId, reviewId))
            : Result<SeasonReviewResponse>.Success(review.ToResponse());
    }

    public async Task<Result<SeasonReviewResponse>> GetByCropCycleAsync(Guid organizationId, Guid cropCycleId, CancellationToken cancellationToken = default)
    {
        var error = ValidateIds(organizationId, cropCycleId);
        if (error is not null) return Result<SeasonReviewResponse>.Failure(error);
        var review = await _reviews.GetByCropCycleAsync(organizationId, cropCycleId, cancellationToken);
        return review is null
            ? Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.CropCycleNotFound(organizationId, cropCycleId))
            : Result<SeasonReviewResponse>.Success(review.ToResponse());
    }

    public async Task<Result<SeasonReviewResponse>> UpdateAsync(Guid organizationId, Guid reviewId, UpdateSeasonReviewRequest request, CancellationToken cancellationToken = default)
    {
        var error = ValidateIds(organizationId, reviewId);
        if (error is not null) return Result<SeasonReviewResponse>.Failure(error);
        if (request is null) return Failure("Update season review request cannot be null.");
        var review = await _reviews.GetByIdAsync(organizationId, reviewId, cancellationToken);
        if (review is null) return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.NotFound(organizationId, reviewId));
        try
        {
            review.UpdateDraft(request.ReviewDate, request.Findings, request.LessonsLearned, request.NextSeasonRecommendations);
        }
        catch (ArgumentException exception) { return Failure(exception.Message); }
        catch (InvalidOperationException exception) { return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.InvalidStatusTransition(exception.Message)); }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonReviewResponse>.Success(review.ToResponse());
    }

    public async Task<Result<SeasonReviewResponse>> FinalizeAsync(Guid organizationId, Guid reviewId, CancellationToken cancellationToken = default)
    {
        var error = ValidateIds(organizationId, reviewId);
        if (error is not null) return Result<SeasonReviewResponse>.Failure(error);
        var review = await _reviews.GetByIdAsync(organizationId, reviewId, cancellationToken);
        if (review is null) return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.NotFound(organizationId, reviewId));
        try { review.FinalizeReview(); }
        catch (InvalidOperationException exception) { return Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.InvalidStatusTransition(exception.Message)); }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<SeasonReviewResponse>.Success(review.ToResponse());
    }

    private static Result<SeasonReviewResponse> Failure(string message) => Result<SeasonReviewResponse>.Failure(SeasonReviewErrors.Validation(message));
    private static Error? ValidateOrganization(Guid id) => id == Guid.Empty ? SeasonReviewErrors.Validation("Organization identifier cannot be empty.") : null;
    private static Error? ValidateIds(Guid organizationId, Guid resourceId) => ValidateOrganization(organizationId) ?? (resourceId == Guid.Empty ? SeasonReviewErrors.Validation("Identifier cannot be empty.") : null);
}
