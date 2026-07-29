using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.Commodities.Services;

public interface ICommodityService
{
    Task<Result<CommodityResponse>> CreateAsync(
        Guid organizationId,
        CreateCommodityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CommodityResponse>>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityResponse>> GetByIdAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityResponse>> UpdateAsync(
        Guid organizationId,
        Guid commodityId,
        UpdateCommodityRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityResponse>> ActivateAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityResponse>> DeactivateAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default);
}
