namespace SiPacul.Application.Organizations.Contracts;

public sealed record CreateOrganizationRequest(
    string Code,
    string Name,
    string? LegalName,
    string? TimeZone);
