using Microsoft.AspNetCore.Authorization;

namespace SiPacul.Api.Security.Authorization;

public sealed record OrganizationPermissionRequirement(
    string Permission) : IAuthorizationRequirement;
