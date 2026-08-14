namespace SiPacul.Domain.Entities.Finance.ProfitSharing.V2;

public sealed record ProfitSharingResidualShareInput(
    string RecipientCode,
    ProfitSharingRate Rate,
    int Sequence);
