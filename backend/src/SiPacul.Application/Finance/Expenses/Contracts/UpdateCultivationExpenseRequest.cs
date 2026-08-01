using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Expenses.Contracts;

public sealed record UpdateCultivationExpenseRequest(
    DateOnly ExpenseDate,
    CultivationExpenseCategory Category,
    string Description,
    decimal Amount,
    string? PayeeName,
    string? ReferenceNumber,
    string? EvidenceUrl,
    string? Notes);
