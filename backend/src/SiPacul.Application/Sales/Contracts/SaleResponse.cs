using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Sales.Contracts;

public sealed record SaleResponse(
    Guid Id,
    Guid OrganizationId,
    string Code,
    DateOnly SaleDate,
    string BuyerName,
    string? BuyerPhone,
    string? BuyerAddress,
    SalePaymentTerm PaymentTerm,
    DateOnly? DueDate,
    decimal DiscountAmount,
    decimal Subtotal,
    decimal TotalAmount,
    SaleStatus Status,
    DateTime? ConfirmedAt,
    string? CancellationReason,
    string? Notes,
    IReadOnlyList<SaleLineResponse> Lines,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
