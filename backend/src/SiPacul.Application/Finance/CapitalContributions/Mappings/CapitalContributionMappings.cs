using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.CapitalContributions.Mappings;

public static class CapitalContributionMappings
{
    public static CapitalContributionResponse ToResponse(
        this CapitalContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        return new CapitalContributionResponse(
            contribution.Id,
            contribution.OrganizationId,
            contribution.CropCycleId,
            contribution.Code,
            contribution.ContributionDate,
            contribution.ContributorCode,
            contribution.ContributorName,
            contribution.ContributorRole,
            contribution.Amount,
            contribution.PaymentMethod,
            contribution.ReferenceNumber,
            contribution.Notes,
            contribution.Status,
            contribution.IsConfirmedCapital,
            contribution.IsInvestorCapital,
            contribution.IsPartnerCapital,
            contribution.ConfirmedAt,
            contribution.CancellationReason,
            contribution.CreatedAt,
            contribution.UpdatedAt);
    }
}
