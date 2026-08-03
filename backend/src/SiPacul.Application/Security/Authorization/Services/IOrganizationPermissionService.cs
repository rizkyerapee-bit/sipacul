namespace SiPacul.Application.Security.Authorization.Services;

public interface IOrganizationPermissionService
{
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string? permission,
        CancellationToken cancellationToken = default);
}
