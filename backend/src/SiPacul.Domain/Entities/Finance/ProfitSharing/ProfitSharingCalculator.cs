using System.Text.RegularExpressions;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public static partial class ProfitSharingCalculator
{
    public const string CurrentCalculationVersion =
        "SIPACUL-PS-1";

    public const int MaxContributorCodeLength = 40;

    public const int MaxContributorNameLength = 150;

    public static ProfitSharingCalculationResult Calculate(
        CropCycleProfitabilityReport profitability,
        string managingPartnerCode,
        string managingPartnerName,
        IReadOnlyCollection<ProfitSharingContributorInput>
            contributors)
    {
        ArgumentNullException.ThrowIfNull(profitability);
        ArgumentNullException.ThrowIfNull(contributors);

        ValidateReport(profitability);

        var normalizedManagingPartnerCode =
            NormalizeContributorCode(
                managingPartnerCode,
                nameof(managingPartnerCode));

        var normalizedManagingPartnerName =
            NormalizeRequiredText(
                managingPartnerName,
                MaxContributorNameLength,
                nameof(managingPartnerName),
                "Managing partner name");

        var normalizedContributors =
            NormalizeAndGroupContributors(contributors);

        EnsureManagingPartnerIdentity(
            normalizedManagingPartnerCode,
            normalizedManagingPartnerName,
            normalizedContributors);

        var contributorsWithManagingPartner =
            AddManagingPartnerIfMissing(
                normalizedManagingPartnerCode,
                normalizedManagingPartnerName,
                normalizedContributors);

        ValidateCapitalTotals(
            profitability,
            contributorsWithManagingPartner);

        var positiveCapitalContributors =
            contributorsWithManagingPartner
                .Where(contributor =>
                    contributor.ConfirmedCapital > 0)
                .ToArray();

        var managementProfitPool =
            profitability.Outcome ==
                ProfitabilityOutcome.Profit
                ? RoundMoney(
                    profitability.NetProfit / 3m)
                : 0m;

        var capitalProfitPool =
            profitability.Outcome ==
                ProfitabilityOutcome.Profit
                ? RoundMoney(
                    profitability.NetProfit -
                        managementProfitPool)
                : 0m;

        var recoverableCapitalPool =
            profitability.Outcome ==
                ProfitabilityOutcome.Loss
                ? RoundMoney(
                    Math.Min(
                        profitability.RecognizedRevenue,
                        profitability.TotalCultivationCost))
                : profitability.TotalCultivationCost;

        var capitalProfitByKey =
            AllocateMoney(
                capitalProfitPool,
                profitability.TotalCultivationCost,
                positiveCapitalContributors);

        var capitalRecoveryByKey =
            AllocateMoney(
                recoverableCapitalPool,
                profitability.TotalCultivationCost,
                positiveCapitalContributors);

        var allocations =
            contributorsWithManagingPartner
                .OrderBy(contributor =>
                    contributor.ContributorRole)
                .ThenBy(contributor =>
                    contributor.ContributorCode,
                    StringComparer.Ordinal)
                .Select(
                    (contributor, index) =>
                    {
                        var key =
                            ContributorKey.Create(
                                contributor.ContributorRole,
                                contributor.ContributorCode);

                        var capitalRecovery =
                            capitalRecoveryByKey.TryGetValue(
                                key,
                                out var recovery)
                                ? recovery
                                : 0m;

                        var capitalProfitShare =
                            capitalProfitByKey.TryGetValue(
                                key,
                                out var capitalProfit)
                                ? capitalProfit
                                : 0m;

                        var managementProfitShare =
                            contributor.ContributorRole ==
                                CapitalContributorRole.Partner &&
                            contributor.ContributorCode ==
                                normalizedManagingPartnerCode
                                ? managementProfitPool
                                : 0m;

                        var capitalLoss =
                            RoundMoney(
                                contributor.ConfirmedCapital -
                                    capitalRecovery);

                        var totalProfitShare =
                            RoundMoney(
                                managementProfitShare +
                                    capitalProfitShare);

                        var totalPayout =
                            RoundMoney(
                                capitalRecovery +
                                    totalProfitShare);

                        return new
                            ProfitSharingAllocationCalculation(
                                contributor.ContributorCode,
                                contributor.ContributorName,
                                contributor.ContributorRole,
                                contributor.ConfirmedCapital,
                                CalculateCapitalRatio(
                                    contributor.ConfirmedCapital,
                                    profitability
                                        .TotalCultivationCost),
                                capitalRecovery,
                                capitalLoss,
                                managementProfitShare,
                                capitalProfitShare,
                                totalProfitShare,
                                totalPayout,
                                index + 1);
                    })
                .ToArray();

        var result =
            new ProfitSharingCalculationResult(
                profitability.OrganizationId,
                profitability.CropCycleId,
                profitability.RecognizedRevenue,
                profitability.CollectedRevenue,
                profitability.OutstandingReceivable,
                profitability.ActivityResourceCost,
                profitability.ManualExpenseCost,
                profitability.TotalCultivationCost,
                profitability.NetProfit,
                profitability.Outcome,
                managementProfitPool,
                capitalProfitPool,
                profitability.ConfirmedInvestorCapital,
                profitability.ConfirmedPartnerCapital,
                profitability.TotalConfirmedCapital,
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.CapitalRecovery)),
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.CapitalLoss)),
                RoundMoney(
                    allocations
                        .Where(allocation =>
                            allocation.ContributorRole ==
                                CapitalContributorRole.Investor)
                        .Sum(allocation =>
                            allocation.TotalProfitShare)),
                RoundMoney(
                    allocations
                        .Where(allocation =>
                            allocation.ContributorRole ==
                                CapitalContributorRole.Partner)
                        .Sum(allocation =>
                            allocation.TotalProfitShare)),
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.TotalPayout)),
                CurrentCalculationVersion,
                Array.AsReadOnly(allocations));

        EnsureResultInvariants(result);

        return result;
    }

    private static IReadOnlyList<NormalizedContributor>
        NormalizeAndGroupContributors(
            IReadOnlyCollection<ProfitSharingContributorInput>
                contributors)
    {
        var normalized =
            contributors
                .Select(contributor =>
                {
                    ArgumentNullException.ThrowIfNull(
                        contributor);

                    ValidateContributorRole(
                        contributor.ContributorRole);

                    return new NormalizedContributor(
                        NormalizeContributorCode(
                            contributor.ContributorCode,
                            nameof(contributors)),
                        NormalizeRequiredText(
                            contributor.ContributorName,
                            MaxContributorNameLength,
                            nameof(contributors),
                            "Contributor name"),
                        contributor.ContributorRole,
                        NormalizePositiveMoney(
                            contributor.ConfirmedCapital,
                            nameof(contributors)));
                })
                .ToArray();

        var grouped = new List<NormalizedContributor>();

        foreach (var group in normalized.GroupBy(
                     contributor =>
                         ContributorKey.Create(
                             contributor.ContributorRole,
                             contributor.ContributorCode)))
        {
            var names =
                group
                    .Select(contributor =>
                        contributor.ContributorName)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

            if (names.Length != 1)
            {
                throw new ArgumentException(
                    "Contributor identity has conflicting names.",
                    nameof(contributors));
            }

            grouped.Add(
                new NormalizedContributor(
                    group.Key.ContributorCode,
                    names[0],
                    group.Key.ContributorRole,
                    RoundMoney(
                        group.Sum(contributor =>
                            contributor.ConfirmedCapital))));
        }

        return grouped
            .OrderBy(contributor =>
                contributor.ContributorRole)
            .ThenBy(contributor =>
                contributor.ContributorCode,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static void EnsureManagingPartnerIdentity(
        string managingPartnerCode,
        string managingPartnerName,
        IReadOnlyCollection<NormalizedContributor>
            contributors)
    {
        var matchingPartner =
            contributors.SingleOrDefault(contributor =>
                contributor.ContributorRole ==
                    CapitalContributorRole.Partner &&
                contributor.ContributorCode ==
                    managingPartnerCode);

        if (matchingPartner is not null &&
            matchingPartner.ContributorName !=
                managingPartnerName)
        {
            throw new ArgumentException(
                "Managing partner identity conflicts with " +
                "the confirmed contributor identity.",
                nameof(managingPartnerName));
        }
    }

    private static IReadOnlyList<NormalizedContributor>
        AddManagingPartnerIfMissing(
            string managingPartnerCode,
            string managingPartnerName,
            IReadOnlyCollection<NormalizedContributor>
                contributors)
    {
        if (contributors.Any(contributor =>
                contributor.ContributorRole ==
                    CapitalContributorRole.Partner &&
                contributor.ContributorCode ==
                    managingPartnerCode))
        {
            return contributors.ToArray();
        }

        return contributors
            .Append(
                new NormalizedContributor(
                    managingPartnerCode,
                    managingPartnerName,
                    CapitalContributorRole.Partner,
                    0m))
            .OrderBy(contributor =>
                contributor.ContributorRole)
            .ThenBy(contributor =>
                contributor.ContributorCode,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static void ValidateCapitalTotals(
        CropCycleProfitabilityReport profitability,
        IReadOnlyCollection<NormalizedContributor>
            contributors)
    {
        var investorCapital =
            RoundMoney(
                contributors
                    .Where(contributor =>
                        contributor.ContributorRole ==
                            CapitalContributorRole.Investor)
                    .Sum(contributor =>
                        contributor.ConfirmedCapital));

        var partnerCapital =
            RoundMoney(
                contributors
                    .Where(contributor =>
                        contributor.ContributorRole ==
                            CapitalContributorRole.Partner)
                    .Sum(contributor =>
                        contributor.ConfirmedCapital));

        var totalCapital =
            RoundMoney(
                investorCapital + partnerCapital);

        if (investorCapital !=
            profitability.ConfirmedInvestorCapital)
        {
            throw new ArgumentException(
                "Investor contributor capital does not match " +
                "the profitability report.",
                nameof(contributors));
        }

        if (partnerCapital !=
            profitability.ConfirmedPartnerCapital)
        {
            throw new ArgumentException(
                "Partner contributor capital does not match " +
                "the profitability report.",
                nameof(contributors));
        }

        if (totalCapital !=
            profitability.TotalConfirmedCapital)
        {
            throw new ArgumentException(
                "Contributor capital does not match total " +
                "confirmed capital.",
                nameof(contributors));
        }

        if (totalCapital !=
            profitability.TotalCultivationCost)
        {
            throw new InvalidOperationException(
                "Confirmed capital must equal total " +
                "cultivation cost before profit sharing.");
        }
    }

    private static Dictionary<ContributorKey, decimal>
        AllocateMoney(
            decimal amountToAllocate,
            decimal denominator,
            IReadOnlyList<NormalizedContributor>
                contributors)
    {
        var result =
            new Dictionary<ContributorKey, decimal>();

        if (amountToAllocate == 0)
        {
            return result;
        }

        if (denominator <= 0)
        {
            throw new InvalidOperationException(
                "Allocation denominator must be positive.");
        }

        if (contributors.Count == 0)
        {
            throw new InvalidOperationException(
                "At least one funded contributor is required.");
        }

        var allocated = 0m;

        for (
            var index = 0;
            index < contributors.Count;
            index++)
        {
            var contributor = contributors[index];

            decimal allocation;

            if (index == contributors.Count - 1)
            {
                allocation =
                    RoundMoney(
                        amountToAllocate - allocated);
            }
            else
            {
                allocation =
                    RoundMoney(
                        amountToAllocate *
                        contributor.ConfirmedCapital /
                        denominator);

                var remaining =
                    RoundMoney(
                        amountToAllocate - allocated);

                allocation =
                    Math.Min(
                        allocation,
                        remaining);
            }

            result[
                ContributorKey.Create(
                    contributor.ContributorRole,
                    contributor.ContributorCode)] =
                allocation;

            allocated =
                RoundMoney(
                    allocated + allocation);
        }

        if (allocated != amountToAllocate)
        {
            throw new InvalidOperationException(
                "Money allocation total is inconsistent.");
        }

        return result;
    }

    private static decimal CalculateCapitalRatio(
        decimal confirmedCapital,
        decimal totalCultivationCost)
    {
        if (confirmedCapital == 0)
        {
            return 0m;
        }

        return Math.Round(
            confirmedCapital /
                totalCultivationCost,
            8,
            MidpointRounding.AwayFromZero);
    }

    private static void ValidateReport(
        CropCycleProfitabilityReport profitability)
    {
        if (profitability.TotalCultivationCost <= 0)
        {
            throw new InvalidOperationException(
                "Zero-cost profit sharing is not supported.");
        }

        if (profitability.TotalConfirmedCapital !=
            profitability.TotalCultivationCost)
        {
            throw new InvalidOperationException(
                "Confirmed capital must equal total " +
                "cultivation cost before profit sharing.");
        }

        if (profitability.CapitalFundingGap != 0 ||
            profitability.CapitalFundingExcess != 0)
        {
            throw new InvalidOperationException(
                "Profitability funding position must be balanced.");
        }

        var expectedOutcome =
            profitability.NetProfit switch
            {
                < 0 => ProfitabilityOutcome.Loss,
                > 0 => ProfitabilityOutcome.Profit,
                _ => ProfitabilityOutcome.BreakEven
            };

        if (profitability.Outcome != expectedOutcome)
        {
            throw new InvalidOperationException(
                "Profitability outcome is inconsistent with " +
                "net profit.");
        }
    }

    private static void EnsureResultInvariants(
        ProfitSharingCalculationResult result)
    {
        if (result.Allocations.Count == 0)
        {
            throw new InvalidOperationException(
                "Profit sharing must contain allocations.");
        }

        if (result.Allocations
            .Select(allocation =>
                allocation.Sequence)
            .SequenceEqual(
                Enumerable.Range(
                    1,
                    result.Allocations.Count)) is false)
        {
            throw new InvalidOperationException(
                "Allocation sequence is not contiguous.");
        }

        if (result.Allocations
            .GroupBy(allocation =>
                ContributorKey.Create(
                    allocation.ContributorRole,
                    allocation.ContributorCodeSnapshot))
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Allocation contributor identities are not unique.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.ConfirmedCapital)) !=
            result.TotalCapital)
        {
            throw new InvalidOperationException(
                "Allocation capital does not equal total capital.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.CapitalRecovery)) !=
            result.TotalCapitalRecovery)
        {
            throw new InvalidOperationException(
                "Allocation recovery does not equal total recovery.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.CapitalLoss)) !=
            result.TotalCapitalLoss)
        {
            throw new InvalidOperationException(
                "Allocation loss does not equal total capital loss.");
        }

        var totalProfitShare =
            RoundMoney(
                result.TotalInvestorProfitShare +
                    result.TotalPartnerProfitShare);

        var expectedProfitShare =
            result.Outcome == ProfitabilityOutcome.Profit
                ? result.NetProfit
                : 0m;

        if (totalProfitShare != expectedProfitShare)
        {
            throw new InvalidOperationException(
                "Total profit share is inconsistent with outcome.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.TotalProfitShare)) !=
            totalProfitShare)
        {
            throw new InvalidOperationException(
                "Allocation profit share does not equal " +
                "settlement profit share.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.TotalPayout)) !=
            result.TotalPayout)
        {
            throw new InvalidOperationException(
                "Allocation payout does not equal total payout.");
        }

        if (result.TotalPayout != result.RecognizedRevenue)
        {
            throw new InvalidOperationException(
                "Total payout must equal recognized revenue.");
        }
    }

    private static void ValidateContributorRole(
        CapitalContributorRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                "Contributor role is unsupported.");
        }
    }

    private static string NormalizeContributorCode(
        string value,
        string parameterName)
    {
        var normalized =
            NormalizeRequiredText(
                value,
                MaxContributorCodeLength,
                parameterName,
                "Contributor code")
            .ToUpperInvariant();

        if (!ContributorCodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                "Contributor code format is invalid.",
                parameterName);
        }

        return normalized;
    }

    private static string NormalizeRequiredText(
        string value,
        int maximumLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} cannot be blank.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{displayName} cannot exceed " +
                $"{maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static decimal NormalizePositiveMoney(
        decimal value,
        string parameterName)
    {
        var normalized = RoundMoney(value);

        if (normalized <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Confirmed capital must be greater than zero.");
        }

        return normalized;
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    [GeneratedRegex(
        "^[A-Z0-9][A-Z0-9._-]{0,39}$")]
    private static partial Regex ContributorCodePattern();

    private sealed record NormalizedContributor(
        string ContributorCode,
        string ContributorName,
        CapitalContributorRole ContributorRole,
        decimal ConfirmedCapital);

    private sealed record ContributorKey(
        CapitalContributorRole ContributorRole,
        string ContributorCode)
    {
        public static ContributorKey Create(
            CapitalContributorRole role,
            string code)
        {
            return new ContributorKey(
                role,
                code);
        }
    }
}
