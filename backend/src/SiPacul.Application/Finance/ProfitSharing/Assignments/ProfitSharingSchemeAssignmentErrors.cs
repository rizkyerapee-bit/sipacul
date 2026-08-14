using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments;

public static class ProfitSharingSchemeAssignmentErrors
{
    public const string ValidationCode =
        "ProfitSharingSchemeAssignments.Validation";

    public const string OrganizationNotFoundCode =
        "ProfitSharingSchemeAssignments.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "ProfitSharingSchemeAssignments.CropCycleNotFound";

    public const string SchemeNotFoundCode =
        "ProfitSharingSchemeAssignments.SchemeNotFound";

    public const string AssignmentNotFoundCode =
        "ProfitSharingSchemeAssignments.AssignmentNotFound";

    public const string SchemeNotActiveCode =
        "ProfitSharingSchemeAssignments.SchemeNotActive";

    public const string CropCycleClosedCode =
        "ProfitSharingSchemeAssignments.CropCycleClosed";

    public const string AssignmentLockedCode =
        "ProfitSharingSchemeAssignments.AssignmentLocked";

    public static Error Validation(string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error OrganizationNotFound(Guid organizationId)
    {
        return Error.NotFound(
            OrganizationNotFoundCode,
            $"Organization '{organizationId}' was not found.");
    }

    public static Error CropCycleNotFound(Guid cropCycleId)
    {
        return Error.NotFound(
            CropCycleNotFoundCode,
            $"Crop cycle '{cropCycleId}' was not found in this " +
            "organization.");
    }

    public static Error SchemeNotFound(Guid schemeId)
    {
        return Error.NotFound(
            SchemeNotFoundCode,
            $"Profit sharing scheme '{schemeId}' was not found " +
            "in this organization.");
    }

    public static Error AssignmentNotFound(Guid cropCycleId)
    {
        return Error.NotFound(
            AssignmentNotFoundCode,
            $"Crop cycle '{cropCycleId}' does not have a profit " +
            "sharing scheme assignment.");
    }

    public static Error SchemeNotActive(Guid schemeId)
    {
        return Error.Conflict(
            SchemeNotActiveCode,
            $"Profit sharing scheme '{schemeId}' must be active " +
            "before it can be assigned.");
    }

    public static Error CropCycleClosed(Guid cropCycleId)
    {
        return Error.Conflict(
            CropCycleClosedCode,
            $"Crop cycle '{cropCycleId}' is completed or " +
            "cancelled and cannot receive a scheme assignment.");
    }

    public static Error AssignmentLocked(Guid cropCycleId)
    {
        return Error.Conflict(
            AssignmentLockedCode,
            $"Profit sharing scheme assignment for crop cycle " +
            $"'{cropCycleId}' cannot be replaced after the cycle " +
            "has started.");
    }
}
