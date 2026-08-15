using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Mappings;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Services;

public sealed class ProfitSharingWaterfallSettlementService :
    IProfitSharingWaterfallSettlementService
{
    private readonly IProfitSharingWaterfallSettlementRepository
        _settlementRepository;

    private readonly IProfitSharingWaterfallSettlementOperationProcessor
        _operationProcessor;

    private readonly ICropCycleRepository _cropCycleRepository;

    private readonly IOrganizationRepository _organizationRepository;

    public ProfitSharingWaterfallSettlementService(
        IProfitSharingWaterfallSettlementRepository settlementRepository,
        IProfitSharingWaterfallSettlementOperationProcessor
            operationProcessor,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository)
    {
        ArgumentNullException.ThrowIfNull(settlementRepository);
        ArgumentNullException.ThrowIfNull(operationProcessor);
        ArgumentNullException.ThrowIfNull(cropCycleRepository);
        ArgumentNullException.ThrowIfNull(organizationRepository);

        _settlementRepository = settlementRepository;
        _operationProcessor = operationProcessor;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository = organizationRepository;
    }

    public async Task<Result<ProfitSharingWaterfallSettlementResponse>>
        FinalizeAsync(
            Guid organizationId,
            Guid cropCycleId,
            FinalizeProfitSharingWaterfallSettlementRequest request,
            CancellationToken cancellationToken = default)
    {
        var validationError = ValidateFinalizeRequest(
            organizationId,
            cropCycleId,
            request);

        if (validationError is not null)
        {
            return Failure(validationError);
        }

        var parentError = await ValidateParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentError is not null)
        {
            return Failure(parentError);
        }

        var result = await _operationProcessor.FinalizeAsync(
            organizationId,
            cropCycleId,
            request.Code,
            request.SettlementDate,
            request.Notes,
            cancellationToken);

        return MapOperationResult(
            cropCycleId,
            Guid.Empty,
            result);
    }

    public async Task<
        Result<IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingWaterfallSettlementFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>
                .Failure(identifierError);
        }

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>
                .Failure(filterError);
        }

        var parentError = await ValidateParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentError is not null)
        {
            return Result<
                IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>
                .Failure(parentError);
        }

        filter ??= new ProfitSharingWaterfallSettlementFilter();

        var settlements = await _settlementRepository.GetAllAsync(
            organizationId,
            cropCycleId,
            filter.Status,
            filter.SettlementDateFrom,
            filter.SettlementDateTo,
            cancellationToken);

        IReadOnlyList<ProfitSharingWaterfallSettlementResponse> response =
            settlements
                .Select(settlement => settlement.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>
            .Success(response);
    }

    public async Task<Result<ProfitSharingWaterfallSettlementResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            settlementId);

        if (identifierError is not null)
        {
            return Failure(identifierError);
        }

        var parentError = await ValidateParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentError is not null)
        {
            return Failure(parentError);
        }

        var settlement = await _settlementRepository.GetByIdAsync(
            organizationId,
            cropCycleId,
            settlementId,
            cancellationToken);

        if (settlement is null)
        {
            return Failure(
                ProfitSharingWaterfallSettlementErrors.NotFound(
                    settlementId));
        }

        return Success(settlement);
    }

    public async Task<Result<ProfitSharingWaterfallSettlementResponse>>
        VoidAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            VoidProfitSharingWaterfallSettlementRequest request,
            CancellationToken cancellationToken = default)
    {
        var validationError = ValidateVoidRequest(
            organizationId,
            cropCycleId,
            settlementId,
            request);

        if (validationError is not null)
        {
            return Failure(validationError);
        }

        var parentError = await ValidateParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentError is not null)
        {
            return Failure(parentError);
        }

        var result = await _operationProcessor.VoidAsync(
            organizationId,
            cropCycleId,
            settlementId,
            request.VoidReason,
            cancellationToken);

        return MapOperationResult(
            cropCycleId,
            settlementId,
            result);
    }

    private async Task<Error?> ValidateParentAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        if (await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken) is null)
        {
            return ProfitSharingWaterfallSettlementErrors
                .OrganizationNotFound(organizationId);
        }

        if (await _cropCycleRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken) is null)
        {
            return ProfitSharingWaterfallSettlementErrors
                .CropCycleNotFound(cropCycleId);
        }

        return null;
    }

    private static Result<ProfitSharingWaterfallSettlementResponse>
        MapOperationResult(
            Guid cropCycleId,
            Guid settlementId,
            ProfitSharingWaterfallSettlementOperationResult result)
    {
        if (result.IsSuccess)
        {
            return Success(result.Settlement!);
        }

        var error = result.Failure switch
        {
            ProfitSharingWaterfallSettlementFailure.CropCycleNotFound =>
                ProfitSharingWaterfallSettlementErrors
                    .CropCycleNotFound(cropCycleId),

            ProfitSharingWaterfallSettlementFailure.AssignmentNotFound =>
                ProfitSharingWaterfallSettlementErrors
                    .AssignmentNotFound(cropCycleId),

            ProfitSharingWaterfallSettlementFailure.SettlementNotFound =>
                ProfitSharingWaterfallSettlementErrors
                    .NotFound(settlementId),

            ProfitSharingWaterfallSettlementFailure.CodeAlreadyExists =>
                ProfitSharingWaterfallSettlementErrors
                    .CodeAlreadyExists(result.Code ?? string.Empty),

            ProfitSharingWaterfallSettlementFailure
                .ActiveSettlementExists =>
                ProfitSharingWaterfallSettlementErrors
                    .ActiveSettlementExists(cropCycleId),

            ProfitSharingWaterfallSettlementFailure
                .CropCycleNotTerminal =>
                ProfitSharingWaterfallSettlementErrors
                    .CropCycleNotTerminal(),

            ProfitSharingWaterfallSettlementFailure
                .ActiveActivityExists =>
                ProfitSharingWaterfallSettlementErrors
                    .ActiveActivityExists(),

            ProfitSharingWaterfallSettlementFailure
                .DraftHarvestExists =>
                ProfitSharingWaterfallSettlementErrors
                    .DraftHarvestExists(),

            ProfitSharingWaterfallSettlementFailure
                .UnsoldHarvestExists =>
                ProfitSharingWaterfallSettlementErrors
                    .UnsoldHarvestExists(),

            ProfitSharingWaterfallSettlementFailure.DraftSaleExists =>
                ProfitSharingWaterfallSettlementErrors
                    .DraftSaleExists(),

            ProfitSharingWaterfallSettlementFailure
                .OutstandingReceivableExists =>
                ProfitSharingWaterfallSettlementErrors
                    .OutstandingReceivableExists(
                        result.OutstandingReceivable),

            ProfitSharingWaterfallSettlementFailure
                .DraftExpenseExists =>
                ProfitSharingWaterfallSettlementErrors
                    .DraftExpenseExists(),

            ProfitSharingWaterfallSettlementFailure
                .DraftContributionExists =>
                ProfitSharingWaterfallSettlementErrors
                    .DraftContributionExists(),

            ProfitSharingWaterfallSettlementFailure
                .DraftPaymentExists =>
                ProfitSharingWaterfallSettlementErrors
                    .DraftPaymentExists(),

            ProfitSharingWaterfallSettlementFailure
                .CapitalDoesNotMatchCost =>
                ProfitSharingWaterfallSettlementErrors
                    .CapitalDoesNotMatchCost(
                        result.TotalCapital,
                        result.TotalCost),

            ProfitSharingWaterfallSettlementFailure
                .ZeroCostUnsupported =>
                ProfitSharingWaterfallSettlementErrors
                    .ZeroCostUnsupported(),

            ProfitSharingWaterfallSettlementFailure
                .CapitalIdentityConflict =>
                ProfitSharingWaterfallSettlementErrors
                    .CapitalIdentityConflict(
                        result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSettlementFailure
                .CapitalNotInScheme =>
                ProfitSharingWaterfallSettlementErrors
                    .CapitalNotInScheme(
                        result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSettlementFailure
                .CapitalRoleMismatch =>
                ProfitSharingWaterfallSettlementErrors
                    .CapitalRoleMismatch(
                        result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSettlementFailure
                .SourceDataChanged =>
                ProfitSharingWaterfallSettlementErrors
                    .SourceDataChanged(),

            ProfitSharingWaterfallSettlementFailure
                .CalculationUnavailable =>
                ProfitSharingWaterfallSettlementErrors
                    .CalculationUnavailable(
                        result.Message ?? "Source data is invalid."),

            ProfitSharingWaterfallSettlementFailure.InvalidStatus =>
                ProfitSharingWaterfallSettlementErrors.InvalidStatus(
                    result.Message ??
                    "The settlement status does not allow this " +
                    "operation."),

            ProfitSharingWaterfallSettlementFailure.Validation =>
                ProfitSharingWaterfallSettlementErrors.Validation(
                    result.Message ?? "The request is invalid."),

            _ => ProfitSharingWaterfallSettlementErrors
                .ConcurrencyConflict()
        };

        return Failure(error);
    }

    private static Error? ValidateFinalizeRequest(
        Guid organizationId,
        Guid cropCycleId,
        FinalizeProfitSharingWaterfallSettlementRequest? request)
    {
        var error = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (error is not null)
        {
            return error;
        }

        if (request is null)
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Waterfall finalization request cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Settlement code is required.");
        }

        if (request.SettlementDate == default)
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Settlement date is required.");
        }

        return null;
    }

    private static Error? ValidateVoidRequest(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        VoidProfitSharingWaterfallSettlementRequest? request)
    {
        var error = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            settlementId);

        if (error is not null)
        {
            return error;
        }

        if (request is null ||
            string.IsNullOrWhiteSpace(request.VoidReason))
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Void reason is required.");
        }

        return null;
    }

    private static Error? ValidateFilter(
        ProfitSharingWaterfallSettlementFilter? filter)
    {
        if (filter?.Status is not null &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Settlement status is invalid.");
        }

        if (filter?.SettlementDateFrom is not null &&
            filter.SettlementDateTo is not null &&
            filter.SettlementDateFrom > filter.SettlementDateTo)
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Settlement date range is invalid.");
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
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (settlementId.HasValue &&
            settlementId.Value == Guid.Empty)
        {
            return ProfitSharingWaterfallSettlementErrors.Validation(
                "Settlement identifier cannot be empty.");
        }

        return null;
    }

    private static Result<ProfitSharingWaterfallSettlementResponse>
        Success(ProfitSharingWaterfallSettlement settlement)
    {
        return Result<ProfitSharingWaterfallSettlementResponse>.Success(
            settlement.ToResponse());
    }

    private static Result<ProfitSharingWaterfallSettlementResponse>
        Failure(Error error)
    {
        return Result<ProfitSharingWaterfallSettlementResponse>.Failure(
            error);
    }
}
