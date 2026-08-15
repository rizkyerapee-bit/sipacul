namespace SiPacul.Domain.Entities.Evaluations;

public sealed record SeasonEvaluationAttention(
    SeasonEvaluationAttentionCode Code,
    SeasonEvaluationAttentionSeverity Severity,
    decimal? Value);
