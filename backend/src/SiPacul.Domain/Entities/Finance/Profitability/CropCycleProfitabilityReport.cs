namespace SiPacul.Domain.Entities.Finance.Profitability;

public sealed record CropCycleProfitabilityReport
{
    private CropCycleProfitabilityReport()
    {
    }

    public Guid OrganizationId { get; init; }

    public Guid CropCycleId { get; init; }

    public string CropCycleCode { get; init; } =
        string.Empty;

    public string CropCycleName { get; init; } =
        string.Empty;

    public Guid CommodityIdSnapshot { get; init; }

    public string CommodityCodeSnapshot { get; init; } =
        string.Empty;

    public string CommodityNameSnapshot { get; init; } =
        string.Empty;

    public decimal RecognizedRevenue { get; init; }

    public decimal CollectedRevenue { get; init; }

    public decimal OutstandingReceivable { get; init; }

    public decimal ActivityResourceCost { get; init; }

    public decimal ManualExpenseCost { get; init; }

    public decimal TotalCultivationCost { get; init; }

    public decimal NetProfit { get; init; }

    public decimal? ProfitMarginPercentage { get; init; }

    public ProfitabilityOutcome Outcome { get; init; }

    public decimal ConfirmedInvestorCapital { get; init; }

    public decimal ConfirmedPartnerCapital { get; init; }

    public decimal TotalConfirmedCapital { get; init; }

    public decimal CapitalFundingGap { get; init; }

    public decimal CapitalFundingExcess { get; init; }

    public decimal AvailableHarvestQuantity { get; init; }

    public DateTime GeneratedAt { get; init; }

    public static CropCycleProfitabilityReport Calculate(
        CropCycleProfitabilityInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        ValidateIdentifier(
            input.OrganizationId,
            nameof(input.OrganizationId),
            "Organization");

        ValidateIdentifier(
            input.CropCycleId,
            nameof(input.CropCycleId),
            "Crop cycle");

        ValidateIdentifier(
            input.CommodityIdSnapshot,
            nameof(input.CommodityIdSnapshot),
            "Commodity snapshot");

        var recognizedRevenue =
            NormalizeMoney(
                input.RecognizedRevenue,
                nameof(input.RecognizedRevenue));

        var collectedRevenue =
            NormalizeMoney(
                input.CollectedRevenue,
                nameof(input.CollectedRevenue));

        if (collectedRevenue > recognizedRevenue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(input.CollectedRevenue),
                "Collected revenue cannot exceed " +
                "recognized revenue.");
        }

        var activityResourceCost =
            NormalizeMoney(
                input.ActivityResourceCost,
                nameof(input.ActivityResourceCost));

        var manualExpenseCost =
            NormalizeMoney(
                input.ManualExpenseCost,
                nameof(input.ManualExpenseCost));

        var investorCapital =
            NormalizeMoney(
                input.ConfirmedInvestorCapital,
                nameof(input.ConfirmedInvestorCapital));

        var partnerCapital =
            NormalizeMoney(
                input.ConfirmedPartnerCapital,
                nameof(input.ConfirmedPartnerCapital));

        var availableHarvest =
            NormalizeQuantity(
                input.AvailableHarvestQuantity,
                nameof(input.AvailableHarvestQuantity));

        var totalCost =
            NormalizeMoney(
                activityResourceCost +
                    manualExpenseCost,
                nameof(input.ActivityResourceCost));

        var netProfit =
            Math.Round(
                recognizedRevenue - totalCost,
                2,
                MidpointRounding.AwayFromZero);

        var outstanding =
            NormalizeMoney(
                recognizedRevenue - collectedRevenue,
                nameof(input.CollectedRevenue));

        var totalCapital =
            NormalizeMoney(
                investorCapital + partnerCapital,
                nameof(input.ConfirmedInvestorCapital));

        var fundingGap =
            NormalizeMoney(
                Math.Max(
                    totalCost - totalCapital,
                    0),
                nameof(input.ConfirmedInvestorCapital));

        var fundingExcess =
            NormalizeMoney(
                Math.Max(
                    totalCapital - totalCost,
                    0),
                nameof(input.ConfirmedInvestorCapital));

        decimal? margin =
            recognizedRevenue == 0
                ? null
                : Math.Round(
                    netProfit /
                    recognizedRevenue *
                    100,
                    4,
                    MidpointRounding.AwayFromZero);

        return new CropCycleProfitabilityReport
        {
            OrganizationId = input.OrganizationId,
            CropCycleId = input.CropCycleId,
            CropCycleCode =
                NormalizeRequiredText(
                    input.CropCycleCode,
                    nameof(input.CropCycleCode)),
            CropCycleName =
                NormalizeRequiredText(
                    input.CropCycleName,
                    nameof(input.CropCycleName)),
            CommodityIdSnapshot =
                input.CommodityIdSnapshot,
            CommodityCodeSnapshot =
                NormalizeRequiredText(
                    input.CommodityCodeSnapshot,
                    nameof(input.CommodityCodeSnapshot)),
            CommodityNameSnapshot =
                NormalizeRequiredText(
                    input.CommodityNameSnapshot,
                    nameof(input.CommodityNameSnapshot)),
            RecognizedRevenue = recognizedRevenue,
            CollectedRevenue = collectedRevenue,
            OutstandingReceivable = outstanding,
            ActivityResourceCost = activityResourceCost,
            ManualExpenseCost = manualExpenseCost,
            TotalCultivationCost = totalCost,
            NetProfit = netProfit,
            ProfitMarginPercentage = margin,
            Outcome = DetermineOutcome(netProfit),
            ConfirmedInvestorCapital = investorCapital,
            ConfirmedPartnerCapital = partnerCapital,
            TotalConfirmedCapital = totalCapital,
            CapitalFundingGap = fundingGap,
            CapitalFundingExcess = fundingExcess,
            AvailableHarvestQuantity = availableHarvest,
            GeneratedAt =
                NormalizeGeneratedAt(input.GeneratedAt)
        };
    }

    private static ProfitabilityOutcome DetermineOutcome(
        decimal netProfit)
    {
        if (netProfit < 0)
        {
            return ProfitabilityOutcome.Loss;
        }

        if (netProfit > 0)
        {
            return ProfitabilityOutcome.Profit;
        }

        return ProfitabilityOutcome.BreakEven;
    }

    private static decimal NormalizeMoney(
        decimal value,
        string parameterName)
    {
        var normalized =
            Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);

        if (normalized < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Money cannot be negative.");
        }

        return normalized;
    }

    private static decimal NormalizeQuantity(
        decimal value,
        string parameterName)
    {
        var normalized =
            Math.Round(
                value,
                4,
                MidpointRounding.AwayFromZero);

        if (normalized < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Quantity cannot be negative.");
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                "Value cannot be blank.",
                parameterName);
        }

        return value.Trim();
    }

    private static DateTime NormalizeGeneratedAt(
        DateTime value)
    {
        if (value == default)
        {
            throw new ArgumentException(
                "Generated-at cannot be default.",
                nameof(value));
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(
                value,
                DateTimeKind.Utc)
        };
    }

    private static void ValidateIdentifier(
        Guid value,
        string parameterName,
        string displayName)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException(
                $"{displayName} identifier cannot be empty.",
                parameterName);
        }
    }
}
