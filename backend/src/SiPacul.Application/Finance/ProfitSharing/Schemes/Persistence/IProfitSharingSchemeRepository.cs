using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;

public interface IProfitSharingSchemeRepository
{
    Task<IReadOnlyList<ProfitSharingScheme>> GetAllAsync(
        Guid organizationId,
        ProfitSharingSchemeStatus? status = null,
        string? code = null,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingScheme?> GetByIdAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingScheme?> GetByIdForUpdateAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default);

    Task<ProfitSharingScheme?> GetActiveForUpdateAsync(
        Guid organizationId,
        Guid schemeFamilyId,
        Guid? excludedSchemeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(
        Guid organizationId,
        string code,
        CancellationToken cancellationToken = default);

    Task<bool> HasDraftAsync(
        Guid organizationId,
        Guid schemeFamilyId,
        Guid? excludedSchemeId = null,
        CancellationToken cancellationToken = default);

    void Add(ProfitSharingScheme scheme);
}
