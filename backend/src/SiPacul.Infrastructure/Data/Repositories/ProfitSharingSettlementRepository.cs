using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.ProfitSharing.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingSettlementRepository :
    IProfitSharingSettlementRepository
{
    private readonly SiPaculDbContext _dbContext;

    public ProfitSharingSettlementRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<
        IReadOnlyList<ProfitSharingSettlement>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingSettlementStatus? status = null,
            DateOnly? settlementDateFrom = null,
            DateOnly? settlementDateTo = null,
            string? managingPartnerCode = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<ProfitSharingSettlement> query =
            _dbContext.ProfitSharingSettlements
                .AsNoTracking()
                .AsSplitQuery()
                .Include(settlement =>
                    settlement.Allocations.OrderBy(
                        allocation =>
                            allocation.Sequence))
                .Where(settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    !settlement.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(settlement =>
                settlement.Status == status.Value);
        }

        if (settlementDateFrom.HasValue)
        {
            query = query.Where(settlement =>
                settlement.SettlementDate >=
                    settlementDateFrom.Value);
        }

        if (settlementDateTo.HasValue)
        {
            query = query.Where(settlement =>
                settlement.SettlementDate <=
                    settlementDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                managingPartnerCode))
        {
            var normalizedCode =
                managingPartnerCode.Trim().ToUpper();

            query = query.Where(settlement =>
                settlement.ManagingPartnerCode ==
                    normalizedCode);
        }

        return await query
            .OrderBy(settlement =>
                settlement.SettlementDate)
            .ThenBy(settlement =>
                settlement.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<ProfitSharingSettlement?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsNoTracking()
            .AsSplitQuery()
            .Include(settlement =>
                settlement.Allocations.OrderBy(
                    allocation =>
                        allocation.Sequence))
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Id ==
                        settlementId &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingSettlement?>
        GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid settlementId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsSplitQuery()
            .Include(settlement =>
                settlement.Allocations.OrderBy(
                    allocation =>
                        allocation.Sequence))
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Id ==
                        settlementId &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingSettlement?>
        GetActiveFinalizedAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsNoTracking()
            .AsSplitQuery()
            .Include(settlement =>
                settlement.Allocations.OrderBy(
                    allocation =>
                        allocation.Sequence))
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingSettlement?>
        GetActiveFinalizedForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsSplitQuery()
            .Include(settlement =>
                settlement.Allocations.OrderBy(
                    allocation =>
                        allocation.Sequence))
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsNoTracking()
            .AnyAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Code == code &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasActiveFinalizedAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid? excludedSettlementId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSettlements
            .AsNoTracking()
            .AnyAsync(
                settlement =>
                    settlement.OrganizationId ==
                        organizationId &&
                    settlement.CropCycleId ==
                        cropCycleId &&
                    settlement.Status ==
                        ProfitSharingSettlementStatus.Finalized &&
                    !settlement.IsDeleted &&
                    (!excludedSettlementId.HasValue ||
                     settlement.Id !=
                        excludedSettlementId.Value),
                cancellationToken);
    }

    public void Add(
        ProfitSharingSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);

        _dbContext.ProfitSharingSettlements.Add(
            settlement);
    }
}
