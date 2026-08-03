using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Organizations.Members.Contracts;

public sealed record UpdateOrganizationMemberRoleRequest(
    OrganizationRole Role);
