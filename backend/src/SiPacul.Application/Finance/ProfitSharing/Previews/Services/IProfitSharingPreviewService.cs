using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Previews.Services;

public interface IProfitSharingPreviewService
{
    Task<Result<ProfitSharingPreviewResponse>> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default);
}
