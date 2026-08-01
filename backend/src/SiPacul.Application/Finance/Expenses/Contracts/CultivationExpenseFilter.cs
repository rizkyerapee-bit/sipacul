using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Expenses.Contracts;

public sealed record CultivationExpenseFilter(
    CultivationExpenseStatus? Status = null,
    CultivationExpenseCategory? Category = null,
    DateOnly? ExpenseDateFrom = null,
    DateOnly? ExpenseDateTo = null,
    string? PayeeName = null);
