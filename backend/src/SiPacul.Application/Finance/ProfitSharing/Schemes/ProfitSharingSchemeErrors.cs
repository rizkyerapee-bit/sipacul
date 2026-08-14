using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes;

public static class ProfitSharingSchemeErrors
{
    public const string ValidationCode =
        "ProfitSharingSchemes.Validation";

    public const string OrganizationNotFoundCode =
        "ProfitSharingSchemes.OrganizationNotFound";

    public const string NotFoundCode =
        "ProfitSharingSchemes.NotFound";

    public const string CodeAlreadyExistsCode =
        "ProfitSharingSchemes.CodeAlreadyExists";

    public const string DraftAlreadyExistsCode =
        "ProfitSharingSchemes.DraftAlreadyExists";

    public const string InvalidStatusTransitionCode =
        "ProfitSharingSchemes.InvalidStatusTransition";

    public const string ConcurrencyConflictCode =
        "ProfitSharingSchemes.ConcurrencyConflict";

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

    public static Error NotFound(Guid schemeId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Profit sharing scheme '{schemeId}' was not found " +
            "in this organization.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Profit sharing scheme code '{code}' already exists " +
            "in this organization.");
    }

    public static Error DraftAlreadyExists(Guid schemeFamilyId)
    {
        return Error.Conflict(
            DraftAlreadyExistsCode,
            $"Scheme family '{schemeFamilyId}' already has a " +
            "draft version.");
    }

    public static Error InvalidStatusTransition(string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error ConcurrencyConflict(string message)
    {
        return Error.Conflict(
            ConcurrencyConflictCode,
            message);
    }
}
