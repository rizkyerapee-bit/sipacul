using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Previews;

public static class ProfitSharingPreviewErrors
{
    public const string ValidationCode =
        "ProfitSharingPreview.Validation";

    public const string OrganizationNotFoundCode =
        "ProfitSharingPreview.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "ProfitSharingPreview.CropCycleNotFound";

    public const string AssignmentNotFoundCode =
        "ProfitSharingPreview.AssignmentNotFound";

    public const string CapitalIdentityConflictCode =
        "ProfitSharingPreview.CapitalIdentityConflict";

    public const string CapitalNotInSchemeCode =
        "ProfitSharingPreview.CapitalNotInScheme";

    public const string CapitalRoleMismatchCode =
        "ProfitSharingPreview.CapitalRoleMismatch";

    public const string SourceDataChangedCode =
        "ProfitSharingPreview.SourceDataChanged";

    public const string CalculationUnavailableCode =
        "ProfitSharingPreview.CalculationUnavailable";

    public static Error Validation(string message) =>
        Error.Validation(ValidationCode, message);

    public static Error OrganizationNotFound(Guid organizationId) =>
        Error.NotFound(
            OrganizationNotFoundCode,
            $"Organization '{organizationId}' was not found.");

    public static Error CropCycleNotFound(Guid cropCycleId) =>
        Error.NotFound(
            CropCycleNotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found in this organization.");

    public static Error AssignmentNotFound(Guid cropCycleId) =>
        Error.NotFound(
            AssignmentNotFoundCode,
            $"A profit-sharing scheme has not been assigned to crop cycle '{cropCycleId}'.");

    public static Error CapitalIdentityConflict(string contributorCode) =>
        Error.Conflict(
            CapitalIdentityConflictCode,
            $"Confirmed capital code '{contributorCode}' uses inconsistent contributor identity data.");

    public static Error CapitalNotInScheme(string contributorCode) =>
        Error.Conflict(
            CapitalNotInSchemeCode,
            $"Confirmed capital code '{contributorCode}' is not a participant in the assigned scheme.");

    public static Error CapitalRoleMismatch(string contributorCode) =>
        Error.Conflict(
            CapitalRoleMismatchCode,
            $"Confirmed capital role for '{contributorCode}' does not match the assigned participant role.");

    public static Error SourceDataChanged() =>
        Error.Conflict(
            SourceDataChangedCode,
            "Confirmed capital changed while the preview sources were being read. Please request the preview again.");

    public static Error CalculationUnavailable(string message) =>
        Error.Conflict(
            CalculationUnavailableCode,
            "The profit-sharing preview cannot be calculated. " + message);
}
