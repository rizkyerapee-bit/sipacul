using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class CapitalContributionRepository :
    ICapitalContributionRepository
{
    private readonly SiPaculDbContext _dbContext;

    public CapitalContributionRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CapitalContribution>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CapitalContributionStatus? status = null,
            CapitalContributorRole? contributorRole = null,
            DateOnly? contributionDateFrom = null,
            DateOnly? contributionDateTo = null,
            string? contributorCode = null,
            string? contributorName = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CapitalContribution> query =
            _dbContext.CapitalContributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    !contribution.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(contribution =>
                contribution.Status == status.Value);
        }

        if (contributorRole.HasValue)
        {
            query = query.Where(contribution =>
                contribution.ContributorRole ==
                    contributorRole.Value);
        }

        if (contributionDateFrom.HasValue)
        {
            query = query.Where(contribution =>
                contribution.ContributionDate >=
                    contributionDateFrom.Value);
        }

        if (contributionDateTo.HasValue)
        {
            query = query.Where(contribution =>
                contribution.ContributionDate <=
                    contributionDateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                contributorCode))
        {
            query = query.Where(contribution =>
                contribution.ContributorCode ==
                    contributorCode);
        }

        if (!string.IsNullOrWhiteSpace(
                contributorName))
        {
            var pattern =
                $"%{contributorName.Trim()}%";

            query = query.Where(contribution =>
                EF.Functions.ILike(
                    contribution.ContributorName,
                    pattern));
        }

        return await query
            .OrderBy(contribution =>
                contribution.ContributionDate)
            .ThenBy(contribution =>
                contribution.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<CapitalContribution?> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CapitalContributions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Id ==
                        contributionId &&
                    !contribution.IsDeleted,
                cancellationToken);
    }

    public Task<CapitalContribution?>
        GetByIdForUpdateAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default)
    {
        return _dbContext.CapitalContributions
            .SingleOrDefaultAsync(
                contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Id ==
                        contributionId &&
                    !contribution.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        Guid cropCycleId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CapitalContributions
            .AsNoTracking()
            .AnyAsync(
                contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.CropCycleId ==
                        cropCycleId &&
                    contribution.Code == code &&
                    !contribution.IsDeleted,
                cancellationToken);
    }

    public Task<CapitalContribution?>
        GetContributorIdentityAsync(
            Guid organizationId,
            CapitalContributorRole contributorRole,
            string contributorCode,
            Guid? excludedContributionId = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CapitalContribution> query =
            _dbContext.CapitalContributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.ContributorRole ==
                        contributorRole &&
                    contribution.ContributorCode ==
                        contributorCode &&
                    !contribution.IsDeleted);

        if (excludedContributionId.HasValue)
        {
            query = query.Where(contribution =>
                contribution.Id !=
                    excludedContributionId.Value);
        }

        return query
            .OrderBy(contribution =>
                contribution.CreatedAt)
            .ThenBy(contribution =>
                contribution.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<CapitalContribution?>
        GetPartnerIdentityAsync(
            Guid organizationId,
            Guid? excludedContributionId = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<CapitalContribution> query =
            _dbContext.CapitalContributions
                .AsNoTracking()
                .Where(contribution =>
                    contribution.OrganizationId ==
                        organizationId &&
                    contribution.ContributorRole ==
                        CapitalContributorRole.Partner &&
                    !contribution.IsDeleted);

        if (excludedContributionId.HasValue)
        {
            query = query.Where(contribution =>
                contribution.Id !=
                    excludedContributionId.Value);
        }

        return query
            .OrderBy(contribution =>
                contribution.CreatedAt)
            .ThenBy(contribution =>
                contribution.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(CapitalContribution contribution)
    {
        ArgumentNullException.ThrowIfNull(contribution);

        _dbContext.CapitalContributions.Add(
            contribution);
    }
}
