using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Contracts;

public sealed record UpdateSaleRequest(
    DateOnly SaleDate,
    string BuyerName,
    string? BuyerPhone,
    string? BuyerAddress,
    SalePaymentTerm PaymentTerm,
    DateOnly? DueDate,
    decimal DiscountAmount,
    string? Notes);
