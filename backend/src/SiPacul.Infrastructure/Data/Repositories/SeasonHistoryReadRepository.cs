using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Evaluations.SeasonHistories.Persistence;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SeasonHistoryReadRepository :
    ISeasonHistoryReadRepository
{
    private readonly SiPaculDbContext _dbContext;

    public SeasonHistoryReadRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<SeasonHistoryPageSource> GetPageAsync(
        Guid organizationId,
        Guid landId,
        Guid? landPlotId,
        bool includeNonTerminal,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(
            organizationId,
            landId,
            landPlotId,
            skip,
            take);

        var query = _dbContext
            .Set<CropCycle>()
            .AsNoTracking()
            .Where(cycle =>
                cycle.OrganizationId == organizationId &&
                cycle.LandId == landId &&
                !cycle.IsDeleted);

        if (landPlotId.HasValue)
        {
            query = query.Where(cycle =>
                cycle.LandPlotId == landPlotId.Value);
        }

        if (!includeNonTerminal)
        {
            query = query.Where(cycle =>
                cycle.Status == CropCycleStatus.Completed ||
                cycle.Status == CropCycleStatus.Cancelled);
        }

        var totalCount = await query.CountAsync(
            cancellationToken);

        var cycles = await query
            .OrderByDescending(cycle =>
                cycle.PlannedStartDate)
            .ThenByDescending(cycle => cycle.Code)
            .ThenBy(cycle => cycle.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        if (cycles.Count == 0)
        {
            return new SeasonHistoryPageSource(
                totalCount,
                Array.Empty<SeasonHistoryCycleSource>());
        }

        var cycleIds = cycles
            .Select(cycle => cycle.Id)
            .ToArray();

        var commodityIds = cycles
            .Select(cycle => cycle.CommodityId)
            .Distinct()
            .ToArray();

        var commodities = await _dbContext
            .Set<Commodity>()
            .AsNoTracking()
            .Where(commodity =>
                commodity.OrganizationId == organizationId &&
                commodityIds.Contains(commodity.Id) &&
                !commodity.IsDeleted)
            .ToDictionaryAsync(
                commodity => commodity.Id,
                cancellationToken);

        if (commodities.Count != commodityIds.Length)
        {
            throw new InvalidOperationException(
                "One or more commodities referenced by " +
                "crop-cycle history were not found.");
        }

        var activityFacts = await _dbContext
            .Set<CultivationActivity>()
            .AsNoTracking()
            .Where(activity =>
                activity.OrganizationId == organizationId &&
                cycleIds.Contains(activity.CropCycleId) &&
                !activity.IsDeleted)
            .Select(activity =>
                new ActivityFactRow(
                    activity.CropCycleId,
                    activity.Status,
                    activity.IssueNotes,
                    activity.CultivationSopId,
                    activity.SopComplianceStatus))
            .ToListAsync(cancellationToken);

        var activityResourceRows = await (
            from activity in _dbContext
                .Set<CultivationActivity>()
                .AsNoTracking()
            join resource in _dbContext
                .Set<CultivationActivityResource>()
                .AsNoTracking()
                on new
                {
                    activity.OrganizationId,
                    ActivityId = activity.Id
                }
                equals new
                {
                    resource.OrganizationId,
                    ActivityId =
                        resource.CultivationActivityId
                }
            where
                activity.OrganizationId == organizationId &&
                cycleIds.Contains(activity.CropCycleId) &&
                !activity.IsDeleted
            select new ActivityResourceRow(
                activity.CropCycleId,
                activity.Status,
                activity.ActualStartDate,
                resource.TotalCost)
        ).ToListAsync(cancellationToken);

        var expenseRows = await _dbContext
            .Set<CultivationExpense>()
            .AsNoTracking()
            .Where(expense =>
                expense.OrganizationId == organizationId &&
                cycleIds.Contains(expense.CropCycleId) &&
                !expense.IsDeleted)
            .Select(expense =>
                new ExpenseRow(
                    expense.CropCycleId,
                    expense.Status,
                    expense.Amount))
            .ToListAsync(cancellationToken);

        var capitalRows = await _dbContext
            .Set<CapitalContribution>()
            .AsNoTracking()
            .Where(contribution =>
                contribution.OrganizationId == organizationId &&
                cycleIds.Contains(contribution.CropCycleId) &&
                !contribution.IsDeleted)
            .Select(contribution =>
                new CapitalRow(
                    contribution.CropCycleId,
                    contribution.Status,
                    contribution.ContributorRole,
                    contribution.Amount))
            .ToListAsync(cancellationToken);

        var harvestRows = await _dbContext
            .Set<HarvestBatch>()
            .AsNoTracking()
            .Where(harvest =>
                harvest.OrganizationId == organizationId &&
                cycleIds.Contains(harvest.CropCycleId) &&
                !harvest.IsDeleted)
            .Select(harvest =>
                new HarvestRow(
                    harvest.CropCycleId,
                    harvest.Status,
                    harvest.QuantityUnit,
                    harvest.NetQuantity))
            .ToListAsync(cancellationToken);

        var relevantSaleIds = await _dbContext
            .Set<SaleLine>()
            .AsNoTracking()
            .Where(line =>
                line.OrganizationId == organizationId &&
                cycleIds.Contains(
                    line.CropCycleIdSnapshot))
            .Select(line => line.SaleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        IReadOnlyList<ProfitabilitySaleSource> sales =
            relevantSaleIds.Count == 0
                ? Array.Empty<ProfitabilitySaleSource>()
                : await LoadSalesAsync(
                    organizationId,
                    relevantSaleIds,
                    cancellationToken);

        var activityFactsByCycle = activityFacts
            .ToLookup(row => row.CropCycleId);

        var resourcesByCycle = activityResourceRows
            .ToLookup(row => row.CropCycleId);

        var expensesByCycle = expenseRows
            .ToLookup(row => row.CropCycleId);

        var capitalByCycle = capitalRows
            .ToLookup(row => row.CropCycleId);

        var harvestsByCycle = harvestRows
            .ToLookup(row => row.CropCycleId);

        var sources = new List<SeasonHistoryCycleSource>(
            cycles.Count);

        foreach (var cycle in cycles)
        {
            var commodity = commodities[cycle.CommodityId];
            var cycleActivities = activityFactsByCycle[
                cycle.Id].ToArray();

            var profitability = ProfitabilitySourceAggregator
                .Build(
                    organizationId,
                    cycle.Id,
                    cycle.Code,
                    cycle.Name,
                    commodity.Id,
                    commodity.Code.Value,
                    commodity.Name,
                    resourcesByCycle[cycle.Id]
                        .Select(row =>
                            new ActivityResourceCostSource(
                                row.Status,
                                row.ActualStartDate,
                                row.TotalCost))
                        .ToArray(),
                    expensesByCycle[cycle.Id]
                        .Select(row =>
                            new ManualExpenseSource(
                                row.Status,
                                row.Amount))
                        .ToArray(),
                    capitalByCycle[cycle.Id]
                        .Select(row =>
                            new CapitalContributionSource(
                                row.Status,
                                row.ContributorRole,
                                row.Amount))
                        .ToArray(),
                    sales,
                    harvestsByCycle[cycle.Id]
                        .Select(row =>
                            new ProfitabilityHarvestSource(
                                row.Status,
                                row.QuantityUnit,
                                row.NetQuantity))
                        .ToArray());

            var totalCost = Math.Round(
                profitability.ActivityResourceCost +
                profitability.ManualExpenseCost,
                2,
                MidpointRounding.AwayFromZero);

            var totalCapital = Math.Round(
                profitability.ConfirmedInvestorCapital +
                profitability.ConfirmedPartnerCapital,
                2,
                MidpointRounding.AwayFromZero);

            sources.Add(
                new SeasonHistoryCycleSource(
                    organizationId,
                    cycle.Id,
                    cycle.Code,
                    cycle.Name,
                    cycle.LandId,
                    cycle.LandPlotId,
                    commodity.Id,
                    commodity.Code.Value,
                    commodity.Name,
                    cycle.Status,
                    cycle.PlannedStartDate,
                    cycle.ExpectedHarvestDate,
                    cycle.ActualStartDate,
                    cycle.ActualHarvestDate,
                    cycleActivities.Length,
                    cycleActivities.Count(row =>
                        row.Status ==
                            CultivationActivityStatus.Completed),
                    cycleActivities.Count(row =>
                        row.Status ==
                            CultivationActivityStatus.Cancelled),
                    cycleActivities.Count(row =>
                        row.Status is
                            CultivationActivityStatus.Planned or
                            CultivationActivityStatus.InProgress),
                    cycleActivities.Count(row =>
                        !string.IsNullOrWhiteSpace(
                            row.IssueNotes)),
                    cycleActivities.Count(row =>
                        row.CultivationSopId.HasValue),
                    cycleActivities.Count(row =>
                        row.CultivationSopId.HasValue &&
                        row.SopComplianceStatus ==
                            SopComplianceStatus.Compliant),
                    cycleActivities.Count(row =>
                        row.CultivationSopId.HasValue &&
                        row.SopComplianceStatus ==
                            SopComplianceStatus.Deviated),
                    cycleActivities.Count(row =>
                        row.CultivationSopId.HasValue &&
                        row.SopComplianceStatus ==
                            SopComplianceStatus.NotEvaluated),
                    harvestsByCycle[cycle.Id].Count(row =>
                        row.Status ==
                            HarvestBatchStatus.Confirmed),
                    profitability.RecognizedRevenue,
                    profitability.CollectedRevenue,
                    totalCost,
                    Math.Max(totalCost - totalCapital, 0)));
        }

        return new SeasonHistoryPageSource(
            totalCount,
            sources);
    }

    private async Task<IReadOnlyList<ProfitabilitySaleSource>>
        LoadSalesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> saleIds,
            CancellationToken cancellationToken)
    {
        var saleIdArray = saleIds.ToArray();

        var sales = await _dbContext
            .Set<Sale>()
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .Where(sale =>
                sale.OrganizationId == organizationId &&
                saleIdArray.Contains(sale.Id) &&
                !sale.IsDeleted)
            .OrderBy(sale => sale.Id)
            .ToListAsync(cancellationToken);

        var paymentRows = await _dbContext
            .Set<SalePayment>()
            .AsNoTracking()
            .Where(payment =>
                payment.OrganizationId == organizationId &&
                saleIdArray.Contains(payment.SaleId) &&
                !payment.IsDeleted)
            .Select(payment =>
                new PaymentRow(
                    payment.SaleId,
                    payment.Status,
                    payment.Amount))
            .ToListAsync(cancellationToken);

        var paymentsBySale = paymentRows
            .GroupBy(payment => payment.SaleId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<ProfitabilityPaymentSource>)
                    group
                        .Select(payment =>
                            new ProfitabilityPaymentSource(
                                payment.Status,
                                payment.Amount))
                        .ToArray());

        return sales
            .Select(sale =>
                new ProfitabilitySaleSource(
                    sale.Id,
                    sale.Status,
                    sale.Subtotal,
                    sale.DiscountAmount,
                    sale.TotalAmount,
                    sale.Lines
                        .OrderBy(line => line.Id)
                        .Select(line =>
                            new ProfitabilitySaleLineSource(
                                line.Id,
                                line.CropCycleIdSnapshot,
                                line.LineTotal,
                                line.Quantity))
                        .ToArray(),
                    paymentsBySale.TryGetValue(
                        sale.Id,
                        out var payments)
                            ? payments
                            : Array.Empty<
                                ProfitabilityPaymentSource>()))
            .ToArray();
    }

    private static void ValidateArguments(
        Guid organizationId,
        Guid landId,
        Guid? landPlotId,
        int skip,
        int take)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty.",
                nameof(organizationId));
        }

        if (landId == Guid.Empty)
        {
            throw new ArgumentException(
                "Land identifier cannot be empty.",
                nameof(landId));
        }

        if (landPlotId == Guid.Empty)
        {
            throw new ArgumentException(
                "Land plot identifier cannot be empty.",
                nameof(landPlotId));
        }

        if (skip < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(skip),
                "Skip cannot be negative.");
        }

        if (take < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(take),
                "Take must be at least one.");
        }
    }

    private sealed record ActivityFactRow(
        Guid CropCycleId,
        CultivationActivityStatus Status,
        string? IssueNotes,
        Guid? CultivationSopId,
        SopComplianceStatus SopComplianceStatus);

    private sealed record ActivityResourceRow(
        Guid CropCycleId,
        CultivationActivityStatus Status,
        DateOnly? ActualStartDate,
        decimal TotalCost);

    private sealed record ExpenseRow(
        Guid CropCycleId,
        CultivationExpenseStatus Status,
        decimal Amount);

    private sealed record CapitalRow(
        Guid CropCycleId,
        CapitalContributionStatus Status,
        CapitalContributorRole ContributorRole,
        decimal Amount);

    private sealed record HarvestRow(
        Guid CropCycleId,
        HarvestBatchStatus Status,
        HarvestQuantityUnit QuantityUnit,
        decimal NetQuantity);

    private sealed record PaymentRow(
        Guid SaleId,
        SalePaymentStatus Status,
        decimal Amount);
}
