namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingWaterfallAllocationCalculation(
    string ParticipantCodeSnapshot,
    string ParticipantNameSnapshot,
    ProfitSharingParticipantRole ParticipantRole,
    decimal ConfirmedCapital,
    decimal CapitalRatio,
    bool ParticipatesInResidualProfit,
    decimal CapitalRecovery,
    decimal CapitalLoss,
    decimal ManagementProfitShare,
    decimal ReturnOnCapitalProfitShare,
    decimal ResidualProfitShare,
    decimal TotalProfitShare,
    decimal TotalPayout,
    int Sequence);
