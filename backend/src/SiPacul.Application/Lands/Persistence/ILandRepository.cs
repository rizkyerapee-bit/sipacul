using SiPacul.Domain.Entities.Lands;

namespace SiPacul.Application.Lands.Persistence;

public interface ILandRepository
{
    Task<IReadOnlyList<Land>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Land?> GetByIdAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<Land?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid landId,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    void Add(Land land);
}
