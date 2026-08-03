using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Organizations.Members.Contracts;

public sealed record CreateOrganizationMemberRequest(
    string Email,
    string? InitialPassword,
    OrganizationRole Role)
{
    public const int MaxInitialPasswordLength = 1024;
}
