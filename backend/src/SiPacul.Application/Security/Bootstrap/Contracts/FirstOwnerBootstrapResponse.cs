using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Security.Bootstrap.Contracts;

public sealed record FirstOwnerBootstrapResponse(
    Guid UserId,
    string Email,
    Guid OrganizationId,
    string OrganizationCode,
    string OrganizationName,
    Guid MembershipId,
    OrganizationRole Role,
    DateTime CreatedAt);
