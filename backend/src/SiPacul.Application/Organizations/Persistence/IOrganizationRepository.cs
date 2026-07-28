using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Organizations.Persistence;

public interface IOrganizationRepository
{
    Task<IReadOnlyList<Organization>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Organization?> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Organization?> GetByIdForUpdateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        string code,
        CancellationToken cancellationToken = default);

    void Add(Organization organization);
}
