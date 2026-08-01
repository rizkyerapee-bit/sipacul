using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.SalePayments.Contracts;

public sealed record CreateSalePaymentRequest(
    string Code,
    DateOnly PaymentDate,
    decimal Amount,
    SalePaymentMethod PaymentMethod,
    string? ReferenceNumber,
    string? ReceivedFrom,
    string? Notes);
