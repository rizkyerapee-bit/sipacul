using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.SalePayments.Contracts;

public sealed record SalePaymentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid SaleId,
    string Code,
    DateOnly PaymentDate,
    decimal Amount,
    SalePaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string? ReceivedFrom,
    string? Notes,
    SalePaymentStatus Status,
    bool IsCollectedRevenue,
    DateTime? ConfirmedAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
