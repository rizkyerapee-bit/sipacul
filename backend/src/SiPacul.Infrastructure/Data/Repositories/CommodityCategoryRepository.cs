using Microsoft.EntityFrameworkCore;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CommodityCategoryRepository :
    ICommodityCategoryRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CommodityCategoryRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommodityCategory>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.CommodityCategories
            .AsNoTracking()
            .Where(category =>
                category.OrganizationId == organizationId &&
                !category.IsDeleted)
            .OrderBy(category =>
                category.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<CommodityCategory?> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommodityCategories
            .AsNoTracking()
            .SingleOrDefaultAsync(
                category =>
                    category.OrganizationId ==
                        organizationId &&
                    category.Id == categoryId &&
                    !category.IsDeleted,
                cancellationToken);
    }

    public Task<CommodityCategory?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommodityCategories
            .SingleOrDefaultAsync(
                category =>
                    category.OrganizationId ==
                        organizationId &&
                    category.Id == categoryId &&
                    !category.IsDeleted,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        string name,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommodityCategories
            .AsNoTracking()
            .AnyAsync(
                category =>
                    category.OrganizationId ==
                        organizationId &&
                    category.Name == name &&
                    !category.IsDeleted &&
                    (
                        excludedCategoryId == null ||
                        category.Id !=
                            excludedCategoryId.Value
                    ),
                cancellationToken);
    }

    public void Add(
        CommodityCategory category)
    {
        ArgumentNullException.ThrowIfNull(category);

        _dbContext.CommodityCategories.Add(category);
    }
}
