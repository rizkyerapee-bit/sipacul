using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Harvests.Contracts;
using SiPacul.Application.Harvests.Mappings;
using SiPacul.Application.Harvests.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Harvests.Services;

public sealed class HarvestBatchService :
    IHarvestBatchService
{
    private readonly IHarvestBatchRepository
        _harvestBatchRepository;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public HarvestBatchService(
        IHarvestBatchRepository harvestBatchRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _harvestBatchRepository =
            harvestBatchRepository;

        _cropCycleRepository = cropCycleRepository;

        _organizationRepository =
            organizationRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HarvestBatchResponse>>
        CreateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateHarvestBatchRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            request,
            "Harvest batch request cannot be null.");

        if (requestError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                requestError);
        }

        var cropCycleResult =
            await GetCropCycleAsync(
                organizationId,
                cropCycleId,
                false,
                cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<HarvestBatchResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;

        var lifecycleError =
            ValidateCropCycleInProgress(cropCycle);

        if (lifecycleError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                lifecycleError);
        }

        var dateError =
            ValidateHarvestDate(
                request.HarvestDate,
                cropCycle);

        if (dateError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                dateError);
        }

        HarvestBatch harvestBatch;

        try
        {
            harvestBatch = HarvestBatch.Create(
                organizationId,
                cropCycleId,
                request.Code,
                request.HarvestDate,
                request.GrossQuantity,
                request.RejectedQuantity,
                request.QuantityUnit,
                request.QualityGrade,
                request.StorageLocation,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors.Validation(
                    exception.Message));
        }

        if (await _harvestBatchRepository
            .CodeExistsAsync(
                organizationId,
                cropCycleId,
                harvestBatch.Code,
                cancellationToken))
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors.CodeAlreadyExists(
                    harvestBatch.Code));
        }

        _harvestBatchRepository.Add(harvestBatch);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<HarvestBatchResponse>.Success(
            harvestBatch.ToResponse());
    }

    public async Task<Result<
        IReadOnlyList<HarvestBatchResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            HarvestBatchFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<HarvestBatchResponse>>
                .Failure(identifierError);
        }

        filter ??= new HarvestBatchFilter();

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<HarvestBatchResponse>>
                .Failure(filterError);
        }

        var cropCycleResult =
            await GetCropCycleAsync(
                organizationId,
                cropCycleId,
                false,
                cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<
                IReadOnlyList<HarvestBatchResponse>>
                .Failure(cropCycleResult.Error);
        }

        var batches =
            await _harvestBatchRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                filter.Status,
                filter.HarvestDateFrom,
                filter.HarvestDateTo,
                filter.QuantityUnit,
                NormalizeQualityFilter(
                    filter.QualityGrade),
                cancellationToken);

        IReadOnlyList<HarvestBatchResponse> responses =
            batches
                .Select(batch => batch.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<HarvestBatchResponse>>
            .Success(responses);
    }

    public async Task<Result<HarvestBatchResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId,
                harvestBatchId);

        if (identifierError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                identifierError);
        }

        var cropCycleResult =
            await GetCropCycleAsync(
                organizationId,
                cropCycleId,
                false,
                cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<HarvestBatchResponse>.Failure(
                cropCycleResult.Error);
        }

        var harvestBatch =
            await _harvestBatchRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                harvestBatchId,
                cancellationToken);

        if (harvestBatch is null)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors.NotFound(
                    cropCycleId,
                    harvestBatchId));
        }

        return Result<HarvestBatchResponse>.Success(
            harvestBatch.ToResponse());
    }

    public async Task<Result<HarvestBatchResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            UpdateHarvestBatchRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            harvestBatchId,
            request,
            "Harvest batch update request cannot be null.");

        if (requestError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                requestError);
        }

        var contextResult =
            await GetMutationContextAsync(
                organizationId,
                cropCycleId,
                harvestBatchId,
                true,
                cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<HarvestBatchResponse>.Failure(
                contextResult.Error);
        }

        var context = contextResult.Value;

        var lifecycleError =
            ValidateCropCycleInProgress(
                context.CropCycle);

        if (lifecycleError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                lifecycleError);
        }

        var dateError =
            ValidateHarvestDate(
                request.HarvestDate,
                context.CropCycle);

        if (dateError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                dateError);
        }

        try
        {
            context.HarvestBatch.UpdateDraft(
                request.HarvestDate,
                request.GrossQuantity,
                request.RejectedQuantity,
                request.QuantityUnit,
                request.QualityGrade,
                request.StorageLocation,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors
                    .InvalidStatusTransition(
                        exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<HarvestBatchResponse>.Success(
            context.HarvestBatch.ToResponse());
    }

    public async Task<Result<HarvestBatchResponse>>
        ConfirmAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId,
                harvestBatchId);

        if (identifierError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                identifierError);
        }

        var contextResult =
            await GetMutationContextAsync(
                organizationId,
                cropCycleId,
                harvestBatchId,
                true,
                cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<HarvestBatchResponse>.Failure(
                contextResult.Error);
        }

        var context = contextResult.Value;

        var lifecycleError =
            ValidateCropCycleInProgress(
                context.CropCycle);

        if (lifecycleError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                lifecycleError);
        }

        var dateError =
            ValidateHarvestDate(
                context.HarvestBatch.HarvestDate,
                context.CropCycle);

        if (dateError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                dateError);
        }

        try
        {
            context.HarvestBatch.Confirm();
        }
        catch (InvalidOperationException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors
                    .InvalidStatusTransition(
                        exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<HarvestBatchResponse>.Success(
            context.HarvestBatch.ToResponse());
    }

    public async Task<Result<HarvestBatchResponse>>
        CancelAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            CancelHarvestBatchRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            harvestBatchId,
            request,
            "Harvest batch cancellation request cannot be null.");

        if (requestError is not null)
        {
            return Result<HarvestBatchResponse>.Failure(
                requestError);
        }

        var contextResult =
            await GetMutationContextAsync(
                organizationId,
                cropCycleId,
                harvestBatchId,
                false,
                cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<HarvestBatchResponse>.Failure(
                contextResult.Error);
        }

        try
        {
            contextResult.Value.HarvestBatch.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<HarvestBatchResponse>.Failure(
                HarvestBatchErrors
                    .InvalidStatusTransition(
                        exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<HarvestBatchResponse>.Success(
            contextResult.Value.HarvestBatch
                .ToResponse());
    }

    private async Task<Result<CropCycle>>
        GetCropCycleAsync(
            Guid organizationId,
            Guid cropCycleId,
            bool forUpdate,
            CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null ||
            organization.IsDeleted)
        {
            return Result<CropCycle>.Failure(
                HarvestBatchErrors.OrganizationNotFound(
                    organizationId));
        }

        var cropCycle =
            forUpdate
                ? await _cropCycleRepository
                    .GetByIdForUpdateAsync(
                        organizationId,
                        cropCycleId,
                        cancellationToken)
                : await _cropCycleRepository
                    .GetByIdAsync(
                        organizationId,
                        cropCycleId,
                        cancellationToken);

        if (cropCycle is null)
        {
            return Result<CropCycle>.Failure(
                HarvestBatchErrors.CropCycleNotFound(
                    cropCycleId));
        }

        return Result<CropCycle>.Success(cropCycle);
    }

    private async Task<Result<MutationContext>>
        GetMutationContextAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid harvestBatchId,
            bool requireCropCycleForUpdate,
            CancellationToken cancellationToken)
    {
        var cropCycleResult =
            await GetCropCycleAsync(
                organizationId,
                cropCycleId,
                requireCropCycleForUpdate,
                cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<MutationContext>.Failure(
                cropCycleResult.Error);
        }

        var harvestBatch =
            await _harvestBatchRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    harvestBatchId,
                    cancellationToken);

        if (harvestBatch is null)
        {
            return Result<MutationContext>.Failure(
                HarvestBatchErrors.NotFound(
                    cropCycleId,
                    harvestBatchId));
        }

        return Result<MutationContext>.Success(
            new MutationContext(
                cropCycleResult.Value,
                harvestBatch));
    }

    private static Error? ValidateCropCycleInProgress(
        CropCycle cropCycle)
    {
        if (cropCycle.Status !=
            CropCycleStatus.InProgress)
        {
            return HarvestBatchErrors
                .CropCycleNotInProgress(
                    cropCycle.Id);
        }

        return null;
    }

    private static Error? ValidateHarvestDate(
        DateOnly harvestDate,
        CropCycle cropCycle)
    {
        if (cropCycle.ActualStartDate.HasValue &&
            harvestDate <
                cropCycle.ActualStartDate.Value)
        {
            return HarvestBatchErrors
                .InvalidHarvestDate(
                    harvestDate,
                    cropCycle.ActualStartDate.Value);
        }

        if (cropCycle.ActualHarvestDate.HasValue &&
            harvestDate >
                cropCycle.ActualHarvestDate.Value)
        {
            return HarvestBatchErrors.Validation(
                "Harvest date cannot be after the crop " +
                "cycle actual harvest date.");
        }

        return null;
    }

    private static Error? ValidateFilter(
        HarvestBatchFilter filter)
    {
        if (filter.HarvestDateFrom.HasValue &&
            filter.HarvestDateTo.HasValue &&
            filter.HarvestDateFrom.Value >
                filter.HarvestDateTo.Value)
        {
            return HarvestBatchErrors.Validation(
                "Harvest date from cannot be after " +
                "harvest date to.");
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return HarvestBatchErrors.Validation(
                "Harvest batch status filter is invalid.");
        }

        if (filter.QuantityUnit.HasValue &&
            !Enum.IsDefined(filter.QuantityUnit.Value))
        {
            return HarvestBatchErrors.Validation(
                "Harvest quantity unit filter is invalid.");
        }

        return null;
    }

    private static string? NormalizeQualityFilter(
        string? qualityGrade)
    {
        return string.IsNullOrWhiteSpace(qualityGrade)
            ? null
            : qualityGrade.Trim();
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return HarvestBatchErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId,
                harvestBatchId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return HarvestBatchErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid? harvestBatchId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return HarvestBatchErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return HarvestBatchErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (harvestBatchId.HasValue &&
            harvestBatchId.Value == Guid.Empty)
        {
            return HarvestBatchErrors.Validation(
                "Harvest batch identifier cannot be empty.");
        }

        return null;
    }

    private sealed record MutationContext(
        CropCycle CropCycle,
        HarvestBatch HarvestBatch);
}
