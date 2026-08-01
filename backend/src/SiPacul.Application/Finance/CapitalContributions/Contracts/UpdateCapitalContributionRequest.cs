using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.CapitalContributions.Contracts;

public sealed record UpdateCapitalContributionRequest(
    DateOnly ContributionDate,
    string ContributorCode,
    string ContributorName,
    CapitalContributorRole ContributorRole,
    decimal Amount,
    CapitalContributionPaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string? Notes);
