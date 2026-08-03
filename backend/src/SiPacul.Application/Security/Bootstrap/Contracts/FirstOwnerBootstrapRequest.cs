namespace SiPacul.Application.Security.Bootstrap.Contracts;

public sealed record FirstOwnerBootstrapRequest(
    string? OrganizationCode,
    string? OrganizationName,
    string? OrganizationLegalName,
    string? OrganizationTimeZone,
    string? Email,
    string? Password)
{
    public const int MaxPasswordLength = 1024;
}
