using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.CommodityCategories;

public static class CommodityCategoryErrors
{
    public const string ValidationCode =
        "CommodityCategories.Validation";

    public const string OrganizationNotFoundCode =
        "CommodityCategories.OrganizationNotFound";

    public const string NotFoundCode =
        "CommodityCategories.NotFound";

    public const string NameAlreadyExistsCode =
        "CommodityCategories.NameAlreadyExists";

    public static Error Validation(
        string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error OrganizationNotFound(
        Guid organizationId)
    {
        return Error.NotFound(
            OrganizationNotFoundCode,
            $"Organization '{organizationId}' was not found.");
    }

    public static Error NotFound(
        Guid organizationId,
        Guid categoryId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Commodity category '{categoryId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error NameAlreadyExists(
        string name)
    {
        return Error.Conflict(
            NameAlreadyExistsCode,
            $"Commodity category name '{name}' already exists " +
            "in this organization.");
    }
}
