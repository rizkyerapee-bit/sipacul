using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;

public sealed record ProfitSharingWaterfallSettlementFilter(
    ProfitSharingWaterfallSettlementStatus? Status = null,
    DateOnly? SettlementDateFrom = null,
    DateOnly? SettlementDateTo = null);
