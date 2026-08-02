using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Profitability.Persistence;

public sealed record ManualExpenseSource(
    CultivationExpenseStatus Status,
    decimal Amount);
