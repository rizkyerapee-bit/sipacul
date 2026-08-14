using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Infrastructure.Data.Repositories;

public sealed class ProfitSharingSchemeRepository :
    IProfitSharingSchemeRepository
{
    private readonly SiPaculDbContext _dbContext;

    public ProfitSharingSchemeRepository(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ProfitSharingScheme>>
        GetAllAsync(
            Guid organizationId,
            ProfitSharingSchemeStatus? status = null,
            string? code = null,
            CancellationToken cancellationToken = default)
    {
        IQueryable<ProfitSharingScheme> query =
            IncludeDefinition(
                _dbContext.ProfitSharingSchemes
                    .AsNoTracking())
                .Where(scheme =>
                    scheme.OrganizationId == organizationId &&
                    !scheme.IsDeleted);

        if (status.HasValue)
        {
            query = query.Where(scheme =>
                scheme.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(code))
        {
            var normalizedCode =
                code.Trim().ToUpperInvariant();

            query = query.Where(scheme =>
                scheme.Code == normalizedCode);
        }

        return await query
            .OrderBy(scheme => scheme.Code)
            .ThenByDescending(scheme => scheme.Version)
            .ToListAsync(cancellationToken);
    }

    public Task<ProfitSharingScheme?> GetByIdAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default)
    {
        return IncludeDefinition(
                _dbContext.ProfitSharingSchemes
                    .AsNoTracking())
            .SingleOrDefaultAsync(
                scheme =>
                    scheme.OrganizationId == organizationId &&
                    scheme.Id == schemeId &&
                    !scheme.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingScheme?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default)
    {
        return IncludeDefinition(
                _dbContext.ProfitSharingSchemes)
            .SingleOrDefaultAsync(
                scheme =>
                    scheme.OrganizationId == organizationId &&
                    scheme.Id == schemeId &&
                    !scheme.IsDeleted,
                cancellationToken);
    }

    public Task<ProfitSharingScheme?> GetActiveForUpdateAsync(
        Guid organizationId,
        Guid schemeFamilyId,
        Guid? excludedSchemeId = null,
        CancellationToken cancellationToken = default)
    {
        return IncludeDefinition(
                _dbContext.ProfitSharingSchemes)
            .SingleOrDefaultAsync(
                scheme =>
                    scheme.OrganizationId == organizationId &&
                    scheme.SchemeFamilyId == schemeFamilyId &&
                    scheme.Status ==
                        ProfitSharingSchemeStatus.Active &&
                    !scheme.IsDeleted &&
                    (!excludedSchemeId.HasValue ||
                     scheme.Id != excludedSchemeId.Value),
                cancellationToken);
    }

    public Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSchemes
            .AsNoTracking()
            .AnyAsync(
                scheme =>
                    scheme.OrganizationId == organizationId &&
                    scheme.Code == code &&
                    !scheme.IsDeleted,
                cancellationToken);
    }

    public Task<bool> HasDraftAsync(
        Guid organizationId,
        Guid schemeFamilyId,
        Guid? excludedSchemeId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ProfitSharingSchemes
            .AsNoTracking()
            .AnyAsync(
                scheme =>
                    scheme.OrganizationId == organizationId &&
                    scheme.SchemeFamilyId == schemeFamilyId &&
                    scheme.Status ==
                        ProfitSharingSchemeStatus.Draft &&
                    !scheme.IsDeleted &&
                    (!excludedSchemeId.HasValue ||
                     scheme.Id != excludedSchemeId.Value),
                cancellationToken);
    }

    public void Add(ProfitSharingScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        _dbContext.ProfitSharingSchemes.Add(scheme);
    }

    private static IQueryable<ProfitSharingScheme>
        IncludeDefinition(
            IQueryable<ProfitSharingScheme> query)
    {
        return query
            .AsSplitQuery()
            .Include(scheme =>
                scheme.Participants.OrderBy(participant =>
                    participant.Sequence))
            .Include(scheme =>
                scheme.PriorityRules.OrderBy(rule =>
                    rule.Sequence))
            .Include(scheme =>
                scheme.ResidualShares.OrderBy(share =>
                    share.Sequence));
    }
}
