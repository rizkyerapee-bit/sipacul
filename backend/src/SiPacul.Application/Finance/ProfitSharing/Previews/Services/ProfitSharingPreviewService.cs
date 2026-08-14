using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Previews;
using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Previews.Mappings;
using SiPacul.Application.Finance.Profitability.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.Profitability;
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

        try
        {
            var profitability =
                CropCycleProfitabilityReport.Calculate(
                    sourceSnapshot.ToInput(
                        _timeProvider.GetUtcNow().UtcDateTime));

            var inputResult = BuildWaterfallInput(
                assignment,
                contributions,
                profitability);

            if (inputResult.IsFailure)
            {
                return Failure(inputResult.Error);
            }

            var calculation =
                ProfitSharingWaterfallCalculator.Calculate(
                    profitability,
                    inputResult.Value);

            return Result<ProfitSharingPreviewResponse>.Success(
                calculation.ToPreviewResponse(
                    assignment,
                    profitability,
                    sourceSnapshot.HarvestQuantityUnit));
        }
        catch (ArgumentException exception)
        {
            return Failure(
                ProfitSharingPreviewErrors.CalculationUnavailable(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                ProfitSharingPreviewErrors.CalculationUnavailable(
                    exception.Message));
        }
        catch (OverflowException exception)
        {
            return Failure(
                ProfitSharingPreviewErrors.CalculationUnavailable(
                    exception.Message));
        }
    }

    private static Result<ProfitSharingWaterfallSchemeInput>
        BuildWaterfallInput(
            ProfitSharingSchemeAssignment assignment,
            IReadOnlyCollection<CapitalContribution> contributions,
            CropCycleProfitabilityReport profitability)
    {
        var participantByCode = assignment.Participants.ToDictionary(
            participant => participant.ParticipantCode,
            StringComparer.Ordinal);

        var capitalByParticipant = assignment.Participants.ToDictionary(
            participant => participant.ParticipantCode,
            _ => 0m,
            StringComparer.Ordinal);

        var groupedContributions = contributions
            .GroupBy(
                contribution => contribution.ContributorCode,
                StringComparer.Ordinal)
            .ToArray();

        foreach (var group in groupedContributions)
        {
            if (!participantByCode.TryGetValue(
                    group.Key,
                    out var participant))
            {
                return Result<ProfitSharingWaterfallSchemeInput>.Failure(
                    ProfitSharingPreviewErrors.CapitalNotInScheme(
                        group.Key));
            }

            if (group.Select(contribution => contribution.ContributorRole)
                    .Distinct()
                    .Count() != 1 ||
                group.Select(contribution => contribution.ContributorName)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != 1)
            {
                return Result<ProfitSharingWaterfallSchemeInput>.Failure(
                    ProfitSharingPreviewErrors.CapitalIdentityConflict(
                        group.Key));
            }

            var contributorRole = group.First().ContributorRole;

            if (!IsCompatibleRole(
                    participant.ParticipantRole,
                    contributorRole))
            {
                return Result<ProfitSharingWaterfallSchemeInput>.Failure(
                    ProfitSharingPreviewErrors.CapitalRoleMismatch(
                        group.Key));
            }

            capitalByParticipant[group.Key] = Math.Round(
                group.Sum(contribution => contribution.Amount),
                2,
                MidpointRounding.AwayFromZero);
        }

        var detailedCapital = Math.Round(
            capitalByParticipant.Values.Sum(),
            2,
            MidpointRounding.AwayFromZero);

        var detailedInvestorCapital = Math.Round(
            contributions
                .Where(contribution =>
                    contribution.ContributorRole ==
                        CapitalContributorRole.Investor)
                .Sum(contribution => contribution.Amount),
            2,
            MidpointRounding.AwayFromZero);

        var detailedPartnerCapital = Math.Round(
            contributions
                .Where(contribution =>
                    contribution.ContributorRole ==
                        CapitalContributorRole.Partner)
                .Sum(contribution => contribution.Amount),
            2,
            MidpointRounding.AwayFromZero);

        if (detailedCapital != profitability.TotalConfirmedCapital ||
            detailedInvestorCapital !=
                profitability.ConfirmedInvestorCapital ||
            detailedPartnerCapital !=
                profitability.ConfirmedPartnerCapital)
        {
            return Result<ProfitSharingWaterfallSchemeInput>.Failure(
                ProfitSharingPreviewErrors.SourceDataChanged());
        }

        var participants = assignment.Participants
            .OrderBy(participant => participant.Sequence)
            .Select(participant =>
                new ProfitSharingWaterfallParticipantInput(
                    participant.ParticipantCode,
                    participant.ParticipantName,
                    participant.ParticipantRole,
                    capitalByParticipant[participant.ParticipantCode],
                    participant.ParticipatesInResidualProfit,
                    participant.Sequence))
            .ToArray();

        var priorityRules = assignment.PriorityRules
            .OrderBy(rule => rule.Sequence)
            .Select(rule =>
                new ProfitSharingPriorityRuleInput(
                    rule.RuleCode,
                    rule.RuleType,
                    rule.RecipientCode,
                    ProfitSharingRate.FromFraction(
                        rule.RateNumerator,
                        rule.RateDenominator),
                    rule.Sequence))
            .ToArray();

        var residualPolicy = BuildResidualPolicy(assignment);

        return Result<ProfitSharingWaterfallSchemeInput>.Success(
            new ProfitSharingWaterfallSchemeInput(
                participants,
                priorityRules,
                residualPolicy));
    }

    private static ProfitSharingResidualPolicyInput BuildResidualPolicy(
        ProfitSharingSchemeAssignment assignment)
    {
        return assignment.ResidualMethod switch
        {
            ProfitSharingResidualMethod.RemainderToParticipant =>
                ProfitSharingResidualPolicyInput
                    .RemainderToParticipant(
                        assignment.ResidualRecipientCode ??
                        throw new InvalidOperationException(
                            "Residual recipient is missing from the assigned scheme.")),

            ProfitSharingResidualMethod.ProRataCapital =>
                ProfitSharingResidualPolicyInput.ProRataCapital(),

            ProfitSharingResidualMethod.FixedPercentage =>
                ProfitSharingResidualPolicyInput.FixedPercentage(
                    assignment.ResidualShares
                        .OrderBy(share => share.Sequence)
                        .Select(share =>
                            new ProfitSharingResidualShareInput(
                                share.RecipientCode,
                                ProfitSharingRate.FromFraction(
                                    share.RateNumerator,
                                    share.RateDenominator),
                                share.Sequence))
                        .ToArray()),

            _ => throw new InvalidOperationException(
                "Residual method is unsupported.")
        };
    }

    private static bool IsCompatibleRole(
        ProfitSharingParticipantRole participantRole,
        CapitalContributorRole contributorRole)
    {
        return participantRole switch
        {
            ProfitSharingParticipantRole.Company =>
                contributorRole == CapitalContributorRole.Investor,

            ProfitSharingParticipantRole.PassiveInvestor =>
                contributorRole == CapitalContributorRole.Investor,

            ProfitSharingParticipantRole.ManagingPartner =>
                contributorRole == CapitalContributorRole.Partner,

            ProfitSharingParticipantRole.Other => true,
            _ => false
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
