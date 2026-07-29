using Microsoft.EntityFrameworkCore;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CommodityRepository :
    ICommodityRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CommodityRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Commodity>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Commodities
            .AsNoTracking()
            .Where(commodity =>
                commodity.OrganizationId ==
                    organizationId &&
                !commodity.IsDeleted)
            .OrderBy(commodity =>
                commodity.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Commodity?> GetByIdAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Commodities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Id == commodityId &&
                    !commodity.IsDeleted,
                cancellationToken);
    }

    public Task<Commodity?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid commodityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Commodities
            .SingleOrDefaultAsync(
                commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Id == commodityId &&
                    !commodity.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        CommodityCode code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Commodities
            .AsNoTracking()
            .AnyAsync(
                commodity =>
                    commodity.OrganizationId ==
                        organizationId &&
                    commodity.Code == code &&
                    !commodity.IsDeleted,
                cancellationToken);
    }

    public void Add(
        Commodity commodity)
    {
        ArgumentNullException.ThrowIfNull(commodity);

        _dbContext.Commodities.Add(commodity);
    }
}
