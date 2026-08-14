namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

public sealed record ProfitSharingSchemeResidualShareDefinition(
    string RecipientCode,
    ProfitSharingRate Rate,
    int Sequence);
