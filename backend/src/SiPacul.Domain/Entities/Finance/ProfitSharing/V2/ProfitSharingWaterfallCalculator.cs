using System.Text.RegularExpressions;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public static partial class ProfitSharingWaterfallCalculator
{
    public const string CurrentCalculationVersion =
        "SIPACUL-PS-2";

    public const int MaxCodeLength = 40;

    public const int MaxNameLength = 150;

    private const decimal RateTolerance =
        0.00000001m;

    public static ProfitSharingWaterfallCalculationResult
        Calculate(
            CropCycleProfitabilityReport profitability,
            ProfitSharingWaterfallSchemeInput scheme)
    {
        ArgumentNullException.ThrowIfNull(profitability);
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(
            scheme.Participants);
        ArgumentNullException.ThrowIfNull(
            scheme.PriorityRules);
        ArgumentNullException.ThrowIfNull(
            scheme.ResidualPolicy);

        ValidateReport(profitability);

        var participants =
            NormalizeParticipants(
                scheme.Participants,
                profitability.TotalCultivationCost);

        var participantsByCode =
            participants.ToDictionary(
                participant => participant.ParticipantCode,
                StringComparer.Ordinal);

        var priorityRules =
            NormalizePriorityRules(
                scheme.PriorityRules,
                participantsByCode);

        var residualPolicy =
            NormalizeResidualPolicy(
                scheme.ResidualPolicy,
                participantsByCode);

        var priorityProfitByParticipant =
            participants.ToDictionary(
                participant => participant.ParticipantCode,
                _ => new PriorityProfitAccumulator(),
                StringComparer.Ordinal);

        var priorityAllocations =
            CalculatePriorityAllocations(
                profitability,
                priorityRules,
                participantsByCode,
                priorityProfitByParticipant,
                out var remainingProfit);

        var residualProfitByParticipant =
            CalculateResidualProfit(
                remainingProfit,
                residualPolicy,
                participants);

        var capitalRecoveryByParticipant =
            CalculateCapitalRecovery(
                profitability,
                participants);

        var allocations =
            participants
                .Select(participant =>
                {
                    var priorityProfit =
                        priorityProfitByParticipant[
                            participant.ParticipantCode];

                    var residualProfit =
                        residualProfitByParticipant[
                            participant.ParticipantCode];

                    var capitalRecovery =
                        capitalRecoveryByParticipant[
                            participant.ParticipantCode];

                    var capitalLoss =
                        RoundMoney(
                            participant.ConfirmedCapital -
                                capitalRecovery);

                    var totalProfitShare =
                        RoundMoney(
                            priorityProfit.ManagementShare +
                                priorityProfit.ReturnOnCapital +
                                residualProfit);

                    return new
                        ProfitSharingWaterfallAllocationCalculation(
                            participant.ParticipantCode,
                            participant.ParticipantName,
                            participant.ParticipantRole,
                            participant.ConfirmedCapital,
                            CalculateCapitalRatio(
                                participant.ConfirmedCapital,
                                profitability
                                    .TotalCultivationCost),
                            participant
                                .ParticipatesInResidualProfit,
                            capitalRecovery,
                            capitalLoss,
                            priorityProfit.ManagementShare,
                            priorityProfit.ReturnOnCapital,
                            residualProfit,
                            totalProfitShare,
                            RoundMoney(
                                capitalRecovery +
                                    totalProfitShare),
                            participant.Sequence);
                })
                .ToArray();

        var totalManagementProfitShare =
            RoundMoney(
                priorityAllocations
                    .Where(allocation =>
                        allocation.RuleType ==
                            ProfitSharingPriorityRuleType
                                .ManagementShare)
                    .Sum(allocation =>
                        allocation.AllocatedAmount));

        var totalReturnOnCapitalProfitShare =
            RoundMoney(
                priorityAllocations
                    .Where(allocation =>
                        allocation.RuleType ==
                            ProfitSharingPriorityRuleType
                                .ReturnOnCapital)
                    .Sum(allocation =>
                        allocation.AllocatedAmount));

        var totalPriorityProfitShare =
            RoundMoney(
                totalManagementProfitShare +
                    totalReturnOnCapitalProfitShare);

        var totalResidualProfitShare =
            RoundMoney(
                allocations.Sum(allocation =>
                    allocation.ResidualProfitShare));

        var totalProfitShare =
            RoundMoney(
                totalPriorityProfitShare +
                    totalResidualProfitShare);

        var result =
            new ProfitSharingWaterfallCalculationResult(
                profitability.OrganizationId,
                profitability.CropCycleId,
                profitability.RecognizedRevenue,
                profitability.TotalCultivationCost,
                profitability.NetProfit,
                profitability.Outcome,
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.ConfirmedCapital)),
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.CapitalRecovery)),
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.CapitalLoss)),
                totalManagementProfitShare,
                totalReturnOnCapitalProfitShare,
                totalPriorityProfitShare,
                totalResidualProfitShare,
                totalProfitShare,
                RoundMoney(
                    allocations.Sum(allocation =>
                        allocation.TotalPayout)),
                residualPolicy.Method,
                CurrentCalculationVersion,
                Array.AsReadOnly(priorityAllocations),
                Array.AsReadOnly(allocations));

        EnsureResultInvariants(result);

        return result;
    }

    private static NormalizedParticipant[]
        NormalizeParticipants(
            IReadOnlyCollection<
                ProfitSharingWaterfallParticipantInput> inputs,
            decimal totalCultivationCost)
    {
        if (inputs.Count == 0)
        {
            throw new ArgumentException(
                "At least one profit-sharing participant is required.",
                nameof(inputs));
        }

        var participants =
            inputs
                .Select(input =>
                {
                    ArgumentNullException.ThrowIfNull(input);

                    if (!Enum.IsDefined(input.ParticipantRole))
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(inputs),
                            "Participant role is unsupported.");
                    }

                    if (input.Sequence <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(inputs),
                            "Participant sequence must be positive.");
                    }

                    return new NormalizedParticipant(
                        NormalizeCode(
                            input.ParticipantCode,
                            nameof(inputs),
                            "Participant code"),
                        NormalizeRequiredText(
                            input.ParticipantName,
                            MaxNameLength,
                            nameof(inputs),
                            "Participant name"),
                        input.ParticipantRole,
                        NormalizeNonNegativeMoney(
                            input.ConfirmedCapital,
                            nameof(inputs),
                            "Confirmed capital"),
                        input.ParticipatesInResidualProfit,
                        input.Sequence);
                })
                .OrderBy(participant => participant.Sequence)
                .ToArray();

        EnsureContiguousSequence(
            participants.Select(participant =>
                participant.Sequence),
            nameof(inputs),
            "Participant");

        if (participants
            .GroupBy(
                participant => participant.ParticipantCode,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Participant codes must be unique.",
                nameof(inputs));
        }

        var totalCapital =
            RoundMoney(
                participants.Sum(participant =>
                    participant.ConfirmedCapital));

        if (totalCapital != totalCultivationCost)
        {
            throw new InvalidOperationException(
                "Participant capital must equal total " +
                "cultivation cost before profit sharing.");
        }

        return participants;
    }

    private static NormalizedPriorityRule[]
        NormalizePriorityRules(
            IReadOnlyCollection<
                ProfitSharingPriorityRuleInput> inputs,
            IReadOnlyDictionary<string, NormalizedParticipant>
                participantsByCode)
    {
        var rules =
            inputs
                .Select(input =>
                {
                    ArgumentNullException.ThrowIfNull(input);

                    if (!Enum.IsDefined(input.RuleType))
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(inputs),
                            "Priority rule type is unsupported.");
                    }

                    if (input.Sequence <= 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(inputs),
                            "Priority rule sequence must be positive.");
                    }

                    ValidatePositiveRate(
                        input.Rate,
                        nameof(inputs));

                    var recipientCode =
                        NormalizeCode(
                            input.RecipientCode,
                            nameof(inputs),
                            "Rule recipient code");

                    if (!participantsByCode.TryGetValue(
                            recipientCode,
                            out var participant))
                    {
                        throw new ArgumentException(
                            "Priority rule recipient does not exist.",
                            nameof(inputs));
                    }

                    if (input.RuleType ==
                            ProfitSharingPriorityRuleType
                                .ReturnOnCapital &&
                        participant.ConfirmedCapital <= 0)
                    {
                        throw new ArgumentException(
                            "Return-on-capital recipient must have " +
                            "confirmed capital.",
                            nameof(inputs));
                    }

                    return new NormalizedPriorityRule(
                        NormalizeCode(
                            input.RuleCode,
                            nameof(inputs),
                            "Priority rule code"),
                        input.RuleType,
                        recipientCode,
                        input.Rate,
                        input.Sequence);
                })
                .OrderBy(rule => rule.Sequence)
                .ToArray();

        EnsureContiguousSequence(
            rules.Select(rule => rule.Sequence),
            nameof(inputs),
            "Priority rule");

        if (rules
            .GroupBy(
                rule => rule.RuleCode,
                StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new ArgumentException(
                "Priority rule codes must be unique.",
                nameof(inputs));
        }

        return rules;
    }

    private static NormalizedResidualPolicy
        NormalizeResidualPolicy(
            ProfitSharingResidualPolicyInput input,
            IReadOnlyDictionary<string, NormalizedParticipant>
                participantsByCode)
    {
        if (!Enum.IsDefined(input.Method))
        {
            throw new ArgumentOutOfRangeException(
                nameof(input),
                "Residual method is unsupported.");
        }

        switch (input.Method)
        {
            case ProfitSharingResidualMethod
                .RemainderToParticipant:
            {
                var recipientCode =
                    NormalizeCode(
                        input.RecipientCode ?? string.Empty,
                        nameof(input),
                        "Residual recipient code");

                if (!participantsByCode.ContainsKey(
                        recipientCode))
                {
                    throw new ArgumentException(
                        "Residual recipient does not exist.",
                        nameof(input));
                }

                if (input.FixedShares.Count != 0)
                {
                    throw new ArgumentException(
                        "Remainder policy cannot contain fixed shares.",
                        nameof(input));
                }

                return new NormalizedResidualPolicy(
                    input.Method,
                    recipientCode,
                    Array.Empty<NormalizedResidualShare>());
            }

            case ProfitSharingResidualMethod.ProRataCapital:
            {
                if (!string.IsNullOrWhiteSpace(
                        input.RecipientCode) ||
                    input.FixedShares.Count != 0)
                {
                    throw new ArgumentException(
                        "Pro-rata policy cannot contain a recipient " +
                        "or fixed shares.",
                        nameof(input));
                }

                if (!participantsByCode.Values.Any(participant =>
                        participant.ParticipatesInResidualProfit &&
                        participant.ConfirmedCapital > 0))
                {
                    throw new ArgumentException(
                        "Pro-rata policy requires a funded residual " +
                        "participant.",
                        nameof(input));
                }

                return new NormalizedResidualPolicy(
                    input.Method,
                    null,
                    Array.Empty<NormalizedResidualShare>());
            }

            case ProfitSharingResidualMethod.FixedPercentage:
            {
                if (!string.IsNullOrWhiteSpace(
                        input.RecipientCode))
                {
                    throw new ArgumentException(
                        "Fixed-percentage policy cannot contain a " +
                        "single recipient.",
                        nameof(input));
                }

                if (input.FixedShares.Count == 0)
                {
                    throw new ArgumentException(
                        "Fixed-percentage policy requires shares.",
                        nameof(input));
                }

                var shares =
                    input.FixedShares
                        .Select(share =>
                        {
                            ArgumentNullException.ThrowIfNull(
                                share);

                            ValidatePositiveRate(
                                share.Rate,
                                nameof(input));

                            if (share.Sequence <= 0)
                            {
                                throw new
                                    ArgumentOutOfRangeException(
                                        nameof(input),
                                        "Residual share sequence must " +
                                        "be positive.");
                            }

                            var recipientCode =
                                NormalizeCode(
                                    share.RecipientCode,
                                    nameof(input),
                                    "Residual share recipient code");

                            if (!participantsByCode.ContainsKey(
                                    recipientCode))
                            {
                                throw new ArgumentException(
                                    "Residual share recipient does " +
                                    "not exist.",
                                    nameof(input));
                            }

                            return new NormalizedResidualShare(
                                recipientCode,
                                share.Rate,
                                share.Sequence);
                        })
                        .OrderBy(share => share.Sequence)
                        .ToArray();

                EnsureContiguousSequence(
                    shares.Select(share => share.Sequence),
                    nameof(input),
                    "Residual share");

                if (shares
                    .GroupBy(
                        share => share.RecipientCode,
                        StringComparer.Ordinal)
                    .Any(group => group.Count() > 1))
                {
                    throw new ArgumentException(
                        "Residual share recipients must be unique.",
                        nameof(input));
                }

                var totalRate =
                    shares.Sum(share => share.Rate.Value);

                if (Math.Abs(totalRate - 1m) >
                    RateTolerance)
                {
                    throw new ArgumentException(
                        "Fixed residual percentages must total 100%.",
                        nameof(input));
                }

                return new NormalizedResidualPolicy(
                    input.Method,
                    null,
                    shares);
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(input),
                    "Residual method is unsupported.");
        }
    }

    private static ProfitSharingPriorityAllocationCalculation[]
        CalculatePriorityAllocations(
            CropCycleProfitabilityReport profitability,
            IReadOnlyList<NormalizedPriorityRule> rules,
            IReadOnlyDictionary<string, NormalizedParticipant>
                participantsByCode,
            IReadOnlyDictionary<string, PriorityProfitAccumulator>
                profitByParticipant,
            out decimal remainingProfit)
    {
        remainingProfit =
            profitability.Outcome ==
                ProfitabilityOutcome.Profit
                ? profitability.NetProfit
                : 0m;

        var allocations =
            new List<
                ProfitSharingPriorityAllocationCalculation>();

        foreach (var rule in rules)
        {
            var participant =
                participantsByCode[rule.RecipientCode];

            var baseAmount =
                profitability.Outcome !=
                    ProfitabilityOutcome.Profit
                    ? 0m
                    : rule.RuleType switch
                    {
                        ProfitSharingPriorityRuleType
                            .ManagementShare =>
                            profitability.NetProfit,
                        ProfitSharingPriorityRuleType
                            .ReturnOnCapital =>
                            participant.ConfirmedCapital,
                        _ =>
                            throw new
                                InvalidOperationException(
                                    "Priority rule type is unsupported.")
                    };

            var requestedAmount =
                rule.Rate.ApplyTo(baseAmount);

            var allocatedAmount =
                RoundMoney(
                    Math.Min(
                        requestedAmount,
                        remainingProfit));

            var unallocatedAmount =
                RoundMoney(
                    requestedAmount - allocatedAmount);

            var accumulator =
                profitByParticipant[rule.RecipientCode];

            if (rule.RuleType ==
                ProfitSharingPriorityRuleType.ManagementShare)
            {
                accumulator.ManagementShare =
                    RoundMoney(
                        accumulator.ManagementShare +
                            allocatedAmount);
            }
            else
            {
                accumulator.ReturnOnCapital =
                    RoundMoney(
                        accumulator.ReturnOnCapital +
                            allocatedAmount);
            }

            remainingProfit =
                RoundMoney(
                    remainingProfit - allocatedAmount);

            allocations.Add(
                new ProfitSharingPriorityAllocationCalculation(
                    rule.RuleCode,
                    rule.RuleType,
                    participant.ParticipantCode,
                    participant.ParticipantName,
                    rule.Rate,
                    baseAmount,
                    requestedAmount,
                    allocatedAmount,
                    unallocatedAmount,
                    rule.Sequence));
        }

        return allocations.ToArray();
    }

    private static Dictionary<string, decimal>
        CalculateResidualProfit(
            decimal remainingProfit,
            NormalizedResidualPolicy policy,
            IReadOnlyList<NormalizedParticipant> participants)
    {
        var result =
            participants.ToDictionary(
                participant => participant.ParticipantCode,
                _ => 0m,
                StringComparer.Ordinal);

        if (remainingProfit == 0)
        {
            return result;
        }

        switch (policy.Method)
        {
            case ProfitSharingResidualMethod
                .RemainderToParticipant:
                result[policy.RecipientCode!] =
                    remainingProfit;
                break;

            case ProfitSharingResidualMethod.ProRataCapital:
            {
                var eligibleParticipants =
                    participants
                        .Where(participant =>
                            participant
                                .ParticipatesInResidualProfit &&
                            participant.ConfirmedCapital > 0)
                        .ToArray();

                var denominator =
                    RoundMoney(
                        eligibleParticipants.Sum(participant =>
                            participant.ConfirmedCapital));

                AllocateByWeight(
                    remainingProfit,
                    denominator,
                    eligibleParticipants,
                    participant =>
                        participant.ParticipantCode,
                    participant =>
                        participant.ConfirmedCapital,
                    result);
                break;
            }

            case ProfitSharingResidualMethod.FixedPercentage:
            {
                var allocated = 0m;

                for (
                    var index = 0;
                    index < policy.FixedShares.Count;
                    index++)
                {
                    var share = policy.FixedShares[index];

                    var amount =
                        index == policy.FixedShares.Count - 1
                            ? RoundMoney(
                                remainingProfit - allocated)
                            : share.Rate.ApplyTo(
                                remainingProfit);

                    var available =
                        RoundMoney(
                            remainingProfit - allocated);

                    amount = Math.Min(amount, available);

                    result[share.RecipientCode] =
                        amount;

                    allocated =
                        RoundMoney(
                            allocated + amount);
                }

                break;
            }

            default:
                throw new InvalidOperationException(
                    "Residual method is unsupported.");
        }

        if (RoundMoney(result.Values.Sum()) !=
            remainingProfit)
        {
            throw new InvalidOperationException(
                "Residual profit allocation is inconsistent.");
        }

        return result;
    }

    private static Dictionary<string, decimal>
        CalculateCapitalRecovery(
            CropCycleProfitabilityReport profitability,
            IReadOnlyList<NormalizedParticipant> participants)
    {
        var result =
            participants.ToDictionary(
                participant => participant.ParticipantCode,
                _ => 0m,
                StringComparer.Ordinal);

        var fundedParticipants =
            participants
                .Where(participant =>
                    participant.ConfirmedCapital > 0)
                .ToArray();

        var recoverableCapital =
            profitability.Outcome ==
                ProfitabilityOutcome.Loss
                ? RoundMoney(
                    Math.Min(
                        profitability.RecognizedRevenue,
                        profitability.TotalCultivationCost))
                : profitability.TotalCultivationCost;

        AllocateByWeight(
            recoverableCapital,
            profitability.TotalCultivationCost,
            fundedParticipants,
            participant => participant.ParticipantCode,
            participant => participant.ConfirmedCapital,
            result);

        return result;
    }

    private static void AllocateByWeight<T>(
        decimal amountToAllocate,
        decimal denominator,
        IReadOnlyList<T> recipients,
        Func<T, string> keySelector,
        Func<T, decimal> weightSelector,
        IDictionary<string, decimal> destination)
    {
        if (amountToAllocate == 0)
        {
            return;
        }

        if (denominator <= 0 || recipients.Count == 0)
        {
            throw new InvalidOperationException(
                "Weighted allocation requires funded recipients.");
        }

        var allocated = 0m;

        for (
            var index = 0;
            index < recipients.Count;
            index++)
        {
            var recipient = recipients[index];

            var allocation =
                index == recipients.Count - 1
                    ? RoundMoney(
                        amountToAllocate - allocated)
                    : RoundMoney(
                        amountToAllocate *
                            weightSelector(recipient) /
                            denominator);

            var available =
                RoundMoney(
                    amountToAllocate - allocated);

            allocation = Math.Min(allocation, available);

            destination[keySelector(recipient)] =
                allocation;

            allocated =
                RoundMoney(
                    allocated + allocation);
        }

        if (allocated != amountToAllocate)
        {
            throw new InvalidOperationException(
                "Weighted allocation total is inconsistent.");
        }
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
                "Confirmed capital must equal total cultivation " +
                "cost before profit sharing.");
        }

        if (profitability.CapitalFundingGap != 0 ||
            profitability.CapitalFundingExcess != 0)
        {
            throw new InvalidOperationException(
                "Profitability funding position must be balanced.");
        }

        var expectedNetProfit =
            RoundMoney(
                profitability.RecognizedRevenue -
                    profitability.TotalCultivationCost);

        if (profitability.NetProfit != expectedNetProfit)
        {
            throw new InvalidOperationException(
                "Profitability net profit is inconsistent.");
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
                "Profitability outcome is inconsistent with net profit.");
        }
    }

    private static void EnsureResultInvariants(
        ProfitSharingWaterfallCalculationResult result)
    {
        if (result.CalculationVersion !=
            CurrentCalculationVersion)
        {
            throw new InvalidOperationException(
                "Waterfall calculation version is inconsistent.");
        }

        EnsureContiguousSequence(
            result.Allocations.Select(allocation =>
                allocation.Sequence),
            nameof(result),
            "Allocation");

        EnsureContiguousSequence(
            result.PriorityAllocations.Select(allocation =>
                allocation.Sequence),
            nameof(result),
            "Priority allocation");

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.ConfirmedCapital)) !=
            result.TotalCapital)
        {
            throw new InvalidOperationException(
                "Allocation capital does not equal total capital.");
        }

        if (result.TotalCapital !=
            result.TotalCultivationCost)
        {
            throw new InvalidOperationException(
                "Total capital does not equal cultivation cost.");
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

        if (RoundMoney(
                result.TotalCapitalRecovery +
                    result.TotalCapitalLoss) !=
            result.TotalCapital)
        {
            throw new InvalidOperationException(
                "Capital recovery and loss do not equal total capital.");
        }

        var expectedProfitShare =
            result.Outcome == ProfitabilityOutcome.Profit
                ? result.NetProfit
                : 0m;

        if (result.TotalProfitShare != expectedProfitShare)
        {
            throw new InvalidOperationException(
                "Total profit share is inconsistent with outcome.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.TotalProfitShare)) !=
            result.TotalProfitShare)
        {
            throw new InvalidOperationException(
                "Participant profit shares are inconsistent.");
        }

        if (RoundMoney(
                result.TotalPriorityProfitShare +
                    result.TotalResidualProfitShare) !=
            result.TotalProfitShare)
        {
            throw new InvalidOperationException(
                "Priority and residual profit shares are inconsistent.");
        }

        if (RoundMoney(
                result.TotalManagementProfitShare +
                    result.TotalReturnOnCapitalProfitShare) !=
            result.TotalPriorityProfitShare)
        {
            throw new InvalidOperationException(
                "Priority profit-share components are inconsistent.");
        }

        if (RoundMoney(
                result.PriorityAllocations.Sum(allocation =>
                    allocation.AllocatedAmount)) !=
            result.TotalPriorityProfitShare)
        {
            throw new InvalidOperationException(
                "Priority allocations are inconsistent.");
        }

        if (result.PriorityAllocations.Any(allocation =>
                allocation.AllocatedAmount < 0 ||
                allocation.UnallocatedAmount < 0 ||
                RoundMoney(
                    allocation.AllocatedAmount +
                        allocation.UnallocatedAmount) !=
                    allocation.RequestedAmount))
        {
            throw new InvalidOperationException(
                "Priority allocation amounts are invalid.");
        }

        if (RoundMoney(
                result.Allocations.Sum(allocation =>
                    allocation.TotalPayout)) !=
            result.TotalPayout)
        {
            throw new InvalidOperationException(
                "Participant payouts are inconsistent.");
        }

        if (result.TotalPayout !=
            result.RecognizedRevenue)
        {
            throw new InvalidOperationException(
                "Total payout must equal recognized revenue.");
        }
    }

    private static void ValidatePositiveRate(
        ProfitSharingRate rate,
        string parameterName)
    {
        if (rate.Denominator <= 0 ||
            rate.Numerator <= 0 ||
            rate.Numerator > rate.Denominator)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Rule rate must be greater than zero and no " +
                "greater than one.");
        }
    }

    private static void EnsureContiguousSequence(
        IEnumerable<int> sequences,
        string parameterName,
        string displayName)
    {
        var values = sequences.ToArray();

        if (!values.SequenceEqual(
                Enumerable.Range(1, values.Length)))
        {
            throw new ArgumentException(
                $"{displayName} sequence must be contiguous.",
                parameterName);
        }
    }

    private static string NormalizeCode(
        string value,
        string parameterName,
        string displayName)
    {
        var normalized =
            NormalizeRequiredText(
                value,
                MaxCodeLength,
                parameterName,
                displayName)
            .ToUpperInvariant();

        if (!CodePattern().IsMatch(normalized))
        {
            throw new ArgumentException(
                $"{displayName} format is invalid.",
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

    private static decimal NormalizeNonNegativeMoney(
        decimal value,
        string parameterName,
        string displayName)
    {
        var normalized = RoundMoney(value);

        if (normalized < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{displayName} cannot be negative.");
        }

        return normalized;
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
            confirmedCapital / totalCultivationCost,
            8,
            MidpointRounding.AwayFromZero);
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._-]{0,39}$")]
    private static partial Regex CodePattern();

    private sealed record NormalizedParticipant(
        string ParticipantCode,
        string ParticipantName,
        ProfitSharingParticipantRole ParticipantRole,
        decimal ConfirmedCapital,
        bool ParticipatesInResidualProfit,
        int Sequence);

    private sealed record NormalizedPriorityRule(
        string RuleCode,
        ProfitSharingPriorityRuleType RuleType,
        string RecipientCode,
        ProfitSharingRate Rate,
        int Sequence);

    private sealed record NormalizedResidualShare(
        string RecipientCode,
        ProfitSharingRate Rate,
        int Sequence);

    private sealed record NormalizedResidualPolicy(
        ProfitSharingResidualMethod Method,
        string? RecipientCode,
        IReadOnlyList<NormalizedResidualShare> FixedShares);

    private sealed class PriorityProfitAccumulator
    {
        public decimal ManagementShare { get; set; }

        public decimal ReturnOnCapital { get; set; }
    }
}
