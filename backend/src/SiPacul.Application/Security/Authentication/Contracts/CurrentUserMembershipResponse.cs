using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Security.Authentication.Contracts;

public sealed record CurrentUserMembershipResponse(
    Guid MembershipId,
    Guid OrganizationId,
    OrganizationRole Role,
    IReadOnlyList<string> Permissions);
