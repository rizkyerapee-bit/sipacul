using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments.Mappings;

public static class ProfitSharingSchemeAssignmentMappings
{
    public static ProfitSharingSchemeAssignmentResponse ToResponse(
        this ProfitSharingSchemeAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ProfitSharingSchemeAssignmentResponse(
            assignment.Id,
            assignment.OrganizationId,
            assignment.CropCycleId,
            assignment.SourceSchemeId,
            assignment.SchemeFamilyId,
            assignment.SchemeCode,
            assignment.SchemeName,
            assignment.SchemeDescription,
            assignment.SchemeVersion,
            assignment.ResidualMethod,
            assignment.ResidualRecipientCode,
            assignment.AssignedAt,
            assignment.CreatedAt,
            assignment.UpdatedAt,
            assignment.Participants
                .OrderBy(participant => participant.Sequence)
                .Select(participant =>
                    new ProfitSharingSchemeAssignmentParticipantResponse(
                        participant.Id,
                        participant.ParticipantCode,
                        participant.ParticipantName,
                        participant.ParticipantRole,
                        participant.ParticipatesInResidualProfit,
                        participant.Sequence))
                .ToArray(),
            assignment.PriorityRules
                .OrderBy(rule => rule.Sequence)
                .Select(rule =>
                    new ProfitSharingSchemeAssignmentPriorityRuleResponse(
                        rule.Id,
                        rule.RuleCode,
                        rule.RuleType,
                        rule.RecipientCode,
                        rule.RateNumerator,
                        rule.RateDenominator,
                        rule.Sequence))
                .ToArray(),
            assignment.ResidualShares
                .OrderBy(share => share.Sequence)
                .Select(share =>
                    new ProfitSharingSchemeAssignmentResidualShareResponse(
                        share.Id,
                        share.RecipientCode,
                        share.RateNumerator,
                        share.RateDenominator,
                        share.Sequence))
                .ToArray());
    }
}
