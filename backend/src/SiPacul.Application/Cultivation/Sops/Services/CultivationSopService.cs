using SiPacul.Application.Cultivation.Activities;
using SiPacul.Application.Cultivation.Activities.Persistence;
using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Application.Cultivation.Sops.Mappings;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Sops.Services;

public sealed class CultivationSopService :
    ICultivationSopService
{
    private readonly ICultivationSopRepository
        _cultivationSopRepository;

    private readonly ICommodityRepository
        _commodityRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    private readonly ICultivationActivityRepository?
        _cultivationActivityRepository;

    public CultivationSopService(
        ICultivationSopRepository cultivationSopRepository,
        ICommodityRepository commodityRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork,
        ICultivationActivityRepository?
            cultivationActivityRepository = null)
    {
        _cultivationSopRepository =
            cultivationSopRepository;

        _commodityRepository =
            commodityRepository;

        _organizationRepository =
            organizationRepository;

        _unitOfWork = unitOfWork;

        _cultivationActivityRepository =
            cultivationActivityRepository;
    }

    public async Task<Result<CultivationSopResponse>>
        CreateAsync(
            Guid organizationId,
            CreateCultivationSopRequest request,
            CancellationToken cancellationToken = default)
    {
        var organizationIdError =
            ValidateOrganizationId(organizationId);

        if (organizationIdError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                organizationIdError);
        }

        if (request is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Cultivation SOP request cannot be null."));
        }

        CultivationSop cultivationSop;

        try
        {
            cultivationSop = CultivationSop.Create(
                organizationId,
                request.CommodityId,
                request.Name,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    exception.Message));
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.OrganizationNotFound(
                    organizationId));
        }

        if (!await CommodityExistsAsync(
                organizationId,
                cultivationSop.CommodityId,
                cancellationToken))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.CommodityNotFound(
                    organizationId,
                    cultivationSop.CommodityId));
        }

        var nameExists =
            await _cultivationSopRepository
                .NameExistsAsync(
                    organizationId,
                    cultivationSop.CommodityId,
                    cultivationSop.Name,
                    cancellationToken:
                        cancellationToken);

        if (nameExists)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.NameAlreadyExists(
                    cultivationSop.CommodityId,
                    cultivationSop.Name));
        }

        _cultivationSopRepository.Add(
            cultivationSop);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CultivationSopResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid? commodityId = null,
            CancellationToken cancellationToken = default)
    {
        var organizationIdError =
            ValidateOrganizationId(organizationId);

        if (organizationIdError is not null)
        {
            return Result<
                IReadOnlyList<CultivationSopResponse>>
                .Failure(organizationIdError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<
                IReadOnlyList<CultivationSopResponse>>
                .Failure(
                    CultivationSopErrors
                        .OrganizationNotFound(
                            organizationId));
        }

        if (commodityId == Guid.Empty)
        {
            return Result<
                IReadOnlyList<CultivationSopResponse>>
                .Failure(
                    CultivationSopErrors.Validation(
                        "Commodity identifier " +
                        "cannot be empty."));
        }

        if (commodityId.HasValue &&
            !await CommodityExistsAsync(
                organizationId,
                commodityId.Value,
                cancellationToken))
        {
            return Result<
                IReadOnlyList<CultivationSopResponse>>
                .Failure(
                    CultivationSopErrors.CommodityNotFound(
                        organizationId,
                        commodityId.Value));
        }

        var cultivationSops =
            await _cultivationSopRepository.GetAllAsync(
                organizationId,
                commodityId,
                cancellationToken);

        IReadOnlyList<CultivationSopResponse> responses =
            cultivationSops
                .Select(sop => sop.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CultivationSopResponse>>
            .Success(responses);
    }

    public async Task<Result<CultivationSopResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cultivationSopId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.OrganizationNotFound(
                    organizationId));
        }

        var cultivationSop =
            await _cultivationSopRepository.GetByIdAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSop is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.NotFound(
                    organizationId,
                    cultivationSopId));
        }

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public async Task<Result<CultivationSopResponse>>
        UpdateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            UpdateCultivationSopRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cultivationSopId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Cultivation SOP request cannot be null."));
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.OrganizationNotFound(
                    organizationId));
        }

        var cultivationSop =
            await _cultivationSopRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cultivationSopId,
                    cancellationToken);

        if (cultivationSop is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.NotFound(
                    organizationId,
                    cultivationSopId));
        }

        var normalizedName =
            request.Name?.Trim() ?? string.Empty;

        if (normalizedName != cultivationSop.Name)
        {
            var nameExists =
                await _cultivationSopRepository
                    .NameExistsAsync(
                        organizationId,
                        cultivationSop.CommodityId,
                        normalizedName,
                        cultivationSop.Id,
                        cancellationToken);

            if (nameExists)
            {
                return Result<CultivationSopResponse>.Failure(
                    CultivationSopErrors
                        .NameAlreadyExists(
                            cultivationSop.CommodityId,
                            normalizedName));
            }
        }

        var previousName = cultivationSop.Name;
        var previousDescription =
            cultivationSop.Description;

        try
        {
            cultivationSop.Update(
                normalizedName,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    exception.Message));
        }

        if (previousName != cultivationSop.Name ||
            previousDescription !=
                cultivationSop.Description)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public Task<Result<CultivationSopResponse>>
        ActivateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            cultivationSopId,
            true,
            cancellationToken);
    }

    public Task<Result<CultivationSopResponse>>
        DeactivateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            cultivationSopId,
            false,
            cancellationToken);
    }

    public async Task<Result<CultivationSopResponse>>
        AddStepAsync(
            Guid organizationId,
            Guid cultivationSopId,
            AddCultivationSopStepRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cultivationSopId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Cultivation SOP step request " +
                    "cannot be null."));
        }

        var cultivationSopResult =
            await GetForUpdateAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSopResult.IsFailure)
        {
            return Result<CultivationSopResponse>.Failure(
                cultivationSopResult.Error);
        }

        var cultivationSop =
            cultivationSopResult.Value;

        try
        {
            cultivationSop.AddStep(
                request.Name,
                request.Description,
                request.PlannedDayOffset,
                request.EstimatedDurationDays,
                request.IsRequired);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public async Task<Result<CultivationSopResponse>>
        UpdateStepAsync(
            Guid organizationId,
            Guid cultivationSopId,
            Guid stepId,
            UpdateCultivationSopStepRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateStepIdentifiers(
                organizationId,
                cultivationSopId,
                stepId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Cultivation SOP step request " +
                    "cannot be null."));
        }

        var cultivationSopResult =
            await GetForUpdateAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSopResult.IsFailure)
        {
            return Result<CultivationSopResponse>.Failure(
                cultivationSopResult.Error);
        }

        var cultivationSop =
            cultivationSopResult.Value;

        var step = cultivationSop.Steps
            .SingleOrDefault(candidate =>
                candidate.Id == stepId);

        if (step is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.StepNotFound(
                    cultivationSopId,
                    stepId));
        }

        var previousName = step.Name;
        var previousDescription = step.Description;
        var previousOffset = step.PlannedDayOffset;
        var previousDuration =
            step.EstimatedDurationDays;
        var previousRequired = step.IsRequired;

        try
        {
            cultivationSop.UpdateStep(
                stepId,
                request.Name,
                request.Description,
                request.PlannedDayOffset,
                request.EstimatedDurationDays,
                request.IsRequired);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    exception.Message));
        }

        var hasChanged =
            previousName != step.Name ||
            previousDescription != step.Description ||
            previousOffset != step.PlannedDayOffset ||
            previousDuration !=
                step.EstimatedDurationDays ||
            previousRequired != step.IsRequired;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public async Task<Result<CultivationSopResponse>>
        RemoveStepAsync(
            Guid organizationId,
            Guid cultivationSopId,
            Guid stepId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateStepIdentifiers(
                organizationId,
                cultivationSopId,
                stepId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        var cultivationSopResult =
            await GetForUpdateAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSopResult.IsFailure)
        {
            return Result<CultivationSopResponse>.Failure(
                cultivationSopResult.Error);
        }

        var cultivationSop =
            cultivationSopResult.Value;

        if (!cultivationSop.Steps.Any(step =>
                step.Id == stepId))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.StepNotFound(
                    cultivationSopId,
                    stepId));
        }

        if (_cultivationActivityRepository is not null &&
            await _cultivationActivityRepository
                .HasAnyActivityForSopStepAsync(
                    organizationId,
                    cultivationSopId,
                    stepId,
                    cancellationToken))
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationActivityErrors
                    .SopStepHistoricalReferenceExists(
                        stepId));
        }

        cultivationSop.RemoveStep(stepId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    public async Task<Result<CultivationSopResponse>>
        MoveStepAsync(
            Guid organizationId,
            Guid cultivationSopId,
            Guid stepId,
            MoveCultivationSopStepRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateStepIdentifiers(
                organizationId,
                cultivationSopId,
                stepId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    "Move step request cannot be null."));
        }

        var cultivationSopResult =
            await GetForUpdateAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSopResult.IsFailure)
        {
            return Result<CultivationSopResponse>.Failure(
                cultivationSopResult.Error);
        }

        var cultivationSop =
            cultivationSopResult.Value;

        var step = cultivationSop.Steps
            .SingleOrDefault(candidate =>
                candidate.Id == stepId);

        if (step is null)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.StepNotFound(
                    cultivationSopId,
                    stepId));
        }

        var previousSequence = step.Sequence;

        try
        {
            cultivationSop.MoveStep(
                stepId,
                request.NewSequence);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationSopResponse>.Failure(
                CultivationSopErrors.Validation(
                    exception.Message));
        }

        if (previousSequence != step.Sequence)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    private async Task<Result<CultivationSopResponse>>
        SetActiveStatusAsync(
            Guid organizationId,
            Guid cultivationSopId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cultivationSopId);

        if (identifierError is not null)
        {
            return Result<CultivationSopResponse>.Failure(
                identifierError);
        }

        var cultivationSopResult =
            await GetForUpdateAsync(
                organizationId,
                cultivationSopId,
                cancellationToken);

        if (cultivationSopResult.IsFailure)
        {
            return Result<CultivationSopResponse>.Failure(
                cultivationSopResult.Error);
        }

        var cultivationSop =
            cultivationSopResult.Value;

        var previousStatus =
            cultivationSop.IsActive;

        if (shouldBeActive)
        {
            cultivationSop.Activate();
        }
        else
        {
            cultivationSop.Deactivate();
        }

        if (previousStatus != cultivationSop.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationSopResponse>.Success(
            cultivationSop.ToResponse());
    }

    private async Task<Result<CultivationSop>>
        GetForUpdateAsync(
            Guid organizationId,
            Guid cultivationSopId,
            CancellationToken cancellationToken)
    {
        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CultivationSop>.Failure(
                CultivationSopErrors.OrganizationNotFound(
                    organizationId));
        }

        var cultivationSop =
            await _cultivationSopRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cultivationSopId,
                    cancellationToken);

        if (cultivationSop is null)
        {
            return Result<CultivationSop>.Failure(
                CultivationSopErrors.NotFound(
                    organizationId,
                    cultivationSopId));
        }

        return Result<CultivationSop>.Success(
            cultivationSop);
    }

    private async Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        return organization is not null;
    }

    private async Task<bool> CommodityExistsAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken)
    {
        var commodity =
            await _commodityRepository.GetByIdAsync(
                organizationId,
                commodityId,
                cancellationToken);

        return commodity is not null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cultivationSopId)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (cultivationSopId == Guid.Empty)
        {
            return CultivationSopErrors.Validation(
                "Cultivation SOP identifier " +
                "cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateStepIdentifiers(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cultivationSopId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (stepId == Guid.Empty)
        {
            return CultivationSopErrors.Validation(
                "Cultivation SOP step identifier " +
                "cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return CultivationSopErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }
}
