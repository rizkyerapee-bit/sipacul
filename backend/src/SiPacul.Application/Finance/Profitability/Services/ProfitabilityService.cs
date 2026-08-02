using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Mappings;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Profitability.Services;

public sealed class ProfitabilityService :
    IProfitabilityService
{
    private readonly IProfitabilityReadRepository
        _readRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly TimeProvider _timeProvider;

    public ProfitabilityService(
        IProfitabilityReadRepository readRepository,
        IOrganizationRepository organizationRepository,
        TimeProvider timeProvider)
    {
        _readRepository = readRepository;
        _organizationRepository = organizationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<CropCycleProfitabilityResponse>>
        GetCropCycleReportAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId);

        if (identifierError is not null)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(identifierError);
        }

        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors
                        .OrganizationNotFound(
                            organizationId));
        }

        ProfitabilitySourceSnapshot? snapshot;

        try
        {
            snapshot =
                await _readRepository.GetAsync(
                    organizationId,
                    cropCycleId,
                    cancellationToken);
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }
        catch (OverflowException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }

        if (snapshot is null)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.CropCycleNotFound(
                        cropCycleId));
        }

        try
        {
            var report =
                CropCycleProfitabilityReport.Calculate(
                    snapshot.ToInput(
                        _timeProvider
                            .GetUtcNow()
                            .UtcDateTime));

            return Result<CropCycleProfitabilityResponse>
                .Success(
                    report.ToResponse(
                        snapshot.HarvestQuantityUnit));
        }
        catch (ArgumentException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }
        catch (OverflowException exception)
        {
            return Result<CropCycleProfitabilityResponse>
                .Failure(
                    ProfitabilityErrors.SourceDataInvalid(
                        exception.Message));
        }
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId)
    {
        if (organizationId == Guid.Empty)
        {
            return ProfitabilityErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return ProfitabilityErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        return null;
    }
}
