namespace SiPacul.Application.Finance.ProfitSharing.Contracts;

public sealed record CreateProfitSharingSettlementRequest(
    string Code,
    DateOnly SettlementDate,
    string ManagingPartnerCode,
    string ManagingPartnerName,
    string? Notes);
