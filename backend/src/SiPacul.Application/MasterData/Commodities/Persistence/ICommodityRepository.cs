using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Application.MasterData.Commodities.Persistence;

public interface ICommodityRepository
{
    Task<IReadOnlyList<Commodity>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Commodity?> GetByIdAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default);

    Task<Commodity?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        CommodityCode code,
        CancellationToken cancellationToken = default);

    void Add(Commodity commodity);
}
