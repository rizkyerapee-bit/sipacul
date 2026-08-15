namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;

public sealed record FinalizeProfitSharingWaterfallSettlementRequest(
    string Code,
    DateOnly SettlementDate,
    string? Notes);
