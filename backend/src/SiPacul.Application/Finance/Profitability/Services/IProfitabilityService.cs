using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Profitability.Services;

public interface IProfitabilityService
{
    Task<Result<CropCycleProfitabilityResponse>>
        GetCropCycleReportAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default);
}
