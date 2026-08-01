using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.SalePayments.Contracts;

public sealed record SalePaymentFilter(
    SalePaymentStatus? Status = null,
    SalePaymentMethod? PaymentMethod = null,
    DateOnly? PaymentDateFrom = null,
    DateOnly? PaymentDateTo = null,
    string? ReceivedFrom = null);
