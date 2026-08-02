using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record CapitalContributionSource(
    CapitalContributionStatus Status,
    CapitalContributorRole ContributorRole,
    decimal Amount);
