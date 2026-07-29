using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Domain.Entities.MasterData;

namespace SiPacul.Application.MasterData.CommodityCategories.Mappings;

internal static class CommodityCategoryMappings
{
    public static CommodityCategoryResponse ToResponse(
        this CommodityCategory category)
    {
        return new CommodityCategoryResponse(
            category.Id,
            category.OrganizationId,
            category.Name,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
