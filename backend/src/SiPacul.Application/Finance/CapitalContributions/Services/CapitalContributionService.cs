using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Application.Finance.CapitalContributions.Mappings;
using SiPacul.Application.Finance.CapitalContributions.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.CapitalContributions.Services;

public sealed class CapitalContributionService :
    ICapitalContributionService
{
    private readonly ICapitalContributionRepository
        _contributionRepository;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CapitalContributionService(
        ICapitalContributionRepository contributionRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _contributionRepository = contributionRepository;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CapitalContributionResponse>>
        CreateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateCapitalContributionRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            request,
            "Capital contribution request cannot be null.");

        if (requestError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(requestError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CapitalContributionResponse>
                .Failure(parentResult.Error);
        }

        var dateError = ValidateContributionDate(
            request.ContributionDate,
            parentResult.Value);

        if (dateError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(dateError);
        }

        CapitalContribution contribution;

        try
        {
            contribution = CapitalContribution.Create(
                organizationId,
                cropCycleId,
                request.Code,
                request.ContributionDate,
                request.ContributorCode,
                request.ContributorName,
                request.ContributorRole,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors.Validation(
                        exception.Message));
        }

        if (await _contributionRepository.CodeExistsAsync(
                organizationId,
                cropCycleId,
                contribution.Code,
                cancellationToken))
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors
                        .CodeAlreadyExists(
                            contribution.Code));
        }

        _contributionRepository.Add(contribution);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CapitalContributionResponse>.Success(
            contribution.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CapitalContributionResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CapitalContributionFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<CapitalContributionResponse>>
                .Failure(identifierError);
        }

        filter ??= new CapitalContributionFilter();

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<CapitalContributionResponse>>
                .Failure(filterError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<
                IReadOnlyList<CapitalContributionResponse>>
                .Failure(parentResult.Error);
        }

        var contributions =
            await _contributionRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                filter.Status,
                filter.ContributorRole,
                filter.ContributionDateFrom,
                filter.ContributionDateTo,
                NormalizeContributorCodeFilter(
                    filter.ContributorCode),
                NormalizeTextFilter(
                    filter.ContributorName),
                cancellationToken);

        IReadOnlyList<CapitalContributionResponse> responses =
            contributions
                .Select(contribution =>
                    contribution.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CapitalContributionResponse>>
            .Success(responses);
    }

    public async Task<Result<CapitalContributionResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            contributionId);

        if (identifierError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(identifierError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CapitalContributionResponse>
                .Failure(parentResult.Error);
        }

        var contribution =
            await _contributionRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                contributionId,
                cancellationToken);

        if (contribution is null)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors.NotFound(
                        contributionId));
        }

        return Result<CapitalContributionResponse>.Success(
            contribution.ToResponse());
    }

    public async Task<Result<CapitalContributionResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            UpdateCapitalContributionRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            contributionId,
            request,
            "Update capital contribution request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            contributionId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CapitalContributionResponse>
                .Failure(contextResult.Error);
        }

        var dateError = ValidateContributionDate(
            request.ContributionDate,
            contextResult.Value.CropCycle);

        if (dateError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(dateError);
        }

        var contribution =
            contextResult.Value.Contribution;

        var previousDate = contribution.ContributionDate;
        var previousContributorCode =
            contribution.ContributorCode;
        var previousContributorName =
            contribution.ContributorName;
        var previousRole = contribution.ContributorRole;
        var previousAmount = contribution.Amount;
        var previousPaymentMethod =
            contribution.PaymentMethod;
        var previousReference =
            contribution.ReferenceNumber;
        var previousNotes = contribution.Notes;

        try
        {
            contribution.UpdateDraft(
                request.ContributionDate,
                request.ContributorCode,
                request.ContributorName,
                request.ContributorRole,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        if (previousDate != contribution.ContributionDate ||
            previousContributorCode !=
                contribution.ContributorCode ||
            previousContributorName !=
                contribution.ContributorName ||
            previousRole != contribution.ContributorRole ||
            previousAmount != contribution.Amount ||
            previousPaymentMethod !=
                contribution.PaymentMethod ||
            previousReference !=
                contribution.ReferenceNumber ||
            previousNotes != contribution.Notes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CapitalContributionResponse>.Success(
            contribution.ToResponse());
    }

    public async Task<Result<CapitalContributionResponse>>
        ConfirmAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            contributionId);

        if (identifierError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(identifierError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            contributionId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CapitalContributionResponse>
                .Failure(contextResult.Error);
        }

        var contribution =
            contextResult.Value.Contribution;

        try
        {
            contribution.Confirm();
        }
        catch (InvalidOperationException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CapitalContributionResponse>.Success(
            contribution.ToResponse());
    }

    public async Task<Result<CapitalContributionResponse>>
        CancelAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancelCapitalContributionRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            contributionId,
            request,
            "Cancel capital contribution request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CapitalContributionResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            contributionId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CapitalContributionResponse>
                .Failure(contextResult.Error);
        }

        var contribution =
            contextResult.Value.Contribution;

        try
        {
            contribution.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CapitalContributionResponse>
                .Failure(
                    CapitalContributionErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CapitalContributionResponse>.Success(
            contribution.ToResponse());
    }

    private async Task<Result<CropCycle>> GetParentAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return Result<CropCycle>.Failure(
                CapitalContributionErrors
                    .OrganizationNotFound(
                        organizationId));
        }

        var cropCycle =
            await _cropCycleRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return Result<CropCycle>.Failure(
                CapitalContributionErrors
                    .CropCycleNotFound(
                        cropCycleId));
        }

        return Result<CropCycle>.Success(cropCycle);
    }

    private async Task<Result<MutationContext>>
        GetMutationContextAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid contributionId,
            CancellationToken cancellationToken)
    {
        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<MutationContext>.Failure(
                parentResult.Error);
        }

        var contribution =
            await _contributionRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    contributionId,
                    cancellationToken);

        if (contribution is null)
        {
            return Result<MutationContext>.Failure(
                CapitalContributionErrors.NotFound(
                    contributionId));
        }

        return Result<MutationContext>.Success(
            new MutationContext(
                parentResult.Value,
                contribution));
    }

    private static Error? ValidateContributionDate(
        DateOnly contributionDate,
        CropCycle cropCycle)
    {
        var earliestDate =
            cropCycle.PlannedStartDate.AddYears(-1);

        var latestReferenceDate =
            cropCycle.ActualHarvestDate ??
            cropCycle.ExpectedHarvestDate;

        var latestDate =
            latestReferenceDate.AddYears(1);

        if (contributionDate < earliestDate ||
            contributionDate > latestDate)
        {
            return CapitalContributionErrors.DateOutOfRange(
                contributionDate,
                earliestDate,
                latestDate);
        }

        return null;
    }

    private static Error? ValidateFilter(
        CapitalContributionFilter filter)
    {
        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return CapitalContributionErrors.Validation(
                "Capital contribution status is not supported.");
        }

        if (filter.ContributorRole.HasValue &&
            !Enum.IsDefined(filter.ContributorRole.Value))
        {
            return CapitalContributionErrors.Validation(
                "Capital contributor role is not supported.");
        }

        if (filter.ContributionDateFrom.HasValue &&
            filter.ContributionDateTo.HasValue &&
            filter.ContributionDateFrom.Value >
                filter.ContributionDateTo.Value)
        {
            return CapitalContributionErrors.Validation(
                "Contribution date-from cannot be after " +
                "contribution date-to.");
        }

        if (filter.ContributorCode is not null &&
            string.IsNullOrWhiteSpace(
                filter.ContributorCode))
        {
            return CapitalContributionErrors.Validation(
                "Contributor-code filter cannot be blank.");
        }

        if (filter.ContributorName is not null &&
            string.IsNullOrWhiteSpace(
                filter.ContributorName))
        {
            return CapitalContributionErrors.Validation(
                "Contributor-name filter cannot be blank.");
        }

        return null;
    }

    private static string? NormalizeContributorCodeFilter(
        string? contributorCode)
    {
        return string.IsNullOrWhiteSpace(contributorCode)
            ? null
            : contributorCode.Trim().ToUpperInvariant();
    }

    private static string? NormalizeTextFilter(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CapitalContributionErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            contributionId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CapitalContributionErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid? contributionId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return CapitalContributionErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return CapitalContributionErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (contributionId.HasValue &&
            contributionId.Value == Guid.Empty)
        {
            return CapitalContributionErrors.Validation(
                "Capital contribution identifier cannot be empty.");
        }

        return null;
    }

    private sealed record MutationContext(
        CropCycle CropCycle,
        CapitalContribution Contribution);
}
