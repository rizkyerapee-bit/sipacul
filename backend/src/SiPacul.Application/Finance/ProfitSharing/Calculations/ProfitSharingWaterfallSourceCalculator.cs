using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.Profitability;

namespace SiPacul.Application.Finance.ProfitSharing.Calculations;

public static class ProfitSharingWaterfallSourceCalculator
{
    public static ProfitSharingWaterfallSourceCalculation Calculate(
        ProfitSharingSchemeAssignment assignment,
        ProfitabilitySourceSnapshot sourceSnapshot,
        IReadOnlyCollection<CapitalContribution> contributions,
        DateTime generatedAt)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        ArgumentNullException.ThrowIfNull(sourceSnapshot);
        ArgumentNullException.ThrowIfNull(contributions);

        try
        {
            var profitability =
                CropCycleProfitabilityReport.Calculate(
                    sourceSnapshot.ToInput(generatedAt));

            var inputResult = BuildWaterfallInput(
                assignment,
                contributions,
                profitability);

            if (inputResult.Failure !=
                ProfitSharingWaterfallSourceFailure.None)
            {
                return ProfitSharingWaterfallSourceCalculation.Failed(
                    inputResult.Failure,
                    inputResult.ContributorCode);
            }

            var calculation =
                ProfitSharingWaterfallCalculator.Calculate(
                    profitability,
                    inputResult.Input!);

            return ProfitSharingWaterfallSourceCalculation.Succeeded(
                profitability,
                calculation);
        }
        catch (Exception exception)
            when (exception is ArgumentException or
                  InvalidOperationException or
                  OverflowException)
        {
            return ProfitSharingWaterfallSourceCalculation.Failed(
                ProfitSharingWaterfallSourceFailure
                    .CalculationUnavailable,
                message: exception.Message);
        }
    }

    private static WaterfallInputResult BuildWaterfallInput(
        ProfitSharingSchemeAssignment assignment,
        IReadOnlyCollection<CapitalContribution> contributions,
        CropCycleProfitabilityReport profitability)
    {
        var participantByCode = assignment.Participants.ToDictionary(
            participant => participant.ParticipantCode,
            StringComparer.Ordinal);

        var capitalByParticipant = assignment.Participants.ToDictionary(
            participant => participant.ParticipantCode,
            _ => 0m,
            StringComparer.Ordinal);

        var confirmedContributions = contributions
            .Where(contribution => contribution.IsConfirmedCapital)
            .ToArray();

        var groupedContributions = confirmedContributions
            .GroupBy(
                contribution => contribution.ContributorCode,
                StringComparer.Ordinal)
            .ToArray();

        foreach (var group in groupedContributions)
        {
            if (!participantByCode.TryGetValue(
                    group.Key,
                    out var participant))
            {
                return WaterfallInputResult.Failed(
                    ProfitSharingWaterfallSourceFailure
                        .CapitalNotInScheme,
                    group.Key);
            }

            if (group.Select(contribution =>
                        contribution.ContributorRole)
                    .Distinct()
                    .Count() != 1 ||
                group.Select(contribution =>
                        contribution.ContributorName)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != 1)
            {
                return WaterfallInputResult.Failed(
                    ProfitSharingWaterfallSourceFailure
                        .CapitalIdentityConflict,
                    group.Key);
            }

            var contributorRole = group.First().ContributorRole;

            if (!IsCompatibleRole(
                    participant.ParticipantRole,
                    contributorRole))
            {
                return WaterfallInputResult.Failed(
                    ProfitSharingWaterfallSourceFailure
                        .CapitalRoleMismatch,
                    group.Key);
            }

            capitalByParticipant[group.Key] = RoundMoney(
                group.Sum(contribution => contribution.Amount));
        }

        var detailedCapital = RoundMoney(
            capitalByParticipant.Values.Sum());

        var detailedInvestorCapital = RoundMoney(
            confirmedContributions
                .Where(contribution =>
                    contribution.ContributorRole ==
                        CapitalContributorRole.Investor)
                .Sum(contribution => contribution.Amount));

        var detailedPartnerCapital = RoundMoney(
            confirmedContributions
                .Where(contribution =>
                    contribution.ContributorRole ==
                        CapitalContributorRole.Partner)
                .Sum(contribution => contribution.Amount));

        if (detailedCapital != profitability.TotalConfirmedCapital ||
            detailedInvestorCapital !=
                profitability.ConfirmedInvestorCapital ||
            detailedPartnerCapital !=
                profitability.ConfirmedPartnerCapital)
        {
            return WaterfallInputResult.Failed(
                ProfitSharingWaterfallSourceFailure
                    .SourceDataChanged);
        }

        var participants = assignment.Participants
            .OrderBy(participant => participant.Sequence)
            .Select(participant =>
                new ProfitSharingWaterfallParticipantInput(
                    participant.ParticipantCode,
                    participant.ParticipantName,
                    participant.ParticipantRole,
                    capitalByParticipant[
                        participant.ParticipantCode],
                    participant.ParticipatesInResidualProfit,
                    participant.Sequence))
            .ToArray();

        var priorityRules = assignment.PriorityRules
            .OrderBy(rule => rule.Sequence)
            .Select(rule =>
                new ProfitSharingPriorityRuleInput(
                    rule.RuleCode,
                    rule.RuleType,
                    rule.RecipientCode,
                    ProfitSharingRate.FromFraction(
                        rule.RateNumerator,
                        rule.RateDenominator),
                    rule.Sequence))
            .ToArray();

        return WaterfallInputResult.Succeeded(
            new ProfitSharingWaterfallSchemeInput(
                participants,
                priorityRules,
                BuildResidualPolicy(assignment)));
    }

    private static ProfitSharingResidualPolicyInput BuildResidualPolicy(
        ProfitSharingSchemeAssignment assignment)
    {
        return assignment.ResidualMethod switch
        {
            ProfitSharingResidualMethod.RemainderToParticipant =>
                ProfitSharingResidualPolicyInput
                    .RemainderToParticipant(
                        assignment.ResidualRecipientCode ??
                        throw new InvalidOperationException(
                            "Residual recipient is missing from the " +
                            "assigned scheme.")),

            ProfitSharingResidualMethod.ProRataCapital =>
                ProfitSharingResidualPolicyInput.ProRataCapital(),

            ProfitSharingResidualMethod.FixedPercentage =>
                ProfitSharingResidualPolicyInput.FixedPercentage(
                    assignment.ResidualShares
                        .OrderBy(share => share.Sequence)
                        .Select(share =>
                            new ProfitSharingResidualShareInput(
                                share.RecipientCode,
                                ProfitSharingRate.FromFraction(
                                    share.RateNumerator,
                                    share.RateDenominator),
                                share.Sequence))
                        .ToArray()),

            _ => throw new InvalidOperationException(
                "Residual method is unsupported.")
        };
    }

    private static bool IsCompatibleRole(
        ProfitSharingParticipantRole participantRole,
        CapitalContributorRole contributorRole)
    {
        return participantRole switch
        {
            ProfitSharingParticipantRole.Company =>
                contributorRole == CapitalContributorRole.Investor,

            ProfitSharingParticipantRole.PassiveInvestor =>
                contributorRole == CapitalContributorRole.Investor,

            ProfitSharingParticipantRole.ManagingPartner =>
                contributorRole == CapitalContributorRole.Partner,

            ProfitSharingParticipantRole.Other => true,
            _ => false
        };
    }

    private static decimal RoundMoney(decimal value)
    {
        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private sealed record WaterfallInputResult(
        ProfitSharingWaterfallSchemeInput? Input,
        ProfitSharingWaterfallSourceFailure Failure,
        string? ContributorCode)
    {
        public static WaterfallInputResult Succeeded(
            ProfitSharingWaterfallSchemeInput input)
        {
            return new WaterfallInputResult(
                input,
                ProfitSharingWaterfallSourceFailure.None,
                null);
        }

        public static WaterfallInputResult Failed(
            ProfitSharingWaterfallSourceFailure failure,
            string? contributorCode = null)
        {
            return new WaterfallInputResult(
                null,
                failure,
                contributorCode);
        }
    }
}
