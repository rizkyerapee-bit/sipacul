using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Expenses.Mappings;

public static class CultivationExpenseMappings
{
    public static CultivationExpenseResponse ToResponse(
        this CultivationExpense expense)
    {
        ArgumentNullException.ThrowIfNull(expense);

        return new CultivationExpenseResponse(
            expense.Id,
            expense.OrganizationId,
            expense.CropCycleId,
            expense.Code,
            expense.ExpenseDate,
            expense.Category,
            expense.Description,
            expense.Amount,
            expense.PayeeName,
            expense.ReferenceNumber,
            expense.EvidenceUrl,
            expense.Notes,
            expense.Status,
            expense.IsRecognizedCost,
            expense.ConfirmedAt,
            expense.CancellationReason,
            expense.CreatedAt,
            expense.UpdatedAt);
    }
}
