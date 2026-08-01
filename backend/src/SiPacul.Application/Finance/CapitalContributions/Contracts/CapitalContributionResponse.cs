using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.CapitalContributions.Contracts;

public sealed record CapitalContributionResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    string Code,
    DateOnly ContributionDate,
    string ContributorCode,
    string ContributorName,
    CapitalContributorRole ContributorRole,
    decimal Amount,
    CapitalContributionPaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string? Notes,
    CapitalContributionStatus Status,
    bool IsConfirmedCapital,
    bool IsInvestorCapital,
    bool IsPartnerCapital,
    DateTime? ConfirmedAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
