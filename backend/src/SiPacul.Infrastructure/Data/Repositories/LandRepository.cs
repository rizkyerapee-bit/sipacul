using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Lands.Persistence;
using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class LandRepository :
    ILandRepository
{
    private readonly SiPaculDbContext _dbContext;

    public LandRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Land>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Lands
            .AsNoTracking()
            .Include(land =>
                land.Plots.OrderBy(
                    plot => plot.Code))
            .Where(land =>
                land.OrganizationId ==
                    organizationId &&
                !land.IsDeleted)
            .OrderBy(land => land.Name)
            .ThenBy(land => land.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Land?> GetByIdAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Lands
            .AsNoTracking()
            .Include(land =>
                land.Plots.OrderBy(
                    plot => plot.Code))
            .SingleOrDefaultAsync(
                land =>
                    land.OrganizationId ==
                        organizationId &&
                    land.Id == landId &&
                    !land.IsDeleted,
                cancellationToken);
    }

    public Task<Land?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Lands
            .Include(land =>
                land.Plots.OrderBy(
                    plot => plot.Code))
            .SingleOrDefaultAsync(
                land =>
                    land.OrganizationId ==
                        organizationId &&
                    land.Id == landId &&
                    !land.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Lands
            .AsNoTracking()
            .AnyAsync(
                land =>
                    land.OrganizationId ==
                        organizationId &&
                    land.Code == code &&
                    !land.IsDeleted,
                cancellationToken);
    }

    public void Add(Land land)
    {
        ArgumentNullException.ThrowIfNull(land);

        _dbContext.Lands.Add(land);
    }

    public void Remove(Land land)
    {
        ArgumentNullException.ThrowIfNull(land);

        _dbContext.Lands.Remove(land);
    }
}
