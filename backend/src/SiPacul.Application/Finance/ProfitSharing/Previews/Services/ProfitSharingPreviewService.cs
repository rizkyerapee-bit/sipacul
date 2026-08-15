using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Calculations;
using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Previews.Mappings;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Previews.Services;

public sealed class ProfitSharingPreviewService :
    IProfitSharingPreviewService
{
    private readonly IProfitSharingSchemeAssignmentRepository
        _assignmentRepository;

    private readonly IProfitabilityReadRepository
        _profitabilityRepository;

    private readonly ICapitalContributionRepository
        _capitalContributionRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly TimeProvider _timeProvider;

    public ProfitSharingPreviewService(
        IProfitSharingSchemeAssignmentRepository assignmentRepository,
        IProfitabilityReadRepository profitabilityRepository,
        ICapitalContributionRepository capitalContributionRepository,
        IOrganizationRepository organizationRepository,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(assignmentRepository);
        ArgumentNullException.ThrowIfNull(profitabilityRepository);
        ArgumentNullException.ThrowIfNull(capitalContributionRepository);
        ArgumentNullException.ThrowIfNull(organizationRepository);
        ArgumentNullException.ThrowIfNull(timeProvider);

        _assignmentRepository = assignmentRepository;
        _profitabilityRepository = profitabilityRepository;
        _capitalContributionRepository = capitalContributionRepository;
        _organizationRepository = organizationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<ProfitSharingPreviewResponse>> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Failure(identifierError);
        }

        if (await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken) is null)
        {
            return Failure(
                ProfitSharingPreviewErrors.OrganizationNotFound(
                    organizationId));
        }

        var sourceSnapshot = await _profitabilityRepository.GetAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (sourceSnapshot is null)
        {
            return Failure(
                ProfitSharingPreviewErrors.CropCycleNotFound(
                    cropCycleId));
        }

        var assignment = await _assignmentRepository
            .GetByCropCycleAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (assignment is null)
        {
            return Failure(
                ProfitSharingPreviewErrors.AssignmentNotFound(
                    cropCycleId));
        }

        var contributions =
            await _capitalContributionRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                status: CapitalContributionStatus.Confirmed,
                cancellationToken: cancellationToken);

        var sourceCalculation =
            ProfitSharingWaterfallSourceCalculator.Calculate(
                assignment,
                sourceSnapshot,
                contributions,
                _timeProvider.GetUtcNow().UtcDateTime);

        if (!sourceCalculation.IsSuccess)
        {
            return Failure(MapFailure(sourceCalculation));
        }

        return Result<ProfitSharingPreviewResponse>.Success(
            sourceCalculation.Calculation!.ToPreviewResponse(
                assignment,
                sourceCalculation.Profitability!,
                sourceSnapshot.HarvestQuantityUnit));
    }

    private static Error MapFailure(
        ProfitSharingWaterfallSourceCalculation result)
    {
        return result.Failure switch
        {
            ProfitSharingWaterfallSourceFailure
                .CapitalIdentityConflict =>
                ProfitSharingPreviewErrors.CapitalIdentityConflict(
                    result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSourceFailure
                .CapitalNotInScheme =>
                ProfitSharingPreviewErrors.CapitalNotInScheme(
                    result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSourceFailure
                .CapitalRoleMismatch =>
                ProfitSharingPreviewErrors.CapitalRoleMismatch(
                    result.ContributorCode ?? string.Empty),

            ProfitSharingWaterfallSourceFailure
                .SourceDataChanged =>
                ProfitSharingPreviewErrors.SourceDataChanged(),

            _ => ProfitSharingPreviewErrors.CalculationUnavailable(
                result.Message ??
                "The source data is not valid for this scheme.")
        };
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId)
    {
        if (organizationId == Guid.Empty)
        {
            return ProfitSharingPreviewErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return ProfitSharingPreviewErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        return null;
    }

    private static Result<ProfitSharingPreviewResponse> Failure(
        Error error)
    {
        return Result<ProfitSharingPreviewResponse>.Failure(error);
    }
}
