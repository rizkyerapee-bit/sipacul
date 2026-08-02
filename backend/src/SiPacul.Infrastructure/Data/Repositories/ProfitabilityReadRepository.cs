using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitabilityReadRepository :
    IProfitabilityReadRepository
{
    private readonly SiPaculDbContext _dbContext;

    public ProfitabilityReadRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProfitabilitySourceSnapshot?> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        var cropCycle =
            await _dbContext
                .Set<CropCycle>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    cycle =>
                        cycle.OrganizationId ==
                            organizationId &&
                        cycle.Id == cropCycleId &&
                        !cycle.IsDeleted,
                    cancellationToken);

        if (cropCycle is null)
        {
            return null;
        }

        var commodity =
            await _dbContext
                .Set<Commodity>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.OrganizationId ==
                            organizationId &&
                        item.Id ==
                            cropCycle.CommodityId &&
                        !item.IsDeleted,
                    cancellationToken);

        if (commodity is null)
        {
            throw new InvalidOperationException(
                $"Commodity '{cropCycle.CommodityId}' " +
                "referenced by crop cycle was not found.");
        }

        var activityResources =
            await (
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
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    !activity.IsDeleted
                select new ActivityResourceCostSource(
                    activity.Status,
                    activity.ActualStartDate,
                    resource.TotalCost)
            ).ToListAsync(cancellationToken);

        var manualExpenses =
            await _dbContext
                .Set<CultivationExpense>()
                .AsNoTracking()
                .Where(expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    !expense.IsDeleted)
                .Select(expense =>
                    new ManualExpenseSource(
                        expense.Status,
                        expense.Amount))
                .ToListAsync(cancellationToken);

        var capitalContributions =
            await _dbContext
                .Set<CapitalContribution>()
                .AsNoTracking()
                .Where(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    !contribution.IsDeleted)
                .Select(contribution =>
                    new CapitalContributionSource(
                        contribution.Status,
                        contribution.ContributorRole,
                        contribution.Amount))
                .ToListAsync(cancellationToken);

        var harvests =
            await _dbContext
                .Set<HarvestBatch>()
                .AsNoTracking()
                .Where(harvest =>
                    harvest.OrganizationId ==
                        organizationId &&
                    harvest.CropCycleId ==
                        cropCycleId &&
                    !harvest.IsDeleted)
                .Select(harvest =>
                    new ProfitabilityHarvestSource(
                        harvest.Status,
                        harvest.QuantityUnit,
                        harvest.NetQuantity))
                .ToListAsync(cancellationToken);

        var relevantSaleIds =
            await _dbContext
                .Set<SaleLine>()
                .AsNoTracking()
                .Where(line =>
                    line.OrganizationId ==
                        organizationId &&
                    line.CropCycleIdSnapshot ==
                        cropCycleId)
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

        return ProfitabilitySourceAggregator.Build(
            organizationId,
            cropCycleId,
            cropCycle.Code,
            cropCycle.Name,
            commodity.Id,
            commodity.Code.Value,
            commodity.Name,
            activityResources,
            manualExpenses,
            capitalContributions,
            sales,
            harvests);
    }

    private async Task<
        IReadOnlyList<ProfitabilitySaleSource>>
        LoadSalesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> saleIds,
            CancellationToken cancellationToken)
    {
        var saleIdArray =
            saleIds.ToArray();

        var sales =
            await _dbContext
                .Set<Sale>()
                .AsNoTracking()
                .Include(sale => sale.Lines)
                .Where(sale =>
                    sale.OrganizationId ==
                        organizationId &&
                    saleIdArray.Contains(sale.Id) &&
                    !sale.IsDeleted)
                .OrderBy(sale => sale.Id)
                .ToListAsync(cancellationToken);

        var paymentRows =
            await _dbContext
                .Set<SalePayment>()
                .AsNoTracking()
                .Where(payment =>
                    payment.OrganizationId ==
                        organizationId &&
                    saleIdArray.Contains(payment.SaleId) &&
                    !payment.IsDeleted)
                .Select(payment =>
                    new
                    {
                        payment.SaleId,
                        payment.Status,
                        payment.Amount
                    })
                .ToListAsync(cancellationToken);

        var paymentsBySale =
            paymentRows
                .GroupBy(payment => payment.SaleId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        (IReadOnlyList<
                            ProfitabilityPaymentSource>)
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
                        out var salePayments)
                            ? salePayments
                            : Array.Empty<
                                ProfitabilityPaymentSource>()))
            .ToArray();
    }
}
