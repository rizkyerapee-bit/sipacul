using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Activities.Contracts;

public sealed record CompleteCultivationActivityRequest(
    DateOnly ActualCompletionDate,
    string? Outcome,
    string? IssueNotes,
    SopComplianceStatus SopComplianceStatus,
    string? DeviationReason);
