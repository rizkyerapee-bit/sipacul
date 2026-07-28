using SiPacul.Application.Organizations.Contracts;
using SiPacul.Domain.Entities.Organizations;

namespace SiPacul.Application.Organizations.Mappings;

internal static class OrganizationMappings
{
    public static OrganizationResponse ToResponse(
        this Organization organization)
    {
        return new OrganizationResponse(
            organization.Id,
            organization.Code,
            organization.Name,
            organization.LegalName,
            organization.TimeZone,
            organization.IsActive,
            organization.CreatedAt,
            organization.UpdatedAt);
    }
}
