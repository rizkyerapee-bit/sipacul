using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Organizations.Contracts;
using SiPacul.Application.Organizations.Mappings;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Organizations;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Organizations.Services;

public sealed class OrganizationService :
    IOrganizationService
{
    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public OrganizationService(
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _organizationRepository =
            organizationRepository;

        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrganizationResponse>> CreateAsync(
        CreateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization request cannot be null."));
        }

        Organization organization;

        try
        {
            organization = Organization.Create(
                request.Code,
                request.Name,
                request.LegalName,
                request.TimeZone);
        }
        catch (ArgumentException exception)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    exception.Message));
        }

        var codeExists =
            await _organizationRepository.CodeExistsAsync(
                organization.Code,
                cancellationToken);

        if (codeExists)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.CodeAlreadyExists(
                    organization.Code));
        }

        _organizationRepository.Add(organization);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<OrganizationResponse>.Success(
            organization.ToResponse());
    }

    public async Task<Result<IReadOnlyList<OrganizationResponse>>>
        GetAllAsync(
            CancellationToken cancellationToken = default)
    {
        var organizations =
            await _organizationRepository.GetAllAsync(
                cancellationToken);

        IReadOnlyList<OrganizationResponse> responses =
            organizations
                .Select(organization =>
                    organization.ToResponse())
                .ToArray();

        return Result<IReadOnlyList<OrganizationResponse>>
            .Success(responses);
    }

    public async Task<Result<OrganizationResponse>> GetByIdAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization identifier cannot be empty."));
        }

        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.NotFound(
                    organizationId));
        }

        return Result<OrganizationResponse>.Success(
            organization.ToResponse());
    }

    public async Task<Result<OrganizationResponse>> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization identifier cannot be empty."));
        }

        if (request is null)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization request cannot be null."));
        }

        var organization =
            await _organizationRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.NotFound(
                    organizationId));
        }

        var previousName = organization.Name;
        var previousLegalName = organization.LegalName;
        var previousTimeZone = organization.TimeZone;

        try
        {
            organization.Update(
                request.Name,
                request.LegalName,
                request.TimeZone);
        }
        catch (ArgumentException exception)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    exception.Message));
        }

        var hasChanged =
            previousName != organization.Name ||
            previousLegalName != organization.LegalName ||
            previousTimeZone != organization.TimeZone;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<OrganizationResponse>.Success(
            organization.ToResponse());
    }

    public Task<Result<OrganizationResponse>> ActivateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            true,
            cancellationToken);
    }

    public Task<Result<OrganizationResponse>> DeactivateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            false,
            cancellationToken);
    }

    private async Task<Result<OrganizationResponse>>
        SetActiveStatusAsync(
            Guid organizationId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        if (organizationId == Guid.Empty)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.Validation(
                    "Organization identifier cannot be empty."));
        }

        var organization =
            await _organizationRepository
                .GetByIdForUpdateAsync(
                    organizationId,
                    cancellationToken);

        if (organization is null)
        {
            return Result<OrganizationResponse>.Failure(
                OrganizationErrors.NotFound(
                    organizationId));
        }

        var previousStatus = organization.IsActive;

        if (shouldBeActive)
        {
            organization.Activate();
        }
        else
        {
            organization.Deactivate();
        }

        if (previousStatus != organization.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<OrganizationResponse>.Success(
            organization.ToResponse());
    }
}
