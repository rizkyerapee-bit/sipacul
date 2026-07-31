using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Application.Cultivation.Activities.Mappings;
using SiPacul.Application.Cultivation.Activities.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Cultivation.Sops.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Cultivation.Activities.Services;

public sealed class CultivationActivityService :
    ICultivationActivityService
{
    private readonly ICultivationActivityRepository
        _activityRepository;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly ICultivationSopRepository
        _sopRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CultivationActivityService(
        ICultivationActivityRepository activityRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        ICultivationSopRepository sopRepository,
        IUnitOfWork unitOfWork)
    {
        _activityRepository = activityRepository;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository = organizationRepository;
        _sopRepository = sopRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CultivationActivityResponse>>
        CreateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateCultivationActivityRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            request,
            "Cultivation activity request cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var parentResult = await GetCropCycleAsync(
            organizationId,
            cropCycleId,
            true,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(parentResult.Error);
        }

        var cropCycle = parentResult.Value;

        var plannedDateError = ValidatePlannedDate(
            request.PlannedDate,
            cropCycle.ExpectedHarvestDate);

        if (plannedDateError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(plannedDateError);
        }

        var snapshotResult = await ResolveSopSnapshotAsync(
            organizationId,
            cropCycle.CommodityId,
            request.CultivationSopId,
            request.CultivationSopStepId,
            cancellationToken);

        if (snapshotResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(snapshotResult.Error);
        }

        CultivationActivity activity;

        try
        {
            var snapshot = snapshotResult.Value;

            activity = CultivationActivity.Create(
                organizationId,
                cropCycleId,
                request.Code,
                request.Name,
                request.ActivityType,
                request.PlannedDate,
                snapshot.CultivationSopId,
                snapshot.CultivationSopStepId,
                snapshot.Sequence,
                snapshot.Name,
                snapshot.PlannedDayOffset,
                snapshot.EstimatedDurationDays,
                snapshot.IsRequired,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }

        if (await _activityRepository.CodeExistsAsync(
                organizationId,
                cropCycleId,
                activity.Code,
                cancellationToken))
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .CodeAlreadyExists(
                            activity.Code));
        }

        _activityRepository.Add(activity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CultivationActivityResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationActivityFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<CultivationActivityResponse>>
                .Failure(identifierError);
        }

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<CultivationActivityResponse>>
                .Failure(filterError);
        }

        var parentResult = await GetCropCycleAsync(
            organizationId,
            cropCycleId,
            false,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<
                IReadOnlyList<CultivationActivityResponse>>
                .Failure(parentResult.Error);
        }

        filter ??= new CultivationActivityFilter();

        var activities =
            await _activityRepository.GetAllAsync(
                organizationId,
                cropCycleId,
                filter.Status,
                filter.ActivityType,
                filter.PlannedFrom,
                filter.PlannedTo,
                filter.CultivationSopStepId,
                cancellationToken);

        IReadOnlyList<CultivationActivityResponse>
            responses = activities
                .Select(activity =>
                    activity.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CultivationActivityResponse>>
            .Success(responses);
    }

    public async Task<Result<CultivationActivityResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            activityId);

        if (identifierError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(identifierError);
        }

        var parentResult = await GetCropCycleAsync(
            organizationId,
            cropCycleId,
            false,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(parentResult.Error);
        }

        var activity =
            await _activityRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                activityId,
                cancellationToken);

        if (activity is null)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.NotFound(
                        cropCycleId,
                        activityId));
        }

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        UpdatePlanAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            UpdateCultivationActivityPlanRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Update cultivation activity request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var context = contextResult.Value;

        var plannedDateError = ValidatePlannedDate(
            request.PlannedDate,
            context.CropCycle.ExpectedHarvestDate);

        if (plannedDateError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(plannedDateError);
        }

        var activity = context.Activity;
        var previousName = activity.Name;
        var previousType = activity.ActivityType;
        var previousDate = activity.PlannedDate;
        var previousNotes = activity.Notes;

        try
        {
            activity.UpdatePlan(
                request.Name,
                request.ActivityType,
                request.PlannedDate,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        if (previousName != activity.Name ||
            previousType != activity.ActivityType ||
            previousDate != activity.PlannedDate ||
            previousNotes != activity.Notes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        StartAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            StartCultivationActivityRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Start cultivation activity request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.Start(request.ActualStartDate);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        CompleteAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            CompleteCultivationActivityRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Complete cultivation activity request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.Complete(
                request.ActualCompletionDate,
                request.Outcome,
                request.IssueNotes,
                request.SopComplianceStatus,
                request.DeviationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        CancelAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            CancelCultivationActivityRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Cancel cultivation activity request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.Cancel(request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        UpdateExecutionNotesAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            UpdateCultivationActivityNotesRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Update cultivation activity notes request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;
        var previousNotes = activity.Notes;
        var previousIssues = activity.IssueNotes;

        try
        {
            activity.UpdateExecutionNotes(
                request.Notes,
                request.IssueNotes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        if (previousNotes != activity.Notes ||
            previousIssues != activity.IssueNotes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        AddResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            AddCultivationActivityResourceRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            activityId,
            request,
            "Add cultivation resource request cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.AddResource(
                request.ResourceType,
                request.Description,
                request.Quantity,
                request.Unit,
                request.UnitCost,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        UpdateResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            Guid resourceId,
            UpdateCultivationActivityResourceRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateResourceRequest(
            organizationId,
            cropCycleId,
            activityId,
            resourceId,
            request,
            "Update cultivation resource request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.UpdateResource(
                resourceId,
                request.Description,
                request.Quantity,
                request.Unit,
                request.UnitCost,
                request.Notes);
        }
        catch (KeyNotFoundException)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .ResourceNotFound(resourceId));
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    public async Task<Result<CultivationActivityResponse>>
        RemoveResourceAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            Guid resourceId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateResourceIdentifiers(
            organizationId,
            cropCycleId,
            activityId,
            resourceId);

        if (identifierError is not null)
        {
            return Result<CultivationActivityResponse>
                .Failure(identifierError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationActivityResponse>
                .Failure(contextResult.Error);
        }

        var activity = contextResult.Value.Activity;

        try
        {
            activity.RemoveResource(resourceId);
        }
        catch (KeyNotFoundException)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .ResourceNotFound(resourceId));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationActivityResponse>
                .Failure(
                    CultivationActivityErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationActivityResponse>
            .Success(activity.ToResponse());
    }

    private async Task<Result<MutationContext>>
        GetMutationContextAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid activityId,
            CancellationToken cancellationToken)
    {
        var parentResult = await GetCropCycleAsync(
            organizationId,
            cropCycleId,
            true,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<MutationContext>.Failure(
                parentResult.Error);
        }

        var activity =
            await _activityRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cropCycleId,
                    activityId,
                    cancellationToken);

        if (activity is null)
        {
            return Result<MutationContext>.Failure(
                CultivationActivityErrors.NotFound(
                    cropCycleId,
                    activityId));
        }

        return Result<MutationContext>.Success(
            new MutationContext(
                parentResult.Value,
                activity));
    }

    private async Task<Result<CropCycle>> GetCropCycleAsync(
        Guid organizationId,
        Guid cropCycleId,
        bool requireMutable,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return Result<CropCycle>.Failure(
                CultivationActivityErrors
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
                CultivationActivityErrors
                    .CropCycleNotFound(
                        cropCycleId));
        }

        if (requireMutable &&
            cropCycle.Status is
                CropCycleStatus.Completed or
                CropCycleStatus.Cancelled)
        {
            return Result<CropCycle>.Failure(
                CultivationActivityErrors
                    .CropCycleTerminal(
                        cropCycleId));
        }

        return Result<CropCycle>.Success(cropCycle);
    }

    private async Task<Result<SopSnapshot>>
        ResolveSopSnapshotAsync(
            Guid organizationId,
            Guid commodityId,
            Guid? cultivationSopId,
            Guid? cultivationSopStepId,
            CancellationToken cancellationToken)
    {
        if (!cultivationSopId.HasValue &&
            !cultivationSopStepId.HasValue)
        {
            return Result<SopSnapshot>.Success(
                SopSnapshot.Empty);
        }

        if (!cultivationSopId.HasValue ||
            !cultivationSopStepId.HasValue)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors.Validation(
                    "Cultivation SOP and SOP step identifiers " +
                    "must be supplied together."));
        }

        if (cultivationSopId == Guid.Empty ||
            cultivationSopStepId == Guid.Empty)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors.Validation(
                    "Cultivation SOP and SOP step identifiers " +
                    "cannot be empty."));
        }

        var sop = await _sopRepository.GetByIdAsync(
            organizationId,
            cultivationSopId.Value,
            cancellationToken);

        if (sop is null)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors.SopNotFound(
                    cultivationSopId.Value));
        }

        if (!sop.IsActive)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors.SopInactive(
                    cultivationSopId.Value));
        }

        if (sop.CommodityId != commodityId)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors
                    .SopCommodityMismatch(
                        cultivationSopId.Value,
                        commodityId));
        }

        var step = sop.Steps.SingleOrDefault(
            candidate =>
                candidate.Id ==
                    cultivationSopStepId.Value);

        if (step is null)
        {
            return Result<SopSnapshot>.Failure(
                CultivationActivityErrors
                    .SopStepMismatch(
                        cultivationSopId.Value,
                        cultivationSopStepId.Value));
        }

        return Result<SopSnapshot>.Success(
            new SopSnapshot(
                sop.Id,
                step.Id,
                step.Sequence,
                step.Name,
                step.PlannedDayOffset,
                step.EstimatedDurationDays,
                step.IsRequired));
    }

    private static Error? ValidatePlannedDate(
        DateOnly plannedDate,
        DateOnly expectedHarvestDate)
    {
        if (plannedDate > expectedHarvestDate)
        {
            return CultivationActivityErrors
                .PlannedDateOutOfRange(
                    plannedDate,
                    expectedHarvestDate);
        }

        return null;
    }

    private static Error? ValidateFilter(
        CultivationActivityFilter? filter)
    {
        if (filter is null)
        {
            return null;
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return CultivationActivityErrors.Validation(
                "Cultivation activity status is not supported.");
        }

        if (filter.ActivityType.HasValue &&
            !Enum.IsDefined(filter.ActivityType.Value))
        {
            return CultivationActivityErrors.Validation(
                "Cultivation activity type is not supported.");
        }

        if (filter.CultivationSopStepId == Guid.Empty)
        {
            return CultivationActivityErrors.Validation(
                "Cultivation SOP step identifier cannot be empty.");
        }

        if (filter.PlannedFrom.HasValue &&
            filter.PlannedTo.HasValue &&
            filter.PlannedFrom.Value >
                filter.PlannedTo.Value)
        {
            return CultivationActivityErrors.Validation(
                "Planned-from date cannot be after " +
                "the planned-to date.");
        }

        return null;
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
            return CultivationActivityErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            activityId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CultivationActivityErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateResourceRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        Guid resourceId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateResourceIdentifiers(
            organizationId,
            cropCycleId,
            activityId,
            resourceId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CultivationActivityErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid? activityId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return CultivationActivityErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return CultivationActivityErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (activityId.HasValue &&
            activityId.Value == Guid.Empty)
        {
            return CultivationActivityErrors.Validation(
                "Cultivation activity identifier " +
                "cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateResourceIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        Guid resourceId)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            activityId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (resourceId == Guid.Empty)
        {
            return CultivationActivityErrors.Validation(
                "Cultivation activity resource identifier " +
                "cannot be empty.");
        }

        return null;
    }

    private sealed record MutationContext(
        CropCycle CropCycle,
        CultivationActivity Activity);

    private sealed record SopSnapshot(
        Guid? CultivationSopId,
        Guid? CultivationSopStepId,
        int? Sequence,
        string? Name,
        int? PlannedDayOffset,
        int? EstimatedDurationDays,
        bool? IsRequired)
    {
        public static readonly SopSnapshot Empty =
            new(
                null,
                null,
                null,
                null,
                null,
                null,
                null);
    }
}
