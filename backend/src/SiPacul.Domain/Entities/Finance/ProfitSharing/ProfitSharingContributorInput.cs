namespace SiPacul.Domain.Entities.Finance.ProfitSharing;

public sealed record ProfitSharingContributorInput(
    string ContributorCode,
    string ContributorName,
    CapitalContributorRole ContributorRole,
    decimal ConfirmedCapital);
