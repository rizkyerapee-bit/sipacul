using SiPacul.Application.Organizations.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Organizations.Services;

public interface IOrganizationService
{
    Task<Result<OrganizationResponse>> CreateAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OrganizationResponse>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationResponse>> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationResponse>> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationResponse>> ActivateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationResponse>> DeactivateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
