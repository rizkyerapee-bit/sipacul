namespace SiPacul.Application.Organizations.Contracts;

public sealed record OrganizationResponse(
    Guid Id,
    string Code,
    string Name,
    string? LegalName,
    string TimeZone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
