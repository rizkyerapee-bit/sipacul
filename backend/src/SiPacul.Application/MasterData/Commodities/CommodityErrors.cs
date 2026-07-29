using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.Commodities;

public static class CommodityErrors
{
    public const string ValidationCode =
        "Commodities.Validation";

    public const string OrganizationNotFoundCode =
        "Commodities.OrganizationNotFound";

    public const string CategoryNotFoundCode =
        "Commodities.CategoryNotFound";

    public const string NotFoundCode =
        "Commodities.NotFound";

    public const string CodeAlreadyExistsCode =
        "Commodities.CodeAlreadyExists";

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

    public static Error CategoryNotFound(
        Guid organizationId,
        Guid categoryId)
    {
        return Error.NotFound(
            CategoryNotFoundCode,
            $"Commodity category '{categoryId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error NotFound(
        Guid organizationId,
        Guid commodityId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Commodity '{commodityId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error CodeAlreadyExists(
        string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Commodity code '{code}' already exists " +
            "in this organization.");
    }
}
