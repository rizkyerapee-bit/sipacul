using Microsoft.EntityFrameworkCore;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Security.Authorization.Services;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Infrastructure.Data;

namespace SiPacul.Infrastructure.Identity;

public sealed class OrganizationPermissionService :
    IOrganizationPermissionService
{
    private readonly SiPaculDbContext _dbContext;

    public OrganizationPermissionService(
        SiPaculDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        _dbContext = dbContext;
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string? permission,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty ||
            organizationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        var normalizedPermission =
            permission.Trim();

        if (!Permissions.All.Contains(
                normalizedPermission,
                StringComparer.Ordinal))
        {
            return false;
        }

        var role =
            await _dbContext.OrganizationMemberships
                .AsNoTracking()
                .Where(membership =>
                    membership.UserId == userId &&
                    membership.OrganizationId ==
                        organizationId &&
                    membership.Status ==
                        OrganizationMembershipStatus.Active &&
                    !membership.IsDeleted)
                .Select(membership =>
                    (OrganizationRole?)membership.Role)
                .SingleOrDefaultAsync(cancellationToken);

        return role.HasValue &&
            RolePermissionCatalog.HasPermission(
                role.Value,
                normalizedPermission);
    }
}
