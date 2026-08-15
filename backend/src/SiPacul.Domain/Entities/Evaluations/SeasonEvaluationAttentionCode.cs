namespace SiPacul.Domain.Entities.Evaluations;

public enum SeasonEvaluationAttentionCode
{
    CycleNotTerminal = 1,

    CycleCancelled = 2,

    LateStart = 3,

    LateHarvest = 4,

    ActivitiesIncomplete = 5,

    ActivitiesCancelled = 6,

    ActivityIssuesRecorded = 7,

    SopDeviationRecorded = 8,

    SopNotEvaluated = 9,

    NoConfirmedHarvest = 10,

    OutstandingReceivable = 11,

    BreakEven = 12,

    Loss = 13,

    CapitalFundingGap = 14
}
