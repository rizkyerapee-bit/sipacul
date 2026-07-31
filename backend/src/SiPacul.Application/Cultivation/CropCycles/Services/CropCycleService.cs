using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Mappings;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.CropCycles.Services;

public sealed class CropCycleService :
    ICropCycleService
{
    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly ICommodityRepository
        _commodityRepository;

    private readonly ICultivationSopRepository
        _cultivationSopRepository;

    private readonly ILandRepository _landRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CropCycleService(
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        ICommodityRepository commodityRepository,
        ICultivationSopRepository cultivationSopRepository,
        ILandRepository landRepository,
        IUnitOfWork unitOfWork)
    {
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository =
            organizationRepository;
        _commodityRepository = commodityRepository;
        _cultivationSopRepository =
            cultivationSopRepository;
        _landRepository = landRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CropCycleResponse>> CreateAsync(
        Guid organizationId,
        CreateCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                organizationError);
        }

        if (request is null)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    "Crop cycle request cannot be null."));
        }

        CropCycle cropCycle;

        try
        {
            cropCycle = CropCycle.Create(
                organizationId,
                request.Code,
                request.Name,
                request.CommodityId,
                request.CultivationSopId,
                request.LandId,
                request.LandPlotId,
                request.PlantedArea,
                request.AreaUnit,
                request.PlannedStartDate,
                request.ExpectedHarvestDate,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.OrganizationNotFound(
                    organizationId));
        }

        var referenceResult =
            await ValidateReferencesAsync(
                organizationId,
                cropCycle.CommodityId,
                cropCycle.CultivationSopId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                true,
                cancellationToken);

        if (referenceResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                referenceResult.Error);
        }

        var areaError = ValidatePlotCapacity(
            cropCycle.PlantedAreaInSquareMeters,
            referenceResult.Value.Plot);

        if (areaError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                areaError);
        }

        if (await _cropCycleRepository.CodeExistsAsync(
                organizationId,
                cropCycle.Code,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.CodeAlreadyExists(
                    cropCycle.Code));
        }

        if (await _cropCycleRepository
            .HasScheduleConflictAsync(
                organizationId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                cropCycle.PlannedStartDate,
                cropCycle.ExpectedHarvestDate,
                null,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.ScheduleConflict(
                    cropCycle.LandPlotId));
        }

        _cropCycleRepository.Add(cropCycle);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CropCycleResponse>>>
        GetAllAsync(
            Guid organizationId,
            CropCycleFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return Result<
                IReadOnlyList<CropCycleResponse>>
                .Failure(organizationError);
        }

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<CropCycleResponse>>
                .Failure(filterError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<
                IReadOnlyList<CropCycleResponse>>
                .Failure(
                    CropCycleErrors.OrganizationNotFound(
                        organizationId));
        }

        filter ??= new CropCycleFilter();

        var cropCycles =
            await _cropCycleRepository.GetAllAsync(
                organizationId,
                filter.Status,
                filter.CommodityId,
                filter.LandId,
                filter.LandPlotId,
                filter.PlannedStartFrom,
                filter.PlannedStartTo,
                cancellationToken);

        IReadOnlyList<CropCycleResponse> responses =
            cropCycles
                .Select(cropCycle =>
                    cropCycle.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CropCycleResponse>>
            .Success(responses);
    }

    public async Task<Result<CropCycleResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.OrganizationNotFound(
                    organizationId));
        }

        var cropCycle =
            await _cropCycleRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.NotFound(
                    organizationId,
                    cropCycleId));
        }

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<Result<CropCycleResponse>>
        UpdatePlanAsync(
            Guid organizationId,
            Guid cropCycleId,
            UpdateCropCyclePlanRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    "Crop cycle request cannot be null."));
        }

        var cropCycleResult = await GetForUpdateAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;

        CropCycle candidate;

        try
        {
            candidate = CropCycle.Create(
                cropCycle.OrganizationId,
                cropCycle.Code,
                request.Name,
                cropCycle.CommodityId,
                request.CultivationSopId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                request.PlantedArea,
                request.AreaUnit,
                request.PlannedStartDate,
                request.ExpectedHarvestDate,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }

        if (cropCycle.Status != CropCycleStatus.Planned)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    "Only a planned crop cycle can be updated."));
        }

        var referenceResult =
            await ValidateReferencesAsync(
                organizationId,
                cropCycle.CommodityId,
                candidate.CultivationSopId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                true,
                cancellationToken);

        if (referenceResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                referenceResult.Error);
        }

        var areaError = ValidatePlotCapacity(
            candidate.PlantedAreaInSquareMeters,
            referenceResult.Value.Plot);

        if (areaError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                areaError);
        }

        if (await _cropCycleRepository
            .HasScheduleConflictAsync(
                organizationId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                candidate.PlannedStartDate,
                candidate.ExpectedHarvestDate,
                cropCycle.Id,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.ScheduleConflict(
                    cropCycle.LandPlotId));
        }

        var previousName = cropCycle.Name;
        var previousSopId = cropCycle.CultivationSopId;
        var previousArea = cropCycle.PlantedArea;
        var previousAreaUnit = cropCycle.AreaUnit;
        var previousStart = cropCycle.PlannedStartDate;
        var previousHarvest =
            cropCycle.ExpectedHarvestDate;
        var previousNotes = cropCycle.Notes;

        try
        {
            cropCycle.UpdatePlan(
                request.Name,
                request.CultivationSopId,
                request.PlantedArea,
                request.AreaUnit,
                request.PlannedStartDate,
                request.ExpectedHarvestDate,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        var hasChanged =
            previousName != cropCycle.Name ||
            previousSopId != cropCycle.CultivationSopId ||
            previousArea != cropCycle.PlantedArea ||
            previousAreaUnit != cropCycle.AreaUnit ||
            previousStart != cropCycle.PlannedStartDate ||
            previousHarvest !=
                cropCycle.ExpectedHarvestDate ||
            previousNotes != cropCycle.Notes;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<Result<CropCycleResponse>> StartAsync(
        Guid organizationId,
        Guid cropCycleId,
        StartCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateLifecycleRequest(
                organizationId,
                cropCycleId,
                request,
                "Start crop cycle request cannot be null.");

        if (requestError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                requestError);
        }

        var cropCycleResult = await GetForUpdateAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;

        if (cropCycle.Status != CropCycleStatus.Planned)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    "Only a planned crop cycle can be started."));
        }

        var referenceResult =
            await ValidateReferencesAsync(
                organizationId,
                cropCycle.CommodityId,
                cropCycle.CultivationSopId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                true,
                cancellationToken);

        if (referenceResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                referenceResult.Error);
        }

        if (await _cropCycleRepository
            .HasInProgressCycleAsync(
                organizationId,
                cropCycle.LandId,
                cropCycle.LandPlotId,
                cropCycle.Id,
                cancellationToken))
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.ActiveCycleAlreadyExists(
                    cropCycle.LandPlotId));
        }

        try
        {
            cropCycle.Start(request.ActualStartDate);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<Result<CropCycleResponse>> CompleteAsync(
        Guid organizationId,
        Guid cropCycleId,
        CompleteCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateLifecycleRequest(
                organizationId,
                cropCycleId,
                request,
                "Complete crop cycle request cannot be null.");

        if (requestError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                requestError);
        }

        var cropCycleResult = await GetForUpdateAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;

        try
        {
            cropCycle.Complete(
                request.ActualHarvestDate);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<Result<CropCycleResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancelCropCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateLifecycleRequest(
                organizationId,
                cropCycleId,
                request,
                "Cancel crop cycle request cannot be null.");

        if (requestError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                requestError);
        }

        var cropCycleResult = await GetForUpdateAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;

        try
        {
            cropCycle.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    public async Task<Result<CropCycleResponse>>
        UpdateNotesAsync(
            Guid organizationId,
            Guid cropCycleId,
            UpdateCropCycleNotesRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateLifecycleRequest(
                organizationId,
                cropCycleId,
                request,
                "Update crop cycle notes request " +
                "cannot be null.");

        if (requestError is not null)
        {
            return Result<CropCycleResponse>.Failure(
                requestError);
        }

        var cropCycleResult = await GetForUpdateAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycleResult.IsFailure)
        {
            return Result<CropCycleResponse>.Failure(
                cropCycleResult.Error);
        }

        var cropCycle = cropCycleResult.Value;
        var previousNotes = cropCycle.Notes;

        try
        {
            cropCycle.UpdateNotes(request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleResponse>.Failure(
                CropCycleErrors.InvalidStatusTransition(
                    exception.Message));
        }

        if (previousNotes != cropCycle.Notes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CropCycleResponse>.Success(
            cropCycle.ToResponse());
    }

    private async Task<Result<CropCycle>> GetForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<CropCycle>.Failure(
                CropCycleErrors.OrganizationNotFound(
                    organizationId));
        }

        var cropCycle =
            await _cropCycleRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    cancellationToken);

        if (cropCycle is null)
        {
            return Result<CropCycle>.Failure(
                CropCycleErrors.NotFound(
                    organizationId,
                    cropCycleId));
        }

        return Result<CropCycle>.Success(cropCycle);
    }

    private async Task<Result<ReferenceContext>>
        ValidateReferencesAsync(
            Guid organizationId,
            Guid commodityId,
            Guid? cultivationSopId,
            Guid landId,
            Guid landPlotId,
            bool requireActive,
            CancellationToken cancellationToken)
    {
        var commodity =
            await _commodityRepository.GetByIdAsync(
                organizationId,
                commodityId,
                cancellationToken);

        if (commodity is null)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.CommodityNotFound(
                    commodityId));
        }

        if (requireActive && !commodity.IsActive)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.CommodityInactive(
                    commodityId));
        }

        var land =
            await _landRepository.GetByIdAsync(
                organizationId,
                landId,
                cancellationToken);

        if (land is null)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.LandNotFound(landId));
        }

        if (requireActive && !land.IsActive)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.LandInactive(landId));
        }

        var plot = land.Plots.SingleOrDefault(candidate =>
            candidate.Id == landPlotId);

        if (plot is null)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.PlotNotFound(
                    landId,
                    landPlotId));
        }

        if (requireActive && !plot.IsActive)
        {
            return Result<ReferenceContext>.Failure(
                CropCycleErrors.PlotInactive(
                    landPlotId));
        }

        CultivationSop? cultivationSop = null;

        if (cultivationSopId.HasValue)
        {
            cultivationSop =
                await _cultivationSopRepository
                    .GetByIdAsync(
                        organizationId,
                        cultivationSopId.Value,
                        cancellationToken);

            if (cultivationSop is null)
            {
                return Result<ReferenceContext>.Failure(
                    CropCycleErrors.SopNotFound(
                        cultivationSopId.Value));
            }

            if (requireActive &&
                !cultivationSop.IsActive)
            {
                return Result<ReferenceContext>.Failure(
                    CropCycleErrors.SopInactive(
                        cultivationSopId.Value));
            }

            if (cultivationSop.CommodityId !=
                commodityId)
            {
                return Result<ReferenceContext>.Failure(
                    CropCycleErrors.SopCommodityMismatch(
                        cultivationSopId.Value,
                        commodityId));
            }
        }

        return Result<ReferenceContext>.Success(
            new ReferenceContext(
                commodity,
                cultivationSop,
                land,
                plot));
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

    private static Error? ValidatePlotCapacity(
        decimal plantedAreaInSquareMeters,
        LandPlot plot)
    {
        var plotAreaInSquareMeters =
            ToSquareMeters(
                plot.Area,
                plot.AreaUnit);

        if (plantedAreaInSquareMeters >
            plotAreaInSquareMeters)
        {
            return CropCycleErrors.AreaCapacityExceeded(
                plantedAreaInSquareMeters,
                plotAreaInSquareMeters);
        }

        return null;
    }

    private static decimal ToSquareMeters(
        decimal area,
        AreaUnit areaUnit)
    {
        return areaUnit switch
        {
            AreaUnit.SquareMeter => area,
            AreaUnit.Hectare => area * 10_000m,
            _ => throw new ArgumentOutOfRangeException(
                nameof(areaUnit),
                areaUnit,
                "Area unit is not supported.")
        };
    }

    private static Error? ValidateFilter(
        CropCycleFilter? filter)
    {
        if (filter is null)
        {
            return null;
        }

        if (filter.CommodityId == Guid.Empty)
        {
            return CropCycleErrors.Validation(
                "Commodity identifier cannot be empty.");
        }

        if (filter.LandId == Guid.Empty)
        {
            return CropCycleErrors.Validation(
                "Land identifier cannot be empty.");
        }

        if (filter.LandPlotId == Guid.Empty)
        {
            return CropCycleErrors.Validation(
                "Land plot identifier cannot be empty.");
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return CropCycleErrors.Validation(
                "Crop cycle status is not supported.");
        }

        if (filter.PlannedStartFrom.HasValue &&
            filter.PlannedStartTo.HasValue &&
            filter.PlannedStartFrom.Value >
                filter.PlannedStartTo.Value)
        {
            return CropCycleErrors.Validation(
                "Planned start-from date cannot be after " +
                "the planned start-to date.");
        }

        return null;
    }

    private static Error? ValidateLifecycleRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CropCycleErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (cropCycleId == Guid.Empty)
        {
            return CropCycleErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return CropCycleErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }

    private sealed record ReferenceContext(
        Commodity Commodity,
        CultivationSop? CultivationSop,
        Land Land,
        LandPlot Plot);
}
