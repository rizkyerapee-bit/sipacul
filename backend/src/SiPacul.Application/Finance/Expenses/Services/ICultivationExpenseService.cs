using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Expenses.Services;

public interface ICultivationExpenseService
{
    Task<Result<CultivationExpenseResponse>> CreateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CreateCultivationExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CultivationExpenseResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationExpenseFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<CultivationExpenseResponse>> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationExpenseResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        UpdateCultivationExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationExpenseResponse>> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<Result<CultivationExpenseResponse>> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancelCultivationExpenseRequest request,
        CancellationToken cancellationToken = default);
}
