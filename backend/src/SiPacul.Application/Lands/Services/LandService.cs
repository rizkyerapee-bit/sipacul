using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Lands.Contracts;
using SiPacul.Application.Lands.Mappings;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Lands;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Lands.Services;

public sealed class LandService :
    ILandService
{
    private readonly ILandRepository _landRepository;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public LandService(
        ILandRepository landRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _landRepository = landRepository;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository =
            organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LandResponse>> CreateAsync(
        Guid organizationId,
        CreateLandRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationIdError =
            ValidateOrganizationId(organizationId);

        if (organizationIdError is not null)
        {
            return Result<LandResponse>.Failure(
                organizationIdError);
        }

        if (request is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    "Land request cannot be null."));
        }

        Land land;

        try
        {
            land = Land.Create(
                organizationId,
                request.Code,
                request.Name,
                request.TenureType,
                request.TotalArea,
                request.AreaUnit,
                request.Address,
                request.LocationDescription,
                request.Latitude,
                request.Longitude,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    exception.Message));
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<LandResponse>.Failure(
                LandErrors.OrganizationNotFound(
                    organizationId));
        }

        var codeExists =
            await _landRepository.CodeExistsAsync(
                organizationId,
                land.Code,
                cancellationToken);

        if (codeExists)
        {
            return Result<LandResponse>.Failure(
                LandErrors.CodeAlreadyExists(
                    land.Code));
        }

        _landRepository.Add(land);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<LandResponse>>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        var organizationIdError =
            ValidateOrganizationId(organizationId);

        if (organizationIdError is not null)
        {
            return Result<
                IReadOnlyList<LandResponse>>
                .Failure(organizationIdError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<
                IReadOnlyList<LandResponse>>
                .Failure(
                    LandErrors.OrganizationNotFound(
                        organizationId));
        }

        var lands =
            await _landRepository.GetAllAsync(
                organizationId,
                cancellationToken);

        IReadOnlyList<LandResponse> responses =
            lands
                .Select(land => land.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<LandResponse>>
            .Success(responses);
    }

    public async Task<Result<LandResponse>> GetByIdAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<LandResponse>.Failure(
                LandErrors.OrganizationNotFound(
                    organizationId));
        }

        var land =
            await _landRepository.GetByIdAsync(
                organizationId,
                landId,
                cancellationToken);

        if (land is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.NotFound(
                    organizationId,
                    landId));
        }

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public async Task<Result<LandResponse>> UpdateAsync(
        Guid organizationId,
        Guid landId,
        UpdateLandRequest request,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    "Land request cannot be null."));
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;

        var previousName = land.Name;
        var previousTenureType = land.TenureType;
        var previousTotalArea = land.TotalArea;
        var previousAreaUnit = land.AreaUnit;
        var previousAddress = land.Address;
        var previousLocationDescription =
            land.LocationDescription;
        var previousLatitude = land.Latitude;
        var previousLongitude = land.Longitude;
        var previousNotes = land.Notes;

        try
        {
            land.Update(
                request.Name,
                request.TenureType,
                request.TotalArea,
                request.AreaUnit,
                request.Address,
                request.LocationDescription,
                request.Latitude,
                request.Longitude,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.AreaCapacityExceeded(
                    exception.Message));
        }

        var hasChanged =
            previousName != land.Name ||
            previousTenureType != land.TenureType ||
            previousTotalArea != land.TotalArea ||
            previousAreaUnit != land.AreaUnit ||
            previousAddress != land.Address ||
            previousLocationDescription !=
                land.LocationDescription ||
            previousLatitude != land.Latitude ||
            previousLongitude != land.Longitude ||
            previousNotes != land.Notes;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public async Task<Result<Guid>> DeleteAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return Result<Guid>.Failure(
                identifierError);
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<Guid>.Failure(
                landResult.Error);
        }

        var cropCycles =
            await _cropCycleRepository.GetAllAsync(
                organizationId,
                landId: landId,
                cancellationToken: cancellationToken);

        if (cropCycles.Count > 0)
        {
            return Result<Guid>.Failure(
                LandErrors.HistoricalReferenceExists(
                    landId));
        }

        _landRepository.Remove(landResult.Value);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<Guid>.Success(landId);
    }

    public Task<Result<LandResponse>> ActivateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            landId,
            true,
            cancellationToken);
    }

    public Task<Result<LandResponse>> DeactivateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            landId,
            false,
            cancellationToken);
    }

    public async Task<Result<LandResponse>> AddPlotAsync(
        Guid organizationId,
        Guid landId,
        AddLandPlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    "Land plot request cannot be null."));
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;

        var normalizedCode =
            NormalizeCodeForComparison(
                request.Code);

        if (land.Plots.Any(plot =>
                plot.Code == normalizedCode))
        {
            return Result<LandResponse>.Failure(
                LandErrors.PlotCodeAlreadyExists(
                    landId,
                    normalizedCode));
        }

        try
        {
            land.AddPlot(
                request.Code,
                request.Name,
                request.Area,
                request.AreaUnit,
                request.GeneralCondition,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.AreaCapacityExceeded(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public async Task<Result<LandResponse>> UpdatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        UpdateLandPlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidatePlotIdentifiers(
                organizationId,
                landId,
                plotId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    "Land plot request cannot be null."));
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;

        var plot = land.Plots
            .SingleOrDefault(candidate =>
                candidate.Id == plotId);

        if (plot is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.PlotNotFound(
                    landId,
                    plotId));
        }

        var previousName = plot.Name;
        var previousArea = plot.Area;
        var previousAreaUnit = plot.AreaUnit;
        var previousGeneralCondition =
            plot.GeneralCondition;
        var previousNotes = plot.Notes;

        try
        {
            land.UpdatePlot(
                plotId,
                request.Name,
                request.Area,
                request.AreaUnit,
                request.GeneralCondition,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<LandResponse>.Failure(
                LandErrors.AreaCapacityExceeded(
                    exception.Message));
        }

        var hasChanged =
            previousName != plot.Name ||
            previousArea != plot.Area ||
            previousAreaUnit != plot.AreaUnit ||
            previousGeneralCondition !=
                plot.GeneralCondition ||
            previousNotes != plot.Notes;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public async Task<Result<LandResponse>> RemovePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidatePlotIdentifiers(
                organizationId,
                landId,
                plotId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;

        if (!land.Plots.Any(plot =>
                plot.Id == plotId))
        {
            return Result<LandResponse>.Failure(
                LandErrors.PlotNotFound(
                    landId,
                    plotId));
        }

        if (await _cropCycleRepository
            .HasAnyCycleForPlotAsync(
                organizationId,
                landId,
                plotId,
                cancellationToken))
        {
            return Result<LandResponse>.Failure(
                CropCycleErrors
                    .HistoricalReferenceExists(
                        plotId));
        }

        land.RemovePlot(plotId);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    public Task<Result<LandResponse>> ActivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default)
    {
        return SetPlotActiveStatusAsync(
            organizationId,
            landId,
            plotId,
            true,
            cancellationToken);
    }

    public Task<Result<LandResponse>> DeactivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        CancellationToken cancellationToken = default)
    {
        return SetPlotActiveStatusAsync(
            organizationId,
            landId,
            plotId,
            false,
            cancellationToken);
    }

    private async Task<Result<LandResponse>>
        SetActiveStatusAsync(
            Guid organizationId,
            Guid landId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;
        var previousStatus = land.IsActive;

        if (shouldBeActive)
        {
            land.Activate();
        }
        else
        {
            if (land.IsActive &&
                await _cropCycleRepository
                    .HasActiveCycleForLandAsync(
                        organizationId,
                        landId,
                        cancellationToken))
            {
                return Result<LandResponse>.Failure(
                    CropCycleErrors.ActiveReferenceExists(
                        "Land",
                        landId));
            }

            land.Deactivate();
        }

        if (previousStatus != land.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    private async Task<Result<LandResponse>>
        SetPlotActiveStatusAsync(
            Guid organizationId,
            Guid landId,
            Guid plotId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError =
            ValidatePlotIdentifiers(
                organizationId,
                landId,
                plotId);

        if (identifierError is not null)
        {
            return Result<LandResponse>.Failure(
                identifierError);
        }

        var landResult = await GetForUpdateAsync(
            organizationId,
            landId,
            cancellationToken);

        if (landResult.IsFailure)
        {
            return Result<LandResponse>.Failure(
                landResult.Error);
        }

        var land = landResult.Value;

        var plot = land.Plots
            .SingleOrDefault(candidate =>
                candidate.Id == plotId);

        if (plot is null)
        {
            return Result<LandResponse>.Failure(
                LandErrors.PlotNotFound(
                    landId,
                    plotId));
        }

        var previousStatus = plot.IsActive;

        if (shouldBeActive)
        {
            land.ActivatePlot(plotId);
        }
        else
        {
            if (plot.IsActive &&
                await _cropCycleRepository
                    .HasActiveCycleForPlotAsync(
                        organizationId,
                        landId,
                        plotId,
                        cancellationToken))
            {
                return Result<LandResponse>.Failure(
                    CropCycleErrors.ActiveReferenceExists(
                        "Land plot",
                        plotId));
            }

            land.DeactivatePlot(plotId);
        }

        if (previousStatus != plot.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<LandResponse>.Success(
            land.ToResponse());
    }

    private async Task<Result<Land>> GetForUpdateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken)
    {
        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<Land>.Failure(
                LandErrors.OrganizationNotFound(
                    organizationId));
        }

        var land =
            await _landRepository.GetByIdForUpdateAsync(
                organizationId,
                landId,
                cancellationToken);

        if (land is null)
        {
            return Result<Land>.Failure(
                LandErrors.NotFound(
                    organizationId,
                    landId));
        }

        return Result<Land>.Success(land);
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

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid landId)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (landId == Guid.Empty)
        {
            return LandErrors.Validation(
                "Land identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidatePlotIdentifiers(
        Guid organizationId,
        Guid landId,
        Guid plotId)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            landId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (plotId == Guid.Empty)
        {
            return LandErrors.Validation(
                "Land plot identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return LandErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }

    private static string NormalizeCodeForComparison(
        string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? string.Empty
            : code.Trim().ToUpperInvariant();
    }
}
