using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Mappings;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Assignments;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Assignments.Services;

public sealed class ProfitSharingSchemeAssignmentService :
    IProfitSharingSchemeAssignmentService
{
    private readonly IProfitSharingSchemeAssignmentRepository
        _assignmentRepository;

    private readonly IProfitSharingSchemeRepository
        _schemeRepository;

    private readonly ICropCycleRepository _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public ProfitSharingSchemeAssignmentService(
        IProfitSharingSchemeAssignmentRepository
            assignmentRepository,
        IProfitSharingSchemeRepository schemeRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(assignmentRepository);
        ArgumentNullException.ThrowIfNull(schemeRepository);
        ArgumentNullException.ThrowIfNull(cropCycleRepository);
        ArgumentNullException.ThrowIfNull(organizationRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _assignmentRepository = assignmentRepository;
        _schemeRepository = schemeRepository;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfitSharingSchemeAssignmentResponse>>
        AssignAsync(
            Guid organizationId,
            Guid cropCycleId,
            AssignProfitSharingSchemeRequest request,
            CancellationToken cancellationToken = default)
    {
        var validationError = ValidateAssignmentRequest(
            organizationId,
            cropCycleId,
            request);

        if (validationError is not null)
        {
            return Failure(validationError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .OrganizationNotFound(organizationId));
        }

        var cropCycle = await _cropCycleRepository.GetByIdAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycle is null)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .CropCycleNotFound(cropCycleId));
        }

        if (cropCycle.Status is CropCycleStatus.Completed or
            CropCycleStatus.Cancelled)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .CropCycleClosed(cropCycleId));
        }

        var assignment = await _assignmentRepository
            .GetByCropCycleForUpdateAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (assignment is not null &&
            assignment.SourceSchemeId == request.SchemeId)
        {
            return Success(assignment);
        }

        if (assignment is not null &&
            cropCycle.Status != CropCycleStatus.Planned)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .AssignmentLocked(cropCycleId));
        }

        var scheme = await _schemeRepository.GetByIdAsync(
            organizationId,
            request.SchemeId,
            cancellationToken);

        if (scheme is null)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .SchemeNotFound(request.SchemeId));
        }

        if (scheme.Status != ProfitSharingSchemeStatus.Active)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .SchemeNotActive(request.SchemeId));
        }

        try
        {
            if (assignment is null)
            {
                assignment = ProfitSharingSchemeAssignment.Create(
                    organizationId,
                    cropCycleId,
                    scheme);

                _assignmentRepository.Add(assignment);
            }
            else
            {
                assignment.ReplaceSnapshot(scheme);
            }
        }
        catch (ArgumentException exception)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors.Validation(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Success(assignment);
    }

    public async Task<Result<ProfitSharingSchemeAssignmentResponse>>
        GetAsync(
            Guid organizationId,
            Guid cropCycleId,
            CancellationToken cancellationToken = default)
    {
        var validationError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (validationError is not null)
        {
            return Failure(validationError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .OrganizationNotFound(organizationId));
        }

        var cropCycle = await _cropCycleRepository.GetByIdAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (cropCycle is null)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .CropCycleNotFound(cropCycleId));
        }

        var assignment = await _assignmentRepository
            .GetByCropCycleAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (assignment is null)
        {
            return Failure(
                ProfitSharingSchemeAssignmentErrors
                    .AssignmentNotFound(cropCycleId));
        }

        return Success(assignment);
    }

    private async Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken) is not null;
    }

    private static Error? ValidateAssignmentRequest(
        Guid organizationId,
        Guid cropCycleId,
        AssignProfitSharingSchemeRequest? request)
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
            return ProfitSharingSchemeAssignmentErrors.Validation(
                "Profit sharing scheme assignment request cannot " +
                "be null.");
        }

        if (request.SchemeId == Guid.Empty)
        {
            return ProfitSharingSchemeAssignmentErrors.Validation(
                "Profit sharing scheme identifier cannot be " +
                "empty.");
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId)
    {
        if (organizationId == Guid.Empty)
        {
            return ProfitSharingSchemeAssignmentErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return ProfitSharingSchemeAssignmentErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        return null;
    }

    private static Result<ProfitSharingSchemeAssignmentResponse>
        Success(ProfitSharingSchemeAssignment assignment)
    {
        return Result<ProfitSharingSchemeAssignmentResponse>.Success(
            assignment.ToResponse());
    }

    private static Result<ProfitSharingSchemeAssignmentResponse>
        Failure(Error error)
    {
        return Result<ProfitSharingSchemeAssignmentResponse>.Failure(
            error);
    }
}
