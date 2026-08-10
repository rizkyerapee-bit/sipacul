using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Harvests.Persistence;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class HarvestBatchRepository :
    IHarvestBatchRepository
{
    private readonly SiPaculDbContext _dbContext;

    public HarvestBatchRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<HarvestBatch>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            HarvestBatchStatus? status = null,
            DateOnly? harvestDateFrom = null,
            DateOnly? harvestDateTo = null,
            HarvestQuantityUnit? quantityUnit = null,
            string? qualityGrade = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<HarvestBatch> query =
            _dbContext.HarvestBatches
                .AsNoTracking()
                .Where(batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    !batch.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(batch =>
                batch.Status == status.Value);
        }

        if (harvestDateFrom.HasValue)
        {
            query = query.Where(batch =>
                batch.HarvestDate >=
                    harvestDateFrom.Value);
        }

        if (harvestDateTo.HasValue)
        {
            query = query.Where(batch =>
                batch.HarvestDate <=
                    harvestDateTo.Value);
        }

        if (quantityUnit.HasValue)
        {
            query = query.Where(batch =>
                batch.QuantityUnit ==
                    quantityUnit.Value);
        }

        if (!string.IsNullOrWhiteSpace(qualityGrade))
        {
            query = query.Where(batch =>
                batch.QualityGrade != null &&
                EF.Functions.ILike(
                    batch.QualityGrade,
                    $"%{qualityGrade}%"));
        }

        return await query
            .OrderBy(batch => batch.HarvestDate)
            .ThenBy(batch => batch.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<HarvestBatch?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Id == harvestBatchId &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public Task<HarvestBatch?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .SingleOrDefaultAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Id == harvestBatchId &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .AsNoTracking()
            .AnyAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Code == code &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasDraftBatchesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .AsNoTracking()
            .AnyAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Status ==
                        HarvestBatchStatus.Draft &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasNonCancelledBatchesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .AsNoTracking()
            .AnyAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Status !=
                        HarvestBatchStatus.Cancelled &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public Task<bool>
        HasNonCancelledBatchWithDifferentUnitAsync(
            Guid organizationId,
            Guid cropCycleId,
            HarvestQuantityUnit quantityUnit,
            Guid? excludedHarvestBatchId = null,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.HarvestBatches
            .AsNoTracking()
            .AnyAsync(
                batch =>
                    batch.OrganizationId ==
                        organizationId &&
                    batch.CropCycleId ==
                        cropCycleId &&
                    batch.Status !=
                        HarvestBatchStatus.Cancelled &&
                    batch.QuantityUnit != quantityUnit &&
                    (!excludedHarvestBatchId.HasValue ||
                        batch.Id !=
                            excludedHarvestBatchId.Value) &&
                    !batch.IsDeleted,
                cancellationToken);
    }

    public void Add(HarvestBatch harvestBatch)
    {
        ArgumentNullException.ThrowIfNull(
            harvestBatch);

        _dbContext.HarvestBatches.Add(
            harvestBatch);
    }
}
