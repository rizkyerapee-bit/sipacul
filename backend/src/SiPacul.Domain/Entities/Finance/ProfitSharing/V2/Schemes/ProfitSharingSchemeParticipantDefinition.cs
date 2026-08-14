namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed record ProfitSharingSchemeParticipantDefinition(
    string ParticipantCode,
    string ParticipantName,
    ProfitSharingParticipantRole ParticipantRole,
    bool ParticipatesInResidualProfit,
    int Sequence);
