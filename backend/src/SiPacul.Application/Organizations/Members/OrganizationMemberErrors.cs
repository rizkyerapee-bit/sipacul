using SiPacul.Shared.Results;

namespace SiPacul.Application.Organizations.Members;

public static class OrganizationMemberErrors
{
    public const string ValidationCode =
        "OrganizationMembers.Validation";

    public const string OrganizationNotFoundCode =
        "OrganizationMembers.OrganizationNotFound";

    public const string NotFoundCode =
        "OrganizationMembers.NotFound";

    public const string AlreadyExistsCode =
        "OrganizationMembers.AlreadyExists";

    public const string UserInactiveCode =
        "OrganizationMembers.UserInactive";

    public const string OwnerProtectedCode =
        "OrganizationMembers.OwnerProtected";

    public const string IdentityValidationCode =
        "OrganizationMembers.IdentityValidation";

    public const string DataConflictCode =
        "OrganizationMembers.DataConflict";

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

    public static Error NotFound(
        Guid organizationId,
        Guid membershipId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Membership '{membershipId}' was not found " +
            $"in organization '{organizationId}'.");
    }

    public static Error AlreadyExists(string email)
    {
        return Error.Conflict(
            AlreadyExistsCode,
            $"User '{email}' is already a member of " +
            "this organization.");
    }

    public static Error UserInactive(string email)
    {
        return Error.Conflict(
            UserInactiveCode,
            $"User account '{email}' is inactive.");
    }

    public static Error OwnerProtected()
    {
        return Error.Validation(
            OwnerProtectedCode,
            "Owner membership can only be changed through " +
            "the dedicated ownership-transfer workflow.");
    }

    public static Error IdentityValidation(string message)
    {
        return Error.Validation(
            IdentityValidationCode,
            message);
    }

    public static Error DataConflict()
    {
        return Error.Conflict(
            DataConflictCode,
            "Member data conflicts with an existing record.");
    }
}
