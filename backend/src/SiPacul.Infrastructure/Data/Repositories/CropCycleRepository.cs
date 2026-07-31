using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CropCycleRepository :
    ICropCycleRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CropCycleRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CropCycle>> GetAllAsync(
        Guid organizationId,
        CropCycleStatus? status = null,
        Guid? commodityId = null,
        Guid? landId = null,
        Guid? landPlotId = null,
        DateOnly? plannedStartFrom = null,
        DateOnly? plannedStartTo = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CropCycle> query =
            _dbContext.CropCycles
                .AsNoTracking()
                .Where(cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    !cropCycle.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.Status == status.Value);
        }

        if (commodityId.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.CommodityId ==
                    commodityId.Value);
        }

        if (landId.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.LandId == landId.Value);
        }

        if (landPlotId.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.LandPlotId ==
                    landPlotId.Value);
        }

        if (plannedStartFrom.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.PlannedStartDate >=
                    plannedStartFrom.Value);
        }

        if (plannedStartTo.HasValue)
        {
            query = query.Where(cropCycle =>
                cropCycle.PlannedStartDate <=
                    plannedStartTo.Value);
        }

        return await query
            .OrderBy(cropCycle =>
                cropCycle.PlannedStartDate)
            .ThenBy(cropCycle => cropCycle.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<CropCycle?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.Id == cropCycleId &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public Task<CropCycle?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .SingleOrDefaultAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.Id == cropCycleId &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.Code == code &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasScheduleConflictAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        DateOnly plannedStartDate,
        DateOnly expectedHarvestDate,
        Guid? excludedCropCycleId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    cropCycle.Status !=
                        CropCycleStatus.Cancelled &&
                    !cropCycle.IsDeleted &&
                    (
                        excludedCropCycleId == null ||
                        cropCycle.Id !=
                            excludedCropCycleId.Value
                    ) &&
                    cropCycle.PlannedStartDate <=
                        expectedHarvestDate &&
                    plannedStartDate <=
                        cropCycle.ExpectedHarvestDate,
                cancellationToken);
    }

    public Task<bool> HasInProgressCycleAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        Guid? excludedCropCycleId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    cropCycle.Status ==
                        CropCycleStatus.InProgress &&
                    !cropCycle.IsDeleted &&
                    (
                        excludedCropCycleId == null ||
                        cropCycle.Id !=
                            excludedCropCycleId.Value
                    ),
                cancellationToken);
    }

    public Task<bool> HasActiveCycleForLandAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    (
                        cropCycle.Status ==
                            CropCycleStatus.Planned ||
                        cropCycle.Status ==
                            CropCycleStatus.InProgress
                    ) &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasActiveCycleForPlotAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    (
                        cropCycle.Status ==
                            CropCycleStatus.Planned ||
                        cropCycle.Status ==
                            CropCycleStatus.InProgress
                    ) &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasAnyCycleForPlotAsync(
        Guid organizationId,
        Guid landId,
        Guid landPlotId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CropCycles
            .AsNoTracking()
            .AnyAsync(
                cropCycle =>
                    cropCycle.OrganizationId ==
                        organizationId &&
                    cropCycle.LandId == landId &&
                    cropCycle.LandPlotId ==
                        landPlotId &&
                    !cropCycle.IsDeleted,
                cancellationToken);
    }

    public void Add(CropCycle cropCycle)
    {
        ArgumentNullException.ThrowIfNull(cropCycle);

        _dbContext.CropCycles.Add(cropCycle);
    }
}
