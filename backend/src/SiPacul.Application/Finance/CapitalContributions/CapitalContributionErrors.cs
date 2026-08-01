using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.CapitalContributions;

public static class CapitalContributionErrors
{
    public const string ValidationCode =
        "CapitalContributions.Validation";

    public const string OrganizationNotFoundCode =
        "CapitalContributions.OrganizationNotFound";

    public const string CropCycleNotFoundCode =
        "CapitalContributions.CropCycleNotFound";

    public const string NotFoundCode =
        "CapitalContributions.NotFound";

    public const string CodeAlreadyExistsCode =
        "CapitalContributions.CodeAlreadyExists";

    public const string InvalidStatusTransitionCode =
        "CapitalContributions.InvalidStatusTransition";

    public const string DateOutOfRangeCode =
        "CapitalContributions.DateOutOfRange";

    public const string ContributorIdentityConflictCode =
        "CapitalContributions.ContributorIdentityConflict";

    public const string FinalizedSettlementExistsCode =
        "CapitalContributions.FinalizedSettlementExists";

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

    public static Error NotFound(Guid contributionId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Capital contribution '{contributionId}' was not " +
            "found in this crop cycle.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Capital contribution code '{code}' already " +
            "exists in this crop cycle.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error DateOutOfRange(
        DateOnly contributionDate,
        DateOnly earliestDate,
        DateOnly latestDate)
    {
        return Error.Validation(
            DateOutOfRangeCode,
            $"Contribution date " +
            $"'{contributionDate:yyyy-MM-dd}' must be between " +
            $"'{earliestDate:yyyy-MM-dd}' and " +
            $"'{latestDate:yyyy-MM-dd}'.");
    }

    public static Error ContributorIdentityConflict(
        CapitalContributorRole contributorRole,
        string contributorCode,
        string contributorName)
    {
        return Error.Conflict(
            ContributorIdentityConflictCode,
            $"Contributor identity '{contributorRole}:" +
            $"{contributorCode}' conflicts with the " +
            $"registered name '{contributorName}' in this " +
            "organization.");
    }

    public static Error FinalizedSettlementExists(
        Guid cropCycleId)
    {
        return Error.Conflict(
            FinalizedSettlementExistsCode,
            $"Crop cycle '{cropCycleId}' already has an active " +
            "finalized settlement. Its capital contributions " +
            "are locked.");
    }
}
