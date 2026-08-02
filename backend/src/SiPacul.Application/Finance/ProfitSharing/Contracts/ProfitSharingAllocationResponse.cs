using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.ProfitSharing.Contracts;

public sealed record ProfitSharingAllocationResponse(
    Guid Id,
    Guid OrganizationId,
    Guid ProfitSharingSettlementId,
    string ContributorCodeSnapshot,
    string ContributorNameSnapshot,
    CapitalContributorRole ContributorRole,
    decimal ConfirmedCapital,
    decimal CapitalRatio,
    decimal CapitalRecovery,
    decimal CapitalLoss,
    decimal ManagementProfitShare,
    decimal CapitalProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    int Sequence,
    DateTime CreatedAt);
