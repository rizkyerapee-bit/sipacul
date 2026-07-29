using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Application.MasterData.CommodityCategories.Persistence;

public interface ICommodityCategoryRepository
{
    Task<IReadOnlyList<CommodityCategory>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<CommodityCategory?> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<CommodityCategory?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default);

    void Add(CommodityCategory category);
}
