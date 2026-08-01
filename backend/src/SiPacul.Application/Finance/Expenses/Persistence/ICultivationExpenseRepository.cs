using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Expenses.Persistence;

public interface ICultivationExpenseRepository
{
    Task<IReadOnlyList<CultivationExpense>> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CultivationExpenseStatus? status = null,
        CultivationExpenseCategory? category = null,
        DateOnly? expenseDateFrom = null,
        DateOnly? expenseDateTo = null,
        string? payeeName = null,
        CancellationToken cancellationToken = default);

    Task<CultivationExpense?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<CultivationExpense?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default);

    void Add(CultivationExpense expense);
}
