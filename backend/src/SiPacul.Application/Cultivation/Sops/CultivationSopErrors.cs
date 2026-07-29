using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Sops;

public static class CultivationSopErrors
{
    public const string ValidationCode =
        "CultivationSops.Validation";

    public const string OrganizationNotFoundCode =
        "CultivationSops.OrganizationNotFound";

    public const string CommodityNotFoundCode =
        "CultivationSops.CommodityNotFound";

    public const string NotFoundCode =
        "CultivationSops.NotFound";

    public const string NameAlreadyExistsCode =
        "CultivationSops.NameAlreadyExists";

    public const string StepNotFoundCode =
        "CultivationSops.StepNotFound";

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

    public static Error CommodityNotFound(
        Guid organizationId,
        Guid commodityId)
    {
        return Error.NotFound(
            CommodityNotFoundCode,
            $"Commodity '{commodityId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error NotFound(
        Guid organizationId,
        Guid cultivationSopId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Cultivation SOP '{cultivationSopId}' " +
            $"was not found in organization " +
            $"'{organizationId}'.");
    }

    public static Error NameAlreadyExists(
        Guid commodityId,
        string name)
    {
        return Error.Conflict(
            NameAlreadyExistsCode,
            $"Cultivation SOP name '{name}' already exists " +
            $"for commodity '{commodityId}'.");
    }

    public static Error StepNotFound(
        Guid cultivationSopId,
        Guid stepId)
    {
        return Error.NotFound(
            StepNotFoundCode,
            $"Cultivation SOP step '{stepId}' was not found " +
            $"in SOP '{cultivationSopId}'.");
    }
}
