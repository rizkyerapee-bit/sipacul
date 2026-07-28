using SiPacul.Shared.Results;

namespace SiPacul.Application.Organizations;

public static class OrganizationErrors
{
    public const string ValidationCode =
        "Organizations.Validation";

    public const string NotFoundCode =
        "Organizations.NotFound";

    public const string CodeAlreadyExistsCode =
        "Organizations.CodeAlreadyExists";

    public static Error Validation(
        string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error NotFound(
        Guid organizationId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Organization '{organizationId}' was not found.");
    }

    public static Error CodeAlreadyExists(
        string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Organization code '{code}' already exists.");
    }
}
