namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public sealed record ProfitSharingAllocationCalculation(
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
    int Sequence);
