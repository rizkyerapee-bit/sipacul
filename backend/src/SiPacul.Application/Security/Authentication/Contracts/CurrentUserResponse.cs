namespace SiPacul.Application.Security.Authentication.Contracts;

public sealed record CurrentUserResponse(
    Guid UserId,
    string Email,
    bool EmailConfirmed,
    DateTime? LastLoginAt,
    IReadOnlyList<CurrentUserMembershipResponse> Memberships);
