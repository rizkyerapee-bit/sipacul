using SiPacul.Application.Organizations.Members.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Organizations.Members.Services;

public interface IOrganizationMemberService
{
    Task<Result<IReadOnlyList<OrganizationMemberResponse>>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default);

    Task<Result<OrganizationMemberResponse>> GetByIdAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationMemberResponse>> CreateAsync(
        Guid organizationId,
        CreateOrganizationMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationMemberResponse>> ChangeRoleAsync(
        Guid organizationId,
        Guid membershipId,
        UpdateOrganizationMemberRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationMemberResponse>> ActivateAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default);

    Task<Result<OrganizationMemberResponse>> SuspendAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default);
}
