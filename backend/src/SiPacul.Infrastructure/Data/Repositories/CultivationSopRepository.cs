using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CultivationSopRepository :
    ICultivationSopRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CultivationSopRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CultivationSop>>
        GetAllAsync(
            Guid organizationId,
            Guid? commodityId = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CultivationSop> query =
            _dbContext.CultivationSops
                .AsNoTracking()
                .Include(sop =>
                    sop.Steps.OrderBy(
                        step => step.Sequence))
                .Where(sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    !sop.IsDeleted);

        if (commodityId.HasValue)
        {
            query = query.Where(sop =>
                sop.CommodityId ==
                    commodityId.Value);
        }

        return await query
            .OrderBy(sop => sop.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<CultivationSop?> GetByIdAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationSops
            .AsNoTracking()
            .Include(sop =>
                sop.Steps.OrderBy(
                    step => step.Sequence))
            .SingleOrDefaultAsync(
                sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    sop.Id == cultivationSopId &&
                    !sop.IsDeleted,
                cancellationToken);
    }

    public Task<CultivationSop?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationSops
            .Include(sop =>
                sop.Steps.OrderBy(
                    step => step.Sequence))
            .SingleOrDefaultAsync(
                sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    sop.Id == cultivationSopId &&
                    !sop.IsDeleted,
                cancellationToken);
    }

    public Task<bool> NameExistsAsync(
        Guid organizationId,
        Guid commodityId,
        string name,
        Guid? excludedCultivationSopId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationSops
            .AsNoTracking()
            .AnyAsync(
                sop =>
                    sop.OrganizationId ==
                        organizationId &&
                    sop.CommodityId ==
                        commodityId &&
                    sop.Name == name &&
                    !sop.IsDeleted &&
                    (
                        excludedCultivationSopId == null ||
                        sop.Id !=
                            excludedCultivationSopId.Value
                    ),
                cancellationToken);
    }

    public void Add(
        CultivationSop cultivationSop)
    {
        ArgumentNullException.ThrowIfNull(
            cultivationSop);

        _dbContext.CultivationSops.Add(
            cultivationSop);
    }
}
