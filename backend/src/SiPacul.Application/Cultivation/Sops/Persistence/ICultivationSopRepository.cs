using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Application.Cultivation.Sops.Persistence;

public interface ICultivationSopRepository
{
    Task<IReadOnlyList<CultivationSop>> GetAllAsync(
        Guid organizationId,
        Guid? commodityId = null,
        CancellationToken cancellationToken = default);

    Task<CultivationSop?> GetByIdAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default);

    Task<CultivationSop?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        Guid commodityId,
        string name,
        Guid? excludedCultivationSopId = null,
        CancellationToken cancellationToken = default);

    void Add(CultivationSop cultivationSop);
}
