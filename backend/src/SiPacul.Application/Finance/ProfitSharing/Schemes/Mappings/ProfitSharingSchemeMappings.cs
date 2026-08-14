using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Mappings;

public static class ProfitSharingSchemeMappings
{
    public static ProfitSharingSchemeResponse ToResponse(
        this ProfitSharingScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        return new ProfitSharingSchemeResponse(
            scheme.Id,
            scheme.OrganizationId,
            scheme.SchemeFamilyId,
            scheme.Code,
            scheme.Name,
            scheme.Description,
            scheme.Version,
            scheme.Status,
            scheme.ResidualMethod,
            scheme.ResidualRecipientCode,
            scheme.ActivatedAt,
            scheme.SupersededAt,
            scheme.CreatedAt,
            scheme.UpdatedAt,
            scheme.Participants
                .OrderBy(participant => participant.Sequence)
                .Select(participant =>
                    new ProfitSharingSchemeParticipantResponse(
                        participant.Id,
                        participant.ParticipantCode,
                        participant.ParticipantName,
                        participant.ParticipantRole,
                        participant.ParticipatesInResidualProfit,
                        participant.Sequence))
                .ToArray(),
            scheme.PriorityRules
                .OrderBy(rule => rule.Sequence)
                .Select(rule =>
                    new ProfitSharingSchemePriorityRuleResponse(
                        rule.Id,
                        rule.RuleCode,
                        rule.RuleType,
                        rule.RecipientCode,
                        rule.RateNumerator,
                        rule.RateDenominator,
                        rule.Sequence))
                .ToArray(),
            scheme.ResidualShares
                .OrderBy(share => share.Sequence)
                .Select(share =>
                    new ProfitSharingSchemeResidualShareResponse(
                        share.Id,
                        share.RecipientCode,
                        share.RateNumerator,
                        share.RateDenominator,
                        share.Sequence))
                .ToArray());
    }

    public static IReadOnlyCollection<
        ProfitSharingSchemeParticipantDefinition>
        ToDefinitions(
            this IReadOnlyCollection<
                ProfitSharingSchemeParticipantRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Select(request =>
            {
                ArgumentNullException.ThrowIfNull(request);

                return new ProfitSharingSchemeParticipantDefinition(
                    request.ParticipantCode,
                    request.ParticipantName,
                    request.ParticipantRole,
                    request.ParticipatesInResidualProfit,
                    request.Sequence);
            })
            .ToArray();
    }

    public static IReadOnlyCollection<
        ProfitSharingSchemePriorityRuleDefinition>
        ToDefinitions(
            this IReadOnlyCollection<
                ProfitSharingSchemePriorityRuleRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Select(request =>
            {
                ArgumentNullException.ThrowIfNull(request);

                return new ProfitSharingSchemePriorityRuleDefinition(
                    request.RuleCode,
                    request.RuleType,
                    request.RecipientCode,
                    ProfitSharingRate.FromFraction(
                        request.RateNumerator,
                        request.RateDenominator),
                    request.Sequence);
            })
            .ToArray();
    }

    public static IReadOnlyCollection<
        ProfitSharingSchemeResidualShareDefinition>
        ToDefinitions(
            this IReadOnlyCollection<
                ProfitSharingSchemeResidualShareRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        return requests
            .Select(request =>
            {
                ArgumentNullException.ThrowIfNull(request);

                return new ProfitSharingSchemeResidualShareDefinition(
                    request.RecipientCode,
                    ProfitSharingRate.FromFraction(
                        request.RateNumerator,
                        request.RateDenominator),
                    request.Sequence);
            })
            .ToArray();
    }
}
