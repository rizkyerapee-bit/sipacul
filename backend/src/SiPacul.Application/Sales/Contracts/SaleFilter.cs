using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Contracts;

public sealed record SaleFilter(
    SaleStatus? Status = null,
    DateOnly? SaleDateFrom = null,
    DateOnly? SaleDateTo = null,
    SalePaymentTerm? PaymentTerm = null,
    string? BuyerName = null);
