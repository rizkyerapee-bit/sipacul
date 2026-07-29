using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Application.MasterData.Commodities.Mappings;

internal static class CommodityMappings
{
    public static CommodityResponse ToResponse(
        this Commodity commodity)
    {
        return new CommodityResponse(
            commodity.Id,
            commodity.OrganizationId,
            commodity.Code.Value,
            commodity.Name,
            commodity.CommodityCategoryId,
            commodity.ScientificName,
            commodity.Description,
            commodity.IsActive,
            commodity.CreatedAt,
            commodity.UpdatedAt);
    }
}
