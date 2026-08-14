using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Services;

public interface IProfitSharingSchemeService
{
    Task<Result<ProfitSharingSchemeResponse>> CreateDraftAsync(
        Guid organizationId,
        CreateProfitSharingSchemeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ProfitSharingSchemeResponse>>>
        GetAllAsync(
            Guid organizationId,
            ProfitSharingSchemeFilter? filter = null,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSchemeResponse>> GetByIdAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSchemeResponse>> UpdateDraftAsync(
        Guid organizationId,
        Guid schemeId,
        UpdateProfitSharingSchemeDraftRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSchemeResponse>>
        CreateNextVersionAsync(
            Guid organizationId,
            Guid sourceSchemeId,
            CancellationToken cancellationToken = default);

    Task<Result<ProfitSharingSchemeResponse>> ActivateAsync(
        Guid organizationId,
        Guid schemeId,
        CancellationToken cancellationToken = default);
}
