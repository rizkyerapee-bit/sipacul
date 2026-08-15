using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Calculations;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingWaterfallSettlementOperationProcessor :
    IProfitSharingWaterfallSettlementOperationProcessor
{
    private const string CodeConstraint =
        "UX_ProfitSharingWaterfallSettlements_Org_Cycle_Code";

    private readonly SiPaculDbContext _dbContext;

    private readonly IProfitSharingWaterfallSettlementRepository
        _settlementRepository;

    private readonly IProfitSharingSchemeAssignmentRepository
        _assignmentRepository;

    private readonly IProfitabilityReadRepository
        _profitabilityRepository;

    private readonly ICapitalContributionRepository
        _contributionRepository;

    private readonly TimeProvider _timeProvider;

    public ProfitSharingWaterfallSettlementOperationProcessor(
        SiPaculDbContext dbContext,
        IProfitSharingWaterfallSettlementRepository settlementRepository,
        IProfitSharingSchemeAssignmentRepository assignmentRepository,
        IProfitabilityReadRepository profitabilityRepository,
        ICapitalContributionRepository contributionRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(settlementRepository);
        ArgumentNullException.ThrowIfNull(assignmentRepository);
        ArgumentNullException.ThrowIfNull(profitabilityRepository);
        ArgumentNullException.ThrowIfNull(contributionRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _dbContext = dbContext;
        _settlementRepository = settlementRepository;
        _assignmentRepository = assignmentRepository;
        _profitabilityRepository = profitabilityRepository;
        _contributionRepository = contributionRepository;
        _timeProvider = timeProvider;
    }

    public Task<ProfitSharingWaterfallSettlementOperationResult>
        FinalizeAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            DateOnly settlementDate,
            string? notes,
            CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            () => FinalizeWithinTransactionAsync(
                organizationId,
                cropCycleId,
                code,
                settlementDate,
                notes,
                cancellationToken),
            code,
            cancellationToken);
    }

    public Task<ProfitSharingWaterfallSettlementOperationResult>
        VoidAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            string voidReason,
            CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(
            () => VoidWithinTransactionAsync(
                organizationId,
                cropCycleId,
                settlementId,
                voidReason,
                cancellationToken),
            code: null,
            cancellationToken: cancellationToken);
    }

    private async Task<ProfitSharingWaterfallSettlementOperationResult>
        ExecuteAsync(
            Func<Task<ProfitSharingWaterfallSettlementOperationResult>>
                operation,
            string? code,
            CancellationToken cancellationToken)
    {
        var executionStrategy =
            _dbContext.Database.CreateExecutionStrategy();

        try
        {
            return await executionStrategy.ExecuteAsync(
                async () =>
                {
                    _dbContext.ChangeTracker.Clear();

                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable,
                            cancellationToken);

                    var result = await operation();

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);
                        return result;
                    }

                    await transaction.CommitAsync(cancellationToken);
                    return result;
                });
        }
        catch (DbUpdateException exception)
            when (FindPostgresException(exception)?.SqlState ==
                  PostgresErrorCodes.UniqueViolation)
        {
            var postgresException = FindPostgresException(exception);

            return ProfitSharingWaterfallSettlementOperationResult.Failed(
                string.Equals(
                    postgresException?.ConstraintName,
                    CodeConstraint,
                    StringComparison.Ordinal)
                    ? ProfitSharingWaterfallSettlementFailure
                        .CodeAlreadyExists
                    : ProfitSharingWaterfallSettlementFailure
                        .ActiveSettlementExists,
                code: code);
        }
        catch (Exception exception)
            when (IsSerializationFailure(exception))
        {
            return ProfitSharingWaterfallSettlementOperationResult.Failed(
                ProfitSharingWaterfallSettlementFailure
                    .ConcurrencyConflict);
        }
    }

    private async Task<ProfitSharingWaterfallSettlementOperationResult>
        FinalizeWithinTransactionAsync(
            Guid organizationId,
            Guid cropCycleId,
            string code,
            DateOnly settlementDate,
            string? notes,
            CancellationToken cancellationToken)
    {
        var cropCycle = await _dbContext
            .LockCropCycleForProfitSharingAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CropCycleNotFound);
        }

        if (cropCycle.Status is not CropCycleStatus.Completed and
            not CropCycleStatus.Cancelled)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CropCycleNotTerminal);
        }

        if (await ActiveSettlementExistsAsync(
                organizationId,
                cropCycleId,
                cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .ActiveSettlementExists);
        }

        var readinessFailure = await ValidateReadinessAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (readinessFailure is not null)
        {
            return readinessFailure;
        }

        var assignment = await _assignmentRepository
            .GetByCropCycleAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (assignment is null)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .AssignmentNotFound);
        }

        var sourceSnapshot = await _profitabilityRepository.GetAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (sourceSnapshot is null)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CropCycleNotFound);
        }

        var preliminaryFailure = ValidateProfitabilitySnapshot(
            sourceSnapshot);

        if (preliminaryFailure is not null)
        {
            return preliminaryFailure;
        }

        var contributions = await _contributionRepository.GetAllAsync(
            organizationId,
            cropCycleId,
            status: CapitalContributionStatus.Confirmed,
            cancellationToken: cancellationToken);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var sourceCalculation =
            ProfitSharingWaterfallSourceCalculator.Calculate(
                assignment,
                sourceSnapshot,
                contributions,
                now);

        if (!sourceCalculation.IsSuccess)
        {
            return MapSourceCalculationFailure(sourceCalculation);
        }

        ProfitSharingWaterfallSettlement settlement;

        try
        {
            settlement =
                ProfitSharingWaterfallSettlement.CreateFinalized(
                    organizationId,
                    cropCycleId,
                    code,
                    settlementDate,
                    assignment,
                    sourceCalculation.Profitability!,
                    sourceCalculation.Calculation!,
                    notes,
                    now);
        }
        catch (ArgumentException exception)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure.Validation,
                message: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .SourceDataChanged,
                message: exception.Message);
        }
        catch (OverflowException exception)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CalculationUnavailable,
                message: exception.Message);
        }

        if (await _settlementRepository.CodeExistsAsync(
                organizationId,
                cropCycleId,
                settlement.Code,
                cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CodeAlreadyExists,
                code: settlement.Code);
        }

        _settlementRepository.Add(settlement);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProfitSharingWaterfallSettlementOperationResult
            .Succeeded(settlement);
    }

    private async Task<ProfitSharingWaterfallSettlementOperationResult>
        VoidWithinTransactionAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            string voidReason,
            CancellationToken cancellationToken)
    {
        var cropCycle = await _dbContext
            .LockCropCycleForProfitSharingAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CropCycleNotFound);
        }

        var settlement = await _settlementRepository
            .GetByIdForUpdateAsync(
                organizationId,
                cropCycleId,
                settlementId,
                cancellationToken);

        if (settlement is null)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .SettlementNotFound);
        }

        try
        {
            settlement.Void(
                voidReason,
                _timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (ArgumentException exception)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure.Validation,
                message: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure.InvalidStatus,
                message: exception.Message);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return ProfitSharingWaterfallSettlementOperationResult
            .Succeeded(settlement);
    }

    private async Task<bool> ActiveSettlementExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        var legacyExists = await _dbContext
            .Set<ProfitSharingSettlement>()
            .AsNoTracking()
            .AnyAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted,
                cancellationToken);

        if (legacyExists)
        {
            return true;
        }

        return await _dbContext
            .Set<ProfitSharingWaterfallSettlement>()
            .AsNoTracking()
            .AnyAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Status ==
                        ProfitSharingWaterfallSettlementStatus.Finalized &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    private async Task<ProfitSharingWaterfallSettlementOperationResult?>
        ValidateReadinessAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<CultivationActivity>()
                .AsNoTracking()
                .AnyAsync(
                    activity =>
                        activity.OrganizationId == organizationId &&
                        activity.CropCycleId == cropCycleId &&
                        (activity.Status ==
                            CultivationActivityStatus.Planned ||
                         activity.Status ==
                            CultivationActivityStatus.InProgress) &&
                        !activity.IsDeleted,
                    cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .ActiveActivityExists);
        }

        if (await _dbContext.Set<HarvestBatch>()
                .AsNoTracking()
                .AnyAsync(
                    harvest =>
                        harvest.OrganizationId == organizationId &&
                        harvest.CropCycleId == cropCycleId &&
                        harvest.Status == HarvestBatchStatus.Draft &&
                        !harvest.IsDeleted,
                    cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .DraftHarvestExists);
        }

        if (await DraftSaleExistsAsync(
                organizationId,
                cropCycleId,
                cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .DraftSaleExists);
        }

        if (await _dbContext.Set<CultivationExpense>()
                .AsNoTracking()
                .AnyAsync(
                    expense =>
                        expense.OrganizationId == organizationId &&
                        expense.CropCycleId == cropCycleId &&
                        expense.Status ==
                            CultivationExpenseStatus.Draft &&
                        !expense.IsDeleted,
                    cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .DraftExpenseExists);
        }

        if (await _dbContext.Set<CapitalContribution>()
                .AsNoTracking()
                .AnyAsync(
                    contribution =>
                        contribution.OrganizationId == organizationId &&
                        contribution.CropCycleId == cropCycleId &&
                        contribution.Status ==
                            CapitalContributionStatus.Draft &&
                        !contribution.IsDeleted,
                    cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .DraftContributionExists);
        }

        if (await DraftPaymentExistsAsync(
                organizationId,
                cropCycleId,
                cancellationToken))
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .DraftPaymentExists);
        }

        return null;
    }

    private async Task<bool> DraftSaleExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        return await (
            from line in _dbContext.Set<SaleLine>().AsNoTracking()
            join sale in _dbContext.Set<Sale>().AsNoTracking()
                on new { line.OrganizationId, line.SaleId }
                equals new
                {
                    sale.OrganizationId,
                    SaleId = sale.Id
                }
            where line.OrganizationId == organizationId &&
                  line.CropCycleIdSnapshot == cropCycleId &&
                  sale.Status == SaleStatus.Draft &&
                  !sale.IsDeleted
            select line.Id)
            .AnyAsync(cancellationToken);
    }

    private async Task<bool> DraftPaymentExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        return await (
            from payment in _dbContext.Set<SalePayment>().AsNoTracking()
            join sale in _dbContext.Set<Sale>().AsNoTracking()
                on new { payment.OrganizationId, payment.SaleId }
                equals new
                {
                    sale.OrganizationId,
                    SaleId = sale.Id
                }
            join line in _dbContext.Set<SaleLine>().AsNoTracking()
                on new
                {
                    sale.OrganizationId,
                    SaleId = sale.Id
                }
                equals new { line.OrganizationId, line.SaleId }
            where payment.OrganizationId == organizationId &&
                  line.CropCycleIdSnapshot == cropCycleId &&
                  payment.Status == SalePaymentStatus.Draft &&
                  !payment.IsDeleted &&
                  !sale.IsDeleted
            select payment.Id)
            .AnyAsync(cancellationToken);
    }

    private static ProfitSharingWaterfallSettlementOperationResult?
        ValidateProfitabilitySnapshot(
            ProfitabilitySourceSnapshot snapshot)
    {
        var outstandingReceivable = RoundMoney(
            snapshot.RecognizedRevenue - snapshot.CollectedRevenue);

        if (outstandingReceivable != 0)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .OutstandingReceivableExists,
                outstandingReceivable: outstandingReceivable);
        }

        if (snapshot.AvailableHarvestQuantity != 0)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .UnsoldHarvestExists);
        }

        var totalCost = RoundMoney(
            snapshot.ActivityResourceCost +
            snapshot.ManualExpenseCost);

        var totalCapital = RoundMoney(
            snapshot.ConfirmedInvestorCapital +
            snapshot.ConfirmedPartnerCapital);

        if (totalCost <= 0)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .ZeroCostUnsupported);
        }

        if (totalCapital != totalCost)
        {
            return Failed(
                ProfitSharingWaterfallSettlementFailure
                    .CapitalDoesNotMatchCost,
                totalCapital: totalCapital,
                totalCost: totalCost);
        }

        return null;
    }

    private static ProfitSharingWaterfallSettlementOperationResult
        MapSourceCalculationFailure(
            ProfitSharingWaterfallSourceCalculation result)
    {
        var failure = result.Failure switch
        {
            ProfitSharingWaterfallSourceFailure
                .CapitalIdentityConflict =>
                ProfitSharingWaterfallSettlementFailure
                    .CapitalIdentityConflict,

            ProfitSharingWaterfallSourceFailure
                .CapitalNotInScheme =>
                ProfitSharingWaterfallSettlementFailure
                    .CapitalNotInScheme,

            ProfitSharingWaterfallSourceFailure
                .CapitalRoleMismatch =>
                ProfitSharingWaterfallSettlementFailure
                    .CapitalRoleMismatch,

            ProfitSharingWaterfallSourceFailure
                .SourceDataChanged =>
                ProfitSharingWaterfallSettlementFailure
                    .SourceDataChanged,

            _ => ProfitSharingWaterfallSettlementFailure
                .CalculationUnavailable
        };

        return Failed(
            failure,
            contributorCode: result.ContributorCode,
            message: result.Message);
    }

    private static ProfitSharingWaterfallSettlementOperationResult Failed(
        ProfitSharingWaterfallSettlementFailure failure,
        string? code = null,
        string? contributorCode = null,
        decimal outstandingReceivable = 0,
        decimal totalCapital = 0,
        decimal totalCost = 0,
        string? message = null)
    {
        return ProfitSharingWaterfallSettlementOperationResult.Failed(
            failure,
            code,
            contributorCode,
            outstandingReceivable,
            totalCapital,
            totalCost,
            message);
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static bool IsSerializationFailure(Exception exception)
    {
        var postgresException = FindPostgresException(exception);

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    private static PostgresException? FindPostgresException(
        Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is PostgresException postgresException)
            {
                return postgresException;
            }

            current = current.InnerException;
        }

        return null;
    }
}
