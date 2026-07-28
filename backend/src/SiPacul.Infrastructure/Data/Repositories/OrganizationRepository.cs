using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class OrganizationRepository :
    IOrganizationRepository
{
    private readonly SiPaculDbContext _dbContext;

    public OrganizationRepository(
        SiPaculDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Organization>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .Where(organization =>
                !organization.IsDeleted)
            .OrderBy(organization =>
                organization.Name)
            .ThenBy(organization =>
                organization.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Organization?> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .AsNoTracking()
            .SingleOrDefaultAsync(
                organization =>
                    organization.Id == organizationId &&
                    !organization.IsDeleted,
                cancellationToken);
    }

    public Task<Organization?> GetByIdForUpdateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .SingleOrDefaultAsync(
                organization =>
                    organization.Id == organizationId &&
                    !organization.IsDeleted,
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(
                organization =>
                    organization.Code == code &&
                    !organization.IsDeleted,
                cancellationToken);
    }

    public void Add(Organization organization)
    {
        ArgumentNullException.ThrowIfNull(organization);

        _dbContext.Organizations.Add(organization);
    }
}
