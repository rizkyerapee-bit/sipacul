namespace SiPacul.Application.Organizations.Contracts;

public sealed record UpdateOrganizationRequest(
    string Name,
    string? LegalName,
    string? TimeZone);
