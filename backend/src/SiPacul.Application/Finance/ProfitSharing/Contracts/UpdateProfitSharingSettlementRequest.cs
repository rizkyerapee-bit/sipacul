namespace SiPacul.Application.Finance.ProfitSharing.Contracts;

public sealed record UpdateProfitSharingSettlementRequest(
    DateOnly SettlementDate,
    string? Notes);
