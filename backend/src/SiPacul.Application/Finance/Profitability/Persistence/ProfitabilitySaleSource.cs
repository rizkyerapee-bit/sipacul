using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ProfitabilitySaleSource(
    Guid SaleId,
    SaleStatus Status,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TotalAmount,
    IReadOnlyList<ProfitabilitySaleLineSource> Lines,
    IReadOnlyList<ProfitabilityPaymentSource> Payments);
