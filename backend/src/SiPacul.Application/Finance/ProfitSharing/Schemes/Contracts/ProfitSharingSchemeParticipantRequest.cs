using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record ProfitSharingSchemeParticipantRequest(
    string ParticipantCode,
    string ParticipantName,
    ProfitSharingParticipantRole ParticipantRole,
    bool ParticipatesInResidualProfit,
    int Sequence);
