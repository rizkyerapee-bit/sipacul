using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingWaterfallSettlementRepository :
    IProfitSharingWaterfallSettlementRepository
{
    private readonly SiPaculDbContext _dbContext;

    public ProfitSharingWaterfallSettlementRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<IReadOnlyList<ProfitSharingWaterfallSettlement>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            ProfitSharingWaterfallSettlementStatus? status = null,
            DateOnly? settlementDateFrom = null,
            DateOnly? settlementDateTo = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<ProfitSharingWaterfallSettlement> query =
            IncludeSnapshot(
                    _dbContext.ProfitSharingWaterfallSettlements
                        .AsNoTracking())
                .Where(settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    !settlement.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(settlement =>
                settlement.Status == status.Value);
        }

        if (settlementDateFrom.HasValue)
        {
            query = query.Where(settlement =>
                settlement.SettlementDate >= settlementDateFrom.Value);
        }

        if (settlementDateTo.HasValue)
        {
            query = query.Where(settlement =>
                settlement.SettlementDate <= settlementDateTo.Value);
        }

        return LoadListAsync(query, cancellationToken);
    }

    public Task<ProfitSharingWaterfallSettlement?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return IncludeSnapshot(
                _dbContext.ProfitSharingWaterfallSettlements
                    .AsNoTracking())
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Id == settlementId &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingWaterfallSettlement?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        CancellationToken cancellationToken = default)
    {
        return IncludeSnapshot(
                _dbContext.ProfitSharingWaterfallSettlements)
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Id == settlementId &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingWaterfallSettlement?> GetActiveFinalizedAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return IncludeSnapshot(
                _dbContext.ProfitSharingWaterfallSettlements
                    .AsNoTracking())
            .SingleOrDefaultAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Status ==
                        ProfitSharingWaterfallSettlementStatus.Finalized &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingWaterfallSettlements
            .AsNoTracking()
            .AnyAsync(
                settlement =>
                    settlement.OrganizationId == organizationId &&
                    settlement.CropCycleId == cropCycleId &&
                    settlement.Code == code &&
                    !settlement.IsDeleted,
                cancellationToken);
    }

    public void Add(ProfitSharingWaterfallSettlement settlement)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        _dbContext.ProfitSharingWaterfallSettlements.Add(settlement);
    }

    private static IQueryable<ProfitSharingWaterfallSettlement>
        IncludeSnapshot(
            IQueryable<ProfitSharingWaterfallSettlement> query)
    {
        return query
            .AsSplitQuery()
            .Include(settlement =>
                settlement.PriorityAllocations.OrderBy(item =>
                    item.Sequence))
            .Include(settlement =>
                settlement.ParticipantAllocations.OrderBy(item =>
                    item.Sequence))
            .Include(settlement =>
                settlement.ResidualShares.OrderBy(item =>
                    item.Sequence));
    }

    private static async Task<
        IReadOnlyList<ProfitSharingWaterfallSettlement>> LoadListAsync(
            IQueryable<ProfitSharingWaterfallSettlement> query,
            CancellationToken cancellationToken)
    {
        return await query
            .OrderBy(settlement => settlement.SettlementDate)
            .ThenBy(settlement => settlement.Code)
            .ToListAsync(cancellationToken);
    }
}
