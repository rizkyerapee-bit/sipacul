using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ProfitabilityPaymentSource(
    SalePaymentStatus Status,
    decimal Amount);
