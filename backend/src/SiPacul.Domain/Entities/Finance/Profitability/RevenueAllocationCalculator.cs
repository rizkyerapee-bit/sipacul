namespace SiPacul.Domain.Entities.Finance.Profitability;

public static class RevenueAllocationCalculator
{
    public static SaleRevenueAllocationResult Allocate(
        decimal subtotal,
        decimal saleDiscountAmount,
        decimal confirmedPaymentAmount,
        IReadOnlyCollection<SaleRevenueLineInput> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (lines.Count == 0)
        {
            throw new ArgumentException(
                "At least one sale line is required.",
                nameof(lines));
        }

        var normalizedLines =
            ValidateAndNormalizeLines(lines);

        var normalizedSubtotal =
            NormalizeMoney(
                subtotal,
                nameof(subtotal),
                allowZero: true);

        var calculatedSubtotal =
            NormalizeMoney(
                normalizedLines.Sum(line =>
                    line.LineTotal),
                nameof(lines),
                allowZero: true);

        if (calculatedSubtotal != normalizedSubtotal)
        {
            throw new ArgumentException(
                "Sale subtotal must equal the sum of " +
                "sale line totals.",
                nameof(subtotal));
        }

        var normalizedDiscount =
            NormalizeMoney(
                saleDiscountAmount,
                nameof(saleDiscountAmount),
                allowZero: true);

        if (normalizedDiscount > normalizedSubtotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(saleDiscountAmount),
                "Sale discount cannot exceed subtotal.");
        }

        var saleTotal =
            NormalizeMoney(
                normalizedSubtotal -
                    normalizedDiscount,
                nameof(saleDiscountAmount),
                allowZero: true);

        var normalizedPayment =
            NormalizeMoney(
                confirmedPaymentAmount,
                nameof(confirmedPaymentAmount),
                allowZero: true);

        if (normalizedPayment > saleTotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmedPaymentAmount),
                "Confirmed payment cannot exceed " +
                "sale total.");
        }

        var discountAllocations =
            AllocateProportionally(
                normalizedDiscount,
                normalizedSubtotal,
                normalizedLines
                    .Select(line => line.LineTotal)
                    .ToArray());

        var netRevenues =
            normalizedLines
                .Select(
                    (line, index) =>
                        NormalizeMoney(
                            line.LineTotal -
                                discountAllocations[index],
                            nameof(lines),
                            allowZero: true))
                .ToArray();

        var paymentAllocations =
            saleTotal == 0
                ? new decimal[normalizedLines.Count]
                : AllocateProportionally(
                    normalizedPayment,
                    saleTotal,
                    netRevenues);

        var allocations =
            normalizedLines
                .Select(
                    (line, index) =>
                        new SaleLineRevenueAllocation(
                            line.SaleLineId,
                            line.CropCycleId,
                            line.LineTotal,
                            discountAllocations[index],
                            netRevenues[index],
                            paymentAllocations[index]))
                .ToArray();

        var result =
            new SaleRevenueAllocationResult(
                normalizedSubtotal,
                normalizedDiscount,
                saleTotal,
                normalizedPayment,
                allocations);

        EnsureAllocationTotals(result);

        return result;
    }

    public static IReadOnlyList<CropCycleRevenueAllocation>
        AggregateByCropCycle(
            IEnumerable<SaleLineRevenueAllocation> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        return lines
            .GroupBy(line => line.CropCycleId)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var recognized =
                    NormalizeMoney(
                        group.Sum(line =>
                            line.NetRecognizedRevenue),
                        nameof(lines),
                        allowZero: true);

                var collected =
                    NormalizeMoney(
                        group.Sum(line =>
                            line.AllocatedCollectedRevenue),
                        nameof(lines),
                        allowZero: true);

                return new CropCycleRevenueAllocation(
                    group.Key,
                    recognized,
                    collected,
                    NormalizeMoney(
                        recognized - collected,
                        nameof(lines),
                        allowZero: true));
            })
            .ToArray();
    }

    private static IReadOnlyList<SaleRevenueLineInput>
        ValidateAndNormalizeLines(
            IReadOnlyCollection<SaleRevenueLineInput> lines)
    {
        var duplicateLineId =
            lines
                .GroupBy(line => line.SaleLineId)
                .FirstOrDefault(group =>
                    group.Count() > 1);

        if (duplicateLineId is not null)
        {
            throw new ArgumentException(
                "Sale line identifiers must be unique.",
                nameof(lines));
        }

        return lines
            .Select(line =>
            {
                if (line.SaleLineId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "Sale line identifier cannot be empty.",
                        nameof(lines));
                }

                if (line.CropCycleId == Guid.Empty)
                {
                    throw new ArgumentException(
                        "Crop cycle identifier cannot be empty.",
                        nameof(lines));
                }

                return line with
                {
                    LineTotal = NormalizeMoney(
                        line.LineTotal,
                        nameof(lines),
                        allowZero: true)
                };
            })
            .OrderBy(line => line.SaleLineId)
            .ToArray();
    }

    private static decimal[] AllocateProportionally(
        decimal amountToAllocate,
        decimal denominator,
        IReadOnlyList<decimal> weights)
    {
        var allocations =
            new decimal[weights.Count];

        if (amountToAllocate == 0)
        {
            return allocations;
        }

        if (denominator <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(denominator),
                "Allocation denominator must be positive.");
        }

        var allocated = 0m;

        for (
            var index = 0;
            index < weights.Count;
            index++)
        {
            if (index == weights.Count - 1)
            {
                allocations[index] =
                    NormalizeMoney(
                        amountToAllocate - allocated,
                        nameof(amountToAllocate),
                        allowZero: true);

                break;
            }

            var raw =
                amountToAllocate *
                weights[index] /
                denominator;

            var rounded =
                NormalizeMoney(
                    raw,
                    nameof(amountToAllocate),
                    allowZero: true);

            var remaining =
                NormalizeMoney(
                    amountToAllocate - allocated,
                    nameof(amountToAllocate),
                    allowZero: true);

            allocations[index] =
                Math.Min(
                    rounded,
                    remaining);

            allocated =
                NormalizeMoney(
                    allocated + allocations[index],
                    nameof(amountToAllocate),
                    allowZero: true);
        }

        return allocations;
    }

    private static void EnsureAllocationTotals(
        SaleRevenueAllocationResult result)
    {
        if (result.RecognizedRevenue !=
            result.SaleTotalAmount)
        {
            throw new InvalidOperationException(
                "Recognized revenue allocation does not " +
                "equal sale total.");
        }

        if (result.CollectedRevenue !=
            result.ConfirmedPaymentAmount)
        {
            throw new InvalidOperationException(
                "Collected revenue allocation does not " +
                "equal confirmed payment.");
        }

        if (result.Lines.Any(line =>
                line.AllocatedSaleDiscount < 0 ||
                line.NetRecognizedRevenue < 0 ||
                line.AllocatedCollectedRevenue < 0 ||
                line.AllocatedCollectedRevenue >
                    line.NetRecognizedRevenue))
        {
            throw new InvalidOperationException(
                "A sale line allocation is outside its " +
                "valid monetary bounds.");
        }
    }

    private static decimal NormalizeMoney(
        decimal value,
        string parameterName,
        bool allowZero)
    {
        var normalized =
            Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);

        if (allowZero
                ? normalized < 0
                : normalized <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                allowZero
                    ? "Money cannot be negative."
                    : "Money must be greater than zero.");
        }

        return normalized;
    }
}
