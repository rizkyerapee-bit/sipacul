using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.Profitability;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public static class ProfitabilitySourceAggregator
{
    public static ProfitabilitySourceSnapshot Build(
        Guid organizationId,
        Guid cropCycleId,
        string cropCycleCode,
        string cropCycleName,
        Guid commodityId,
        string commodityCode,
        string commodityName,
        IReadOnlyCollection<ActivityResourceCostSource>
            activityResources,
        IReadOnlyCollection<ManualExpenseSource>
            manualExpenses,
        IReadOnlyCollection<CapitalContributionSource>
            capitalContributions,
        IReadOnlyCollection<ProfitabilitySaleSource> sales,
        IReadOnlyCollection<ProfitabilityHarvestSource>
            harvests)
    {
        ArgumentNullException.ThrowIfNull(activityResources);
        ArgumentNullException.ThrowIfNull(manualExpenses);
        ArgumentNullException.ThrowIfNull(
            capitalContributions);
        ArgumentNullException.ThrowIfNull(sales);
        ArgumentNullException.ThrowIfNull(harvests);

        ValidateUniqueSaleIdentifiers(sales);

        var activityResourceCost =
            RoundMoney(
                activityResources
                    .Where(IsActualActivityResource)
                    .Sum(source => source.TotalCost));

        var manualExpenseCost =
            RoundMoney(
                manualExpenses
                    .Where(expense =>
                        expense.Status ==
                            CultivationExpenseStatus.Confirmed)
                    .Sum(expense => expense.Amount));

        var investorCapital =
            RoundMoney(
                capitalContributions
                    .Where(contribution =>
                        contribution.Status ==
                            CapitalContributionStatus.Confirmed &&
                        contribution.ContributorRole ==
                            CapitalContributorRole.Investor)
                    .Sum(contribution =>
                        contribution.Amount));

        var partnerCapital =
            RoundMoney(
                capitalContributions
                    .Where(contribution =>
                        contribution.Status ==
                            CapitalContributionStatus.Confirmed &&
                        contribution.ContributorRole ==
                            CapitalContributorRole.Partner)
                    .Sum(contribution =>
                        contribution.Amount));

        var confirmedSales =
            sales
                .Where(sale =>
                    sale.Status == SaleStatus.Confirmed)
                .OrderBy(sale => sale.SaleId)
                .ToArray();

        var allocations =
            confirmedSales
                .SelectMany(AllocateSale)
                .Where(line =>
                    line.CropCycleId == cropCycleId)
                .ToArray();

        var recognizedRevenue =
            RoundMoney(
                allocations.Sum(line =>
                    line.NetRecognizedRevenue));

        var collectedRevenue =
            RoundMoney(
                allocations.Sum(line =>
                    line.AllocatedCollectedRevenue));

        var harvestSummary =
            CalculateAvailableHarvest(
                cropCycleId,
                confirmedSales,
                harvests);

        return new ProfitabilitySourceSnapshot(
            organizationId,
            cropCycleId,
            cropCycleCode,
            cropCycleName,
            commodityId,
            commodityCode,
            commodityName,
            recognizedRevenue,
            collectedRevenue,
            activityResourceCost,
            manualExpenseCost,
            investorCapital,
            partnerCapital,
            harvestSummary.AvailableQuantity,
            harvestSummary.QuantityUnit);
    }

    private static IReadOnlyList<
        SaleLineRevenueAllocation> AllocateSale(
            ProfitabilitySaleSource sale)
    {
        var confirmedPayment =
            RoundMoney(
                sale.Payments
                    .Where(payment =>
                        payment.Status ==
                            SalePaymentStatus.Confirmed)
                    .Sum(payment => payment.Amount));

        var allocation =
            RevenueAllocationCalculator.Allocate(
                sale.Subtotal,
                sale.DiscountAmount,
                confirmedPayment,
                sale.Lines
                    .Select(line =>
                        new SaleRevenueLineInput(
                            line.SaleLineId,
                            line.CropCycleId,
                            line.LineTotal))
                    .ToArray());

        if (allocation.SaleTotalAmount !=
            RoundMoney(sale.TotalAmount))
        {
            throw new InvalidOperationException(
                $"Sale '{sale.SaleId}' total does not match " +
                "its subtotal and discount.");
        }

        return allocation.Lines;
    }

    private static HarvestSummary CalculateAvailableHarvest(
        Guid cropCycleId,
        IReadOnlyCollection<ProfitabilitySaleSource>
            confirmedSales,
        IReadOnlyCollection<ProfitabilityHarvestSource>
            harvests)
    {
        var confirmedHarvests =
            harvests
                .Where(harvest =>
                    harvest.Status ==
                        HarvestBatchStatus.Confirmed)
                .ToArray();

        if (confirmedHarvests.Length == 0)
        {
            return new HarvestSummary(
                0,
                null);
        }

        var units =
            confirmedHarvests
                .Select(harvest =>
                    harvest.QuantityUnit)
                .Distinct()
                .ToArray();

        if (units.Length != 1)
        {
            throw new InvalidOperationException(
                "Available harvest quantity cannot be " +
                "aggregated across different quantity units.");
        }

        var harvestedQuantity =
            RoundQuantity(
                confirmedHarvests.Sum(harvest =>
                    harvest.NetQuantity));

        var soldQuantity =
            RoundQuantity(
                confirmedSales
                    .SelectMany(sale => sale.Lines)
                    .Where(line =>
                        line.CropCycleId == cropCycleId)
                    .Sum(line => line.Quantity));

        if (soldQuantity > harvestedQuantity)
        {
            throw new InvalidOperationException(
                "Confirmed sold quantity exceeds confirmed " +
                "harvest quantity for the crop cycle.");
        }

        return new HarvestSummary(
            RoundQuantity(
                harvestedQuantity - soldQuantity),
            units[0]);
    }

    private static bool IsActualActivityResource(
        ActivityResourceCostSource source)
    {
        return source.ActivityStatus is
                   CultivationActivityStatus.InProgress or
                   CultivationActivityStatus.Completed ||
               (
                   source.ActivityStatus ==
                       CultivationActivityStatus.Cancelled &&
                   source.ActualStartDate.HasValue
               );
    }

    private static void ValidateUniqueSaleIdentifiers(
        IReadOnlyCollection<ProfitabilitySaleSource> sales)
    {
        var duplicate =
            sales
                .GroupBy(sale => sale.SaleId)
                .FirstOrDefault(group =>
                    group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                "Sale identifiers in profitability sources " +
                "must be unique.",
                nameof(sales));
        }
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static decimal RoundQuantity(decimal value)
    {
        return Math.Round(
            value,
            4,
            MidpointRounding.AwayFromZero);
    }

    private sealed record HarvestSummary(
        decimal AvailableQuantity,
        HarvestQuantityUnit? QuantityUnit);
}
