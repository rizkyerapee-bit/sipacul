using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Cultivation.Activities.Persistence;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CultivationActivityRepository :
    ICultivationActivityRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CultivationActivityRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CultivationActivity>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationActivityStatus? status = null,
            CultivationActivityType? activityType = null,
            DateOnly? plannedFrom = null,
            DateOnly? plannedTo = null,
            Guid? cultivationSopStepId = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CultivationActivity> query =
            _dbContext.CultivationActivities
                .AsNoTracking()
                .Include(activity => activity.Resources)
                .Where(activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    !activity.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(activity =>
                activity.Status == status.Value);
        }

        if (activityType.HasValue)
        {
            query = query.Where(activity =>
                activity.ActivityType ==
                    activityType.Value);
        }

        if (plannedFrom.HasValue)
        {
            query = query.Where(activity =>
                activity.PlannedDate >=
                    plannedFrom.Value);
        }

        if (plannedTo.HasValue)
        {
            query = query.Where(activity =>
                activity.PlannedDate <=
                    plannedTo.Value);
        }

        if (cultivationSopStepId.HasValue)
        {
            query = query.Where(activity =>
                activity.CultivationSopStepId ==
                    cultivationSopStepId.Value);
        }

        return await query
            .OrderBy(activity => activity.PlannedDate)
            .ThenBy(activity => activity.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<CultivationActivity?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationActivities
            .AsNoTracking()
            .Include(activity => activity.Resources)
            .SingleOrDefaultAsync(
                activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Id == activityId &&
                    !activity.IsDeleted,
                cancellationToken);
    }

    public Task<CultivationActivity?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationActivities
            .Include(activity => activity.Resources)
            .SingleOrDefaultAsync(
                activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Id == activityId &&
                    !activity.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationActivities
            .AsNoTracking()
            .AnyAsync(
                activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Code == code &&
                    !activity.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasInProgressActivitiesAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationActivities
            .AsNoTracking()
            .AnyAsync(
                activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CropCycleId ==
                        cropCycleId &&
                    activity.Status ==
                        CultivationActivityStatus.InProgress &&
                    !activity.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasAnyActivityForSopStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid cultivationSopStepId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CultivationActivities
            .AsNoTracking()
            .AnyAsync(
                activity =>
                    activity.OrganizationId ==
                        organizationId &&
                    activity.CultivationSopId ==
                        cultivationSopId &&
                    activity.CultivationSopStepId ==
                        cultivationSopStepId &&
                    !activity.IsDeleted,
                cancellationToken);
    }

    public void Add(CultivationActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        _dbContext.CultivationActivities.Add(activity);
    }
}
