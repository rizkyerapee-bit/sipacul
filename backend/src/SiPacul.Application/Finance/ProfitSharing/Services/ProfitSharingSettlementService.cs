using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Mappings;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Services;

public sealed class ProfitSharingSettlementService :
    IProfitSharingSettlementService
{
    private readonly IProfitSharingSettlementRepository
        _settlementRepository;

    private readonly ICapitalContributionRepository
        _contributionRepository;

    private readonly IProfitabilityService
        _profitabilityService;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public ProfitSharingSettlementService(
        IProfitSharingSettlementRepository
            settlementRepository,
        ICapitalContributionRepository
            contributionRepository,
        IProfitabilityService profitabilityService,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _settlementRepository =
            settlementRepository;

        _contributionRepository =
            contributionRepository;

        _profitabilityService =
            profitabilityService;

        _cropCycleRepository =
            cropCycleRepository;

        _organizationRepository =
            organizationRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfitSharingSettlementResponse>>
        CreateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateRequest(
                organizationId,
                cropCycleId,
                request,
                "Profit sharing settlement request " +
                "cannot be null.");

        if (requestError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(requestError);
        }

        var parentError =
            await ValidateParentAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (parentError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(parentError);
        }

        var calculationResult =
            await BuildCalculationAsync(
                organizationId,
                cropCycleId,
                request.ManagingPartnerCode,
                request.ManagingPartnerName,
                cancellationToken);

        if (calculationResult.IsFailure)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(calculationResult.Error);
        }

        ProfitSharingSettlement settlement;

        try
        {
            settlement =
                ProfitSharingSettlement.CreateDraft(
                    organizationId,
                    cropCycleId,
                    request.Code,
                    request.SettlementDate,
                    request.ManagingPartnerCode,
                    request.ManagingPartnerName,
                    calculationResult.Value,
                    request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }
        catch (OverflowException)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }

        if (await _settlementRepository.CodeExistsAsync(
                organizationId,
                cropCycleId,
                settlement.Code,
                cancellationToken))
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors
                        .CodeAlreadyExists(
                            settlement.Code));
        }

        _settlementRepository.Add(settlement);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProfitSharingSettlementResponse>
            .Success(settlement.ToResponse());
    }

    public async Task<
        Result<
            IReadOnlyList<
                ProfitSharingSettlementResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingSettlementFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>
                .Failure(identifierError);
        }

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>
                .Failure(filterError);
        }

        var parentError =
            await ValidateParentAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (parentError is not null)
        {
            return Result<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>
                .Failure(parentError);
        }

        filter ??= new ProfitSharingSettlementFilter();

        var settlements =
            await _settlementRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                filter.Status,
                filter.SettlementDateFrom,
                filter.SettlementDateTo,
                NormalizeOptionalCode(
                    filter.ManagingPartnerCode),
                cancellationToken);

        IReadOnlyList<
            ProfitSharingSettlementResponse> responses =
            settlements
                .Select(settlement =>
                    settlement.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<
                ProfitSharingSettlementResponse>>
            .Success(responses);
    }

    public async Task<Result<ProfitSharingSettlementResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId,
                settlementId);

        if (identifierError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(identifierError);
        }

        var parentError =
            await ValidateParentAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (parentError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(parentError);
        }

        var settlement =
            await _settlementRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                settlementId,
                cancellationToken);

        if (settlement is null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors.NotFound(
                        settlementId));
        }

        return Result<ProfitSharingSettlementResponse>
            .Success(settlement.ToResponse());
    }

    public async Task<Result<ProfitSharingSettlementResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            UpdateProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateRequest(
                organizationId,
                cropCycleId,
                settlementId,
                request,
                "Profit sharing settlement update request " +
                "cannot be null.");

        if (requestError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(requestError);
        }

        var mutationResult =
            await GetForMutationAsync(
                organizationId,
                cropCycleId,
                settlementId,
                cancellationToken);

        if (mutationResult.IsFailure)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(mutationResult.Error);
        }

        var settlement = mutationResult.Value;

        var previousDate = settlement.SettlementDate;
        var previousNotes = settlement.Notes;

        try
        {
            settlement.UpdateDraft(
                request.SettlementDate,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        if (previousDate != settlement.SettlementDate ||
            previousNotes != settlement.Notes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<ProfitSharingSettlementResponse>
            .Success(settlement.ToResponse());
    }

    public async Task<Result<ProfitSharingSettlementResponse>>
        VoidAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            VoidProfitSharingSettlementRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError =
            ValidateRequest(
                organizationId,
                cropCycleId,
                settlementId,
                request,
                "Profit sharing settlement void request " +
                "cannot be null.");

        if (requestError is not null)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(requestError);
        }

        var mutationResult =
            await GetForMutationAsync(
                organizationId,
                cropCycleId,
                settlementId,
                cancellationToken);

        if (mutationResult.IsFailure)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(mutationResult.Error);
        }

        var settlement = mutationResult.Value;

        try
        {
            settlement.Void(request.VoidReason);
        }
        catch (ArgumentException exception)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<ProfitSharingSettlementResponse>
                .Failure(
                    ProfitSharingSettlementErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProfitSharingSettlementResponse>
            .Success(settlement.ToResponse());
    }

    private async Task<
        Result<ProfitSharingCalculationResult>>
        BuildCalculationAsync(
            Guid organizationId,
            Guid cropCycleId,
            string managingPartnerCode,
            string managingPartnerName,
            CancellationToken cancellationToken)
    {
        var profitabilityResult =
            await _profitabilityService
                .GetCropCycleReportAsync(
                    organizationId,
                    cropCycleId,
                    cancellationToken);

        if (profitabilityResult.IsFailure)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(profitabilityResult.Error);
        }

        CropCycleProfitabilityReport report;

        try
        {
            report =
                ToDomainReport(
                    profitabilityResult.Value);
        }
        catch (ArgumentException)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }
        catch (InvalidOperationException)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }
        catch (OverflowException)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }

        var contributions =
            await _contributionRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                CapitalContributionStatus.Confirmed,
                null,
                null,
                null,
                null,
                null,
                cancellationToken);

        var confirmedContributions =
            contributions
                .Where(contribution =>
                    contribution.IsConfirmedCapital)
                .ToArray();

        var actualInvestorCapital =
            RoundMoney(
                confirmedContributions
                    .Where(contribution =>
                        contribution.ContributorRole ==
                            CapitalContributorRole.Investor)
                    .Sum(contribution =>
                        contribution.Amount));

        var actualPartnerCapital =
            RoundMoney(
                confirmedContributions
                    .Where(contribution =>
                        contribution.ContributorRole ==
                            CapitalContributorRole.Partner)
                    .Sum(contribution =>
                        contribution.Amount));

        if (actualInvestorCapital !=
                report.ConfirmedInvestorCapital ||
            actualPartnerCapital !=
                report.ConfirmedPartnerCapital)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }

        if (report.TotalCultivationCost <= 0)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .ZeroCostUnsupported());
        }

        if (report.TotalConfirmedCapital !=
            report.TotalCultivationCost)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .CapitalDoesNotMatchCost(
                            report.TotalConfirmedCapital,
                            report.TotalCultivationCost));
        }

        var contributorInputs =
            confirmedContributions
                .Select(contribution =>
                    new ProfitSharingContributorInput(
                        contribution.ContributorCode,
                        contribution.ContributorName,
                        contribution.ContributorRole,
                        contribution.Amount))
                .ToArray();

        try
        {
            return Result<ProfitSharingCalculationResult>
                .Success(
                    ProfitSharingCalculator.Calculate(
                        report,
                        managingPartnerCode,
                        managingPartnerName,
                        contributorInputs));
        }
        catch (ArgumentException exception)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }
        catch (OverflowException)
        {
            return Result<ProfitSharingCalculationResult>
                .Failure(
                    ProfitSharingSettlementErrors
                        .SourceDataChanged());
        }
    }

    private async Task<Error?> ValidateParentAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return ProfitSharingSettlementErrors
                .OrganizationNotFound(
                    organizationId);
        }

        var cropCycle =
            await _cropCycleRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return ProfitSharingSettlementErrors
                .CropCycleNotFound(
                    cropCycleId);
        }

        return null;
    }

    private async Task<Result<ProfitSharingSettlement>>
        GetForMutationAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken)
    {
        var parentError =
            await ValidateParentAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (parentError is not null)
        {
            return Result<ProfitSharingSettlement>
                .Failure(parentError);
        }

        var settlement =
            await _settlementRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    settlementId,
                    cancellationToken);

        if (settlement is null)
        {
            return Result<ProfitSharingSettlement>
                .Failure(
                    ProfitSharingSettlementErrors.NotFound(
                        settlementId));
        }

        return Result<ProfitSharingSettlement>
            .Success(settlement);
    }

    private static CropCycleProfitabilityReport
        ToDomainReport(
            CropCycleProfitabilityResponse response)
    {
        return CropCycleProfitabilityReport.Calculate(
            new CropCycleProfitabilityInput(
                response.OrganizationId,
                response.CropCycleId,
                response.CropCycleCode,
                response.CropCycleName,
                response.CommodityIdSnapshot,
                response.CommodityCodeSnapshot,
                response.CommodityNameSnapshot,
                response.RecognizedRevenue,
                response.CollectedRevenue,
                response.ActivityResourceCost,
                response.ManualExpenseCost,
                response.ConfirmedInvestorCapital,
                response.ConfirmedPartnerCapital,
                response.AvailableHarvestQuantity,
                response.GeneratedAt));
    }

    private static Error? ValidateFilter(
        ProfitSharingSettlementFilter? filter)
    {
        if (filter is null)
        {
            return null;
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return ProfitSharingSettlementErrors.Validation(
                "Profit sharing settlement status is unsupported.");
        }

        if (filter.SettlementDateFrom.HasValue &&
            filter.SettlementDateTo.HasValue &&
            filter.SettlementDateFrom.Value >
                filter.SettlementDateTo.Value)
        {
            return ProfitSharingSettlementErrors.Validation(
                "Settlement date from cannot be later than " +
                "settlement date to.");
        }

        if (!string.IsNullOrWhiteSpace(
                filter.ManagingPartnerCode) &&
            filter.ManagingPartnerCode.Trim().Length >
                ProfitSharingSettlement
                    .MaxManagingPartnerCodeLength)
        {
            return ProfitSharingSettlementErrors.Validation(
                "Managing partner code is too long.");
        }

        return null;
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
            return ProfitSharingSettlementErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                cropCycleId,
                settlementId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return ProfitSharingSettlementErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid? settlementId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return ProfitSharingSettlementErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return ProfitSharingSettlementErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (settlementId.HasValue &&
            settlementId.Value == Guid.Empty)
        {
            return ProfitSharingSettlementErrors.Validation(
                "Profit sharing settlement identifier " +
                "cannot be empty.");
        }

        return null;
    }

    private static string? NormalizeOptionalCode(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim().ToUpperInvariant();
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }
}
