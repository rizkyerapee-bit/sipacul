using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.Expenses.Contracts;

public sealed record CultivationExpenseResponse(
    Guid Id,
    Guid OrganizationId,
    Guid CropCycleId,
    string Code,
    DateOnly ExpenseDate,
    CultivationExpenseCategory Category,
    string Description,
    decimal Amount,
    string? PayeeName,
    string? ReferenceNumber,
    string? EvidenceUrl,
    string? Notes,
    CultivationExpenseStatus Status,
    bool IsRecognizedCost,
    DateTime? ConfirmedAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
