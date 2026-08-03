using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Organizations.Members.Contracts;

public sealed record OrganizationMemberResponse(
    Guid MembershipId,
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    bool UserIsActive,
    OrganizationRole Role,
    OrganizationMembershipStatus Status,
    DateTime JoinedAt,
    DateTime? SuspendedAt);
