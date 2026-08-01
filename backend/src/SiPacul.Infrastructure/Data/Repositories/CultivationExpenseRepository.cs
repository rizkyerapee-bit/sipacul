using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.Expenses.Persistence;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CultivationExpenseRepository :
    ICultivationExpenseRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CultivationExpenseRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CultivationExpense>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationExpenseStatus? status = null,
            CultivationExpenseCategory? category = null,
            DateOnly? expenseDateFrom = null,
            DateOnly? expenseDateTo = null,
            string? payeeName = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CultivationExpense> query =
            _dbContext.CultivationExpenses
                .AsNoTracking()
                .Where(expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    !expense.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(expense =>
                expense.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(expense =>
                expense.Category == category.Value);
        }

        if (expenseDateFrom.HasValue)
        {
            query = query.Where(expense =>
                expense.ExpenseDate >=
                    expenseDateFrom.Value);
        }

        if (expenseDateTo.HasValue)
        {
            query = query.Where(expense =>
                expense.ExpenseDate <=
                    expenseDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(payeeName))
        {
            var pattern = $"%{payeeName.Trim()}%";

            query = query.Where(expense =>
                expense.PayeeName != null &&
                EF.Functions.ILike(
                    expense.PayeeName,
                    pattern));
        }

        return await query
            .OrderBy(expense => expense.ExpenseDate)
            .ThenBy(expense => expense.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<CultivationExpense?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationExpenses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    expense.Id == expenseId &&
                    !expense.IsDeleted,
                cancellationToken);
    }

    public Task<CultivationExpense?>
        GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationExpenses
            .SingleOrDefaultAsync(
                expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    expense.Id == expenseId &&
                    !expense.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationExpenses
            .AsNoTracking()
            .AnyAsync(
                expense =>
                    expense.OrganizationId ==
                        organizationId &&
                    expense.CropCycleId ==
                        cropCycleId &&
                    expense.Code == code &&
                    !expense.IsDeleted,
                cancellationToken);
    }

    public void Add(CultivationExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        _dbContext.CultivationExpenses.Add(expense);
    }
}
