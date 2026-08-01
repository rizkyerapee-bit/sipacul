using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Contracts;

public sealed record CreateSaleRequest(
    string Code,
    DateOnly SaleDate,
    string BuyerName,
    string? BuyerPhone,
    string? BuyerAddress,
    SalePaymentTerm PaymentTerm,
    DateOnly? DueDate,
    string? Notes);
