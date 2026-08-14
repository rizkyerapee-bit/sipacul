using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;

public sealed record ProfitSharingSchemeFilter(
    ProfitSharingSchemeStatus? Status = null,
    string? Code = null);
