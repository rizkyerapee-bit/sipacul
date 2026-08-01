using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class SaleRepository : ISaleRepository
{
    private readonly SiPaculDbContext _dbContext;

    public SaleRepository(SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Sale>> GetAllAsync(
        Guid organizationId,
        SaleStatus? status = null,
        DateOnly? saleDateFrom = null,
        DateOnly? saleDateTo = null,
        SalePaymentTerm? paymentTerm = null,
        string? buyerName = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Sale> query = _dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .AsSplitQuery()
            .Where(sale =>
                sale.OrganizationId == organizationId &&
                !sale.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(sale =>
                sale.Status == status.Value);
        }

        if (saleDateFrom.HasValue)
        {
            query = query.Where(sale =>
                sale.SaleDate >= saleDateFrom.Value);
        }

        if (saleDateTo.HasValue)
        {
            query = query.Where(sale =>
                sale.SaleDate <= saleDateTo.Value);
        }

        if (paymentTerm.HasValue)
        {
            query = query.Where(sale =>
                sale.PaymentTerm == paymentTerm.Value);
        }

        if (!string.IsNullOrWhiteSpace(buyerName))
        {
            query = query.Where(sale =>
                EF.Functions.ILike(
                    sale.BuyerName,
                    $"%{buyerName}%"));
        }

        return await query
            .OrderByDescending(sale => sale.SaleDate)
            .ThenBy(sale => sale.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Sale?> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sales
            .AsNoTracking()
            .Include(sale => sale.Lines)
            .AsSplitQuery()
            .SingleOrDefaultAsync(
                sale =>
                    sale.OrganizationId == organizationId &&
                    sale.Id == saleId &&
                    !sale.IsDeleted,
                cancellationToken);
    }

    public Task<Sale?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sales
            .Include(sale => sale.Lines)
            .SingleOrDefaultAsync(
                sale =>
                    sale.OrganizationId == organizationId &&
                    sale.Id == saleId &&
                    !sale.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Sales
            .AsNoTracking()
            .AnyAsync(
                sale =>
                    sale.OrganizationId == organizationId &&
                    sale.Code == code &&
                    !sale.IsDeleted,
                cancellationToken);
    }

    public async Task<SaleHarvestReference?>
        GetHarvestReferenceAsync(
            Guid organizationId,
            Guid harvestBatchId,
            CancellationToken cancellationToken = default)
    {
        var harvestBatch = await _dbContext.HarvestBatches
            .AsNoTracking()
            .SingleOrDefaultAsync(
                batch =>
                    batch.OrganizationId == organizationId &&
                    batch.Id == harvestBatchId &&
                    !batch.IsDeleted,
                cancellationToken);

        if (harvestBatch is null)
        {
            return null;
        }

        var cropCycle = await _dbContext.CropCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                cycle =>
                    cycle.OrganizationId == organizationId &&
                    cycle.Id == harvestBatch.CropCycleId &&
                    !cycle.IsDeleted,
                cancellationToken);

        if (cropCycle is null)
        {
            return null;
        }

        var commodity = await _dbContext.Commodities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item =>
                    item.OrganizationId == organizationId &&
                    item.Id == cropCycle.CommodityId &&
                    !item.IsDeleted,
                cancellationToken);

        if (commodity is null)
        {
            return null;
        }

        return new SaleHarvestReference(
            harvestBatch.Id,
            harvestBatch.Code,
            cropCycle.Id,
            cropCycle.Code,
            commodity.Id,
            commodity.Code.Value,
            commodity.Name,
            harvestBatch.QualityGrade,
            harvestBatch.Status,
            harvestBatch.NetQuantity,
            harvestBatch.QuantityUnit);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>>
        GetConfirmedSoldQuantitiesAsync(
            Guid organizationId,
            IReadOnlyCollection<Guid> harvestBatchIds,
            CancellationToken cancellationToken = default)
    {
        var identifiers = harvestBatchIds
            .Distinct()
            .ToArray();

        if (identifiers.Length == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var quantities = await (
            from line in _dbContext.SaleLines.AsNoTracking()
            join sale in _dbContext.Sales.AsNoTracking()
                on new
                {
                    line.OrganizationId,
                    line.SaleId
                }
                equals new
                {
                    sale.OrganizationId,
                    SaleId = sale.Id
                }
            where
                line.OrganizationId == organizationId &&
                identifiers.Contains(line.HarvestBatchId) &&
                sale.Status == SaleStatus.Confirmed &&
                !sale.IsDeleted
            group line by line.HarvestBatchId
            into groupedLines
            select new
            {
                HarvestBatchId = groupedLines.Key,
                Quantity = groupedLines.Sum(
                    line => line.Quantity)
            })
            .ToListAsync(cancellationToken);

        return quantities.ToDictionary(
            item => item.HarvestBatchId,
            item => Math.Round(
                item.Quantity,
                4,
                MidpointRounding.AwayFromZero));
    }

    public async Task<decimal>
        GetConfirmedSoldQuantityAsync(
            Guid organizationId,
            Guid harvestBatchId,
            CancellationToken cancellationToken = default)
    {
        var quantities =
            await GetConfirmedSoldQuantitiesAsync(
                organizationId,
                new[] { harvestBatchId },
                cancellationToken);

        return quantities.TryGetValue(
            harvestBatchId,
            out var quantity)
                ? quantity
                : 0;
    }

    public Task<bool> HasActiveConfirmedSaleForHarvestAsync(
        Guid organizationId,
        Guid harvestBatchId,
        CancellationToken cancellationToken = default)
    {
        return (
            from line in _dbContext.SaleLines.AsNoTracking()
            join sale in _dbContext.Sales.AsNoTracking()
                on new
                {
                    line.OrganizationId,
                    line.SaleId
                }
                equals new
                {
                    sale.OrganizationId,
                    SaleId = sale.Id
                }
            where
                line.OrganizationId == organizationId &&
                line.HarvestBatchId == harvestBatchId &&
                sale.Status == SaleStatus.Confirmed &&
                !sale.IsDeleted
            select line.Id)
            .AnyAsync(cancellationToken);
    }

    public void Add(Sale sale)
    {
        ArgumentNullException.ThrowIfNull(sale);

        _dbContext.Sales.Add(sale);
    }
}
