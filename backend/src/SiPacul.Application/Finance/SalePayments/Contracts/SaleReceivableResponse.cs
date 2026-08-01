using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Finance.SalePayments.Contracts;

public sealed record SaleReceivableResponse(
    Guid SaleId,
    string SaleCode,
    DateOnly SaleDate,
    string BuyerName,
    SalePaymentTerm PaymentTerm,
    DateOnly? DueDate,
    decimal SaleTotalAmount,
    decimal ConfirmedPaidAmount,
    decimal OutstandingReceivable,
    SalePaymentState PaymentState,
    bool IsFullyPaid,
    bool HasCollectedRevenue);
