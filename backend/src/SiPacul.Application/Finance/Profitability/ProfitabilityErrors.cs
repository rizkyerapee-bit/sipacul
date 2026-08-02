using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Profitability;

public static class ProfitabilityErrors
{
    public const string ValidationCode =
        "Profitability.Validation";

    public const string OrganizationNotFoundCode =
        "Profitability.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "Profitability.CropCycleNotFound";

    public const string SourceDataInvalidCode =
        "Profitability.SourceDataInvalid";

    public static Error Validation(string message)
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

    public static Error CropCycleNotFound(
        Guid cropCycleId)
    {
        return Error.NotFound(
            CropCycleNotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found " +
            "in this organization.");
    }

    public static Error SourceDataInvalid(string message)
    {
        return Error.Conflict(
            SourceDataInvalidCode,
            "Profitability source data is inconsistent. " +
            message);
    }
}
