using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingSchemeAssignmentRepository :
    IProfitSharingSchemeAssignmentRepository
{
    private readonly SiPaculDbContext _dbContext;

    public ProfitSharingSchemeAssignmentRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public Task<ProfitSharingSchemeAssignment?>
        GetByCropCycleAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        return IncludeSnapshot(
                _dbContext.ProfitSharingSchemeAssignments
                    .AsNoTracking())
            .SingleOrDefaultAsync(
                assignment =>
                    assignment.OrganizationId == organizationId &&
                    assignment.CropCycleId == cropCycleId &&
                    !assignment.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingSchemeAssignment?>
        GetByCropCycleForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        return IncludeSnapshot(
                _dbContext.ProfitSharingSchemeAssignments)
            .SingleOrDefaultAsync(
                assignment =>
                    assignment.OrganizationId == organizationId &&
                    assignment.CropCycleId == cropCycleId &&
                    !assignment.IsDeleted,
                cancellationToken);
    }

    public void Add(ProfitSharingSchemeAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        _dbContext.ProfitSharingSchemeAssignments.Add(assignment);
    }

    private static IQueryable<ProfitSharingSchemeAssignment>
        IncludeSnapshot(
            IQueryable<ProfitSharingSchemeAssignment> query)
    {
        return query
            .AsSplitQuery()
            .Include(assignment =>
                assignment.Participants.OrderBy(participant =>
                    participant.Sequence))
            .Include(assignment =>
                assignment.PriorityRules.OrderBy(rule =>
                    rule.Sequence))
            .Include(assignment =>
                assignment.ResidualShares.OrderBy(share =>
                    share.Sequence));
    }
}
