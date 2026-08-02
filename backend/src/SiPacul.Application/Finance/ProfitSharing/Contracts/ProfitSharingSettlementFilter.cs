using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Application.Finance.ProfitSharing.Contracts;

public sealed record ProfitSharingSettlementFilter(
    ProfitSharingSettlementStatus? Status = null,
    DateOnly? SettlementDateFrom = null,
    DateOnly? SettlementDateTo = null,
    string? ManagingPartnerCode = null);
