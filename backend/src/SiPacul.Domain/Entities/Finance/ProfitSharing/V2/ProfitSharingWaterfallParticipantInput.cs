namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingWaterfallParticipantInput(
    string ParticipantCode,
    string ParticipantName,
    ProfitSharingParticipantRole ParticipantRole,
    decimal ConfirmedCapital,
    bool ParticipatesInResidualProfit,
    int Sequence);
