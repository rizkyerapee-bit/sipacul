using SiPacul.Domain.Entities.Evaluations;

namespace SiPacul.Application.Evaluations.SeasonHistories.Contracts;

public sealed record SeasonEvaluationAttentionResponse(
    SeasonEvaluationAttentionCode Code,
    SeasonEvaluationAttentionSeverity Severity,
    decimal? Value);
