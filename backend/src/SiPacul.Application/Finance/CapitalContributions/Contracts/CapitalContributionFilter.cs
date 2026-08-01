using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.CapitalContributions.Contracts;

public sealed record CapitalContributionFilter(
    CapitalContributionStatus? Status = null,
    CapitalContributorRole? ContributorRole = null,
    DateOnly? ContributionDateFrom = null,
    DateOnly? ContributionDateTo = null,
    string? ContributorCode = null,
    string? ContributorName = null);
