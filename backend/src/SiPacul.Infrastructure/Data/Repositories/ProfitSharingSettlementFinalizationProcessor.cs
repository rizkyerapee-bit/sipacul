using System.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Services;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Finance.ProfitSharing;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class
    ProfitSharingSettlementFinalizationProcessor :
    IProfitSharingSettlementFinalizationProcessor
{
    private readonly SiPaculDbContext _dbContext;

    private readonly IProfitSharingSettlementRepository
        _settlementRepository;

    private readonly ICapitalContributionRepository
        _contributionRepository;

    private readonly IProfitabilityService
        _profitabilityService;

    public ProfitSharingSettlementFinalizationProcessor(
        SiPaculDbContext dbContext,
        IProfitSharingSettlementRepository
            settlementRepository,
        ICapitalContributionRepository
            contributionRepository,
        IProfitabilityService profitabilityService)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(
            settlementRepository);
        ArgumentNullException.ThrowIfNull(
            contributionRepository);
        ArgumentNullException.ThrowIfNull(
            profitabilityService);

        _dbContext = dbContext;
        _settlementRepository =
            settlementRepository;
        _contributionRepository =
            contributionRepository;
        _profitabilityService =
            profitabilityService;
    }

    public async Task<ProfitSharingFinalizationResult>
        FinalizeAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default)
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
                        await _dbContext.Database
                            .BeginTransactionAsync(
                                IsolationLevel.Serializable,
                                cancellationToken);

                    var result =
                        await FinalizeWithinTransactionAsync(
                            organizationId,
                            cropCycleId,
                            settlementId,
                            cancellationToken);

                    if (!result.IsSuccess)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return result;
                    }

                    await transaction.CommitAsync(
                        cancellationToken);

                    return result;
                });
        }
        catch (DbUpdateException exception)
            when (IsActiveSettlementUniqueViolation(
                exception))
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .ActiveSettlementExists);
        }
        catch (Exception exception)
            when (IsSerializationFailure(exception))
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .ConcurrencyConflict);
        }
    }

    private async Task<ProfitSharingFinalizationResult>
        FinalizeWithinTransactionAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken)
    {
        var settlement =
            await _settlementRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    settlementId,
                    cancellationToken);

        if (settlement is null)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .SettlementNotFound);
        }

        if (settlement.Status !=
            ProfitSharingSettlementStatus.Draft)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .InvalidStatus,
                message:
                    "Only a draft settlement can be finalized.");
        }

        if (await _settlementRepository
                .HasActiveFinalizedAsync(
                    organizationId,
                    cropCycleId,
                    settlement.Id,
                    cancellationToken))
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .ActiveSettlementExists);
        }

        var cropCycle = await _dbContext
            .LockCropCycleForProfitSharingAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle?.Status is not
                CropCycleStatus.Completed and
            not CropCycleStatus.Cancelled)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .CropCycleNotTerminal);
        }

        var activeWaterfallSettlementExists =
            await _dbContext
                .Set<ProfitSharingWaterfallSettlement>()
                .AsNoTracking()
                .AnyAsync(
                    candidate =>
                        candidate.OrganizationId == organizationId &&
                        candidate.CropCycleId == cropCycleId &&
                        candidate.Status ==
                            ProfitSharingWaterfallSettlementStatus.Finalized &&
                        !candidate.IsDeleted,
                    cancellationToken);

        if (activeWaterfallSettlementExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .ActiveSettlementExists);
        }

        var activeActivityExists =
            await _dbContext
                .Set<CultivationActivity>()
                .AsNoTracking()
                .AnyAsync(
                    activity =>
                        activity.OrganizationId ==
                            organizationId &&
                        activity.CropCycleId ==
                            cropCycleId &&
                        (
                            activity.Status ==
                                CultivationActivityStatus
                                    .Planned ||
                            activity.Status ==
                                CultivationActivityStatus
                                    .InProgress
                        ) &&
                        !activity.IsDeleted,
                    cancellationToken);

        if (activeActivityExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .ActiveActivityExists);
        }

        var draftHarvestExists =
            await _dbContext
                .Set<HarvestBatch>()
                .AsNoTracking()
                .AnyAsync(
                    harvest =>
                        harvest.OrganizationId ==
                            organizationId &&
                        harvest.CropCycleId ==
                            cropCycleId &&
                        harvest.Status ==
                            HarvestBatchStatus.Draft &&
                        !harvest.IsDeleted,
                    cancellationToken);

        if (draftHarvestExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .DraftHarvestExists);
        }

        var draftSaleExists =
            await (
                from line in _dbContext
                    .Set<SaleLine>()
                    .AsNoTracking()
                join sale in _dbContext
                    .Set<Sale>()
                    .AsNoTracking()
                    on new
                    {
                        line.OrganizationId,
                        line.SaleId
                    }
                    equals new
                    {
                        sale.OrganizationId,
                        SaleId = sale.Id
                    }
                where
                    line.OrganizationId ==
                        organizationId &&
                    line.CropCycleIdSnapshot ==
                        cropCycleId &&
                    sale.Status ==
                        SaleStatus.Draft &&
                    !sale.IsDeleted
                select line.Id)
                .AnyAsync(cancellationToken);

        if (draftSaleExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .DraftSaleExists);
        }

        var draftExpenseExists =
            await _dbContext
                .Set<CultivationExpense>()
                .AsNoTracking()
                .AnyAsync(
                    expense =>
                        expense.OrganizationId ==
                            organizationId &&
                        expense.CropCycleId ==
                            cropCycleId &&
                        expense.Status ==
                            CultivationExpenseStatus.Draft &&
                        !expense.IsDeleted,
                    cancellationToken);

        if (draftExpenseExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .DraftExpenseExists);
        }

        var draftContributionExists =
            await _dbContext
                .Set<CapitalContribution>()
                .AsNoTracking()
                .AnyAsync(
                    contribution =>
                        contribution.OrganizationId ==
                            organizationId &&
                        contribution.CropCycleId ==
                            cropCycleId &&
                        contribution.Status ==
                            CapitalContributionStatus.Draft &&
                        !contribution.IsDeleted,
                    cancellationToken);

        if (draftContributionExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .DraftContributionExists);
        }

        var draftPaymentExists =
            await (
                from payment in _dbContext
                    .Set<SalePayment>()
                    .AsNoTracking()
                join sale in _dbContext
                    .Set<Sale>()
                    .AsNoTracking()
                    on new
                    {
                        payment.OrganizationId,
                        payment.SaleId
                    }
                    equals new
                    {
                        sale.OrganizationId,
                        SaleId = sale.Id
                    }
                join line in _dbContext
                    .Set<SaleLine>()
                    .AsNoTracking()
                    on new
                    {
                        sale.OrganizationId,
                        SaleId = sale.Id
                    }
                    equals new
                    {
                        line.OrganizationId,
                        line.SaleId
                    }
                where
                    payment.OrganizationId ==
                        organizationId &&
                    line.CropCycleIdSnapshot ==
                        cropCycleId &&
                    payment.Status ==
                        SalePaymentStatus.Draft &&
                    !payment.IsDeleted &&
                    !sale.IsDeleted
                select payment.Id)
                .AnyAsync(cancellationToken);

        if (draftPaymentExists)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .DraftPaymentExists);
        }

        var calculationResult =
            await BuildCurrentCalculationAsync(
                organizationId,
                cropCycleId,
                settlement.ManagingPartnerCode,
                settlement.ManagingPartnerName,
                cancellationToken);

        if (!calculationResult.IsSuccess)
        {
            return calculationResult.FailureResult!;
        }

        var currentReport =
            calculationResult.Report!;

        if (currentReport.OutstandingReceivable != 0)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .OutstandingReceivableExists,
                outstandingReceivable:
                    currentReport.OutstandingReceivable);
        }

        if (currentReport.AvailableHarvestQuantity != 0)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .UnsoldHarvestExists);
        }

        if (!settlement.MatchesCalculation(
                calculationResult.Calculation!))
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .SourceDataChanged);
        }

        try
        {
            settlement.FinalizeSettlement();

            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            return ProfitSharingFinalizationResult.Failed(
                ProfitSharingFinalizationFailure
                    .InvalidStatus,
                message: exception.Message);
        }

        return ProfitSharingFinalizationResult.Succeeded(
            settlement);
    }

    private async Task<CurrentCalculationResult>
        BuildCurrentCalculationAsync(
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
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .SourceDataChanged));
        }

        CropCycleProfitabilityReport report;

        try
        {
            report =
                ToDomainReport(
                    profitabilityResult.Value);
        }
        catch (
            Exception exception)
            when (
                exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .SourceDataChanged));
        }

        if (report.TotalCultivationCost <= 0)
        {
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .ZeroCostUnsupported));
        }

        if (report.TotalConfirmedCapital !=
            report.TotalCultivationCost)
        {
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .CapitalDoesNotMatchCost,
                    totalCapital:
                        report.TotalConfirmedCapital,
                    totalCost:
                        report.TotalCultivationCost));
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

        var investorCapital =
            RoundMoney(
                confirmedContributions
                    .Where(contribution =>
                        contribution.ContributorRole ==
                            CapitalContributorRole.Investor)
                    .Sum(contribution =>
                        contribution.Amount));

        var partnerCapital =
            RoundMoney(
                confirmedContributions
                    .Where(contribution =>
                        contribution.ContributorRole ==
                            CapitalContributorRole.Partner)
                    .Sum(contribution =>
                        contribution.Amount));

        if (investorCapital !=
                report.ConfirmedInvestorCapital ||
            partnerCapital !=
                report.ConfirmedPartnerCapital)
        {
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .SourceDataChanged));
        }

        try
        {
            var calculation =
                ProfitSharingCalculator.Calculate(
                    report,
                    managingPartnerCode,
                    managingPartnerName,
                    confirmedContributions
                        .Select(contribution =>
                            new ProfitSharingContributorInput(
                                contribution.ContributorCode,
                                contribution.ContributorName,
                                contribution.ContributorRole,
                                contribution.Amount))
                        .ToArray());

            return CurrentCalculationResult.Succeeded(
                report,
                calculation);
        }
        catch (
            Exception exception)
            when (
                exception is ArgumentException or
                InvalidOperationException or
                OverflowException)
        {
            return CurrentCalculationResult.Failed(
                ProfitSharingFinalizationResult.Failed(
                    ProfitSharingFinalizationFailure
                        .SourceDataChanged));
        }
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

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static bool
        IsActiveSettlementUniqueViolation(
            Exception exception)
    {
        var postgresException =
            FindPostgresException(exception);

        return postgresException?.SqlState ==
                PostgresErrorCodes.UniqueViolation &&
            (
                postgresException.ConstraintName?.Contains(
                    "ProfitSharingSettlements",
                    StringComparison.OrdinalIgnoreCase) ==
                    true ||
                postgresException.ConstraintName?.Contains(
                    "Settlement",
                    StringComparison.OrdinalIgnoreCase) ==
                    true
            );
    }

    private static bool IsSerializationFailure(
        Exception exception)
    {
        var postgresException =
            FindPostgresException(exception);

        return postgresException?.SqlState is
            PostgresErrorCodes.SerializationFailure or
            PostgresErrorCodes.DeadlockDetected;
    }

    private static PostgresException?
        FindPostgresException(
            Exception exception)
    {
        Exception? current = exception;

        while (current is not null)
        {
            if (current is PostgresException
                postgresException)
            {
                return postgresException;
            }

            current = current.InnerException;
        }

        return null;
    }

    private sealed record CurrentCalculationResult(
        CropCycleProfitabilityReport? Report,
        ProfitSharingCalculationResult? Calculation,
        ProfitSharingFinalizationResult?
            FailureResult)
    {
        public bool IsSuccess =>
            Report is not null &&
            Calculation is not null &&
            FailureResult is null;

        public static CurrentCalculationResult Succeeded(
            CropCycleProfitabilityReport report,
            ProfitSharingCalculationResult calculation)
        {
            return new CurrentCalculationResult(
                report,
                calculation,
                null);
        }

        public static CurrentCalculationResult Failed(
            ProfitSharingFinalizationResult failureResult)
        {
            return new CurrentCalculationResult(
                null,
                null,
                failureResult);
        }
    }
}
