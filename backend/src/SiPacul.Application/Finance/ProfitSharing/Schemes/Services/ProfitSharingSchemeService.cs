using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Mappings;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.ProfitSharing.Schemes.Services;

public sealed class ProfitSharingSchemeService :
    IProfitSharingSchemeService
{
    private readonly IProfitSharingSchemeRepository
        _schemeRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IProfitSharingSchemeActivationProcessor
        _activationProcessor;

    private readonly IUnitOfWork _unitOfWork;

    public ProfitSharingSchemeService(
        IProfitSharingSchemeRepository schemeRepository,
        IOrganizationRepository organizationRepository,
        IProfitSharingSchemeActivationProcessor
            activationProcessor,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(schemeRepository);
        ArgumentNullException.ThrowIfNull(organizationRepository);
        ArgumentNullException.ThrowIfNull(activationProcessor);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _schemeRepository = schemeRepository;
        _organizationRepository = organizationRepository;
        _activationProcessor = activationProcessor;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfitSharingSchemeResponse>>
        CreateDraftAsync(
            Guid organizationId,
            CreateProfitSharingSchemeRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            request,
            "Profit sharing scheme request cannot be null.");

        if (requestError is not null)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                requestError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.OrganizationNotFound(
                    organizationId));
        }

        ProfitSharingScheme scheme;

        try
        {
            scheme = ProfitSharingScheme.CreateDraft(
                organizationId,
                request.Code,
                request.Name,
                request.Description,
                request.Participants.ToDefinitions(),
                request.PriorityRules.ToDefinitions(),
                request.ResidualMethod,
                request.ResidualRecipientCode,
                request.ResidualShares.ToDefinitions());
        }
        catch (ArgumentException exception)
        {
            return ValidationFailure(exception.Message);
        }
        catch (OverflowException exception)
        {
            return ValidationFailure(exception.Message);
        }

        if (await _schemeRepository.CodeExistsAsync(
                organizationId,
                scheme.Code,
                cancellationToken))
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.CodeAlreadyExists(
                    scheme.Code));
        }

        _schemeRepository.Add(scheme);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProfitSharingSchemeResponse>.Success(
            scheme.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<ProfitSharingSchemeResponse>>>
        GetAllAsync(
            Guid organizationId,
            ProfitSharingSchemeFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return Result<
                IReadOnlyList<ProfitSharingSchemeResponse>>
                .Failure(organizationError);
        }

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<ProfitSharingSchemeResponse>>
                .Failure(filterError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<
                IReadOnlyList<ProfitSharingSchemeResponse>>
                .Failure(
                    ProfitSharingSchemeErrors
                        .OrganizationNotFound(
                            organizationId));
        }

        filter ??= new ProfitSharingSchemeFilter();

        var schemes = await _schemeRepository.GetAllAsync(
            organizationId,
            filter.Status,
            NormalizeOptionalCode(filter.Code),
            cancellationToken);

        IReadOnlyList<ProfitSharingSchemeResponse> responses =
            schemes
                .Select(scheme => scheme.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<ProfitSharingSchemeResponse>>
            .Success(responses);
    }

    public async Task<Result<ProfitSharingSchemeResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                schemeId);

        if (identifierError is not null)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.OrganizationNotFound(
                    organizationId));
        }

        var scheme = await _schemeRepository.GetByIdAsync(
            organizationId,
            schemeId,
            cancellationToken);

        if (scheme is null)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.NotFound(schemeId));
        }

        return Result<ProfitSharingSchemeResponse>.Success(
            scheme.ToResponse());
    }

    public async Task<Result<ProfitSharingSchemeResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid schemeId,
            UpdateProfitSharingSchemeDraftRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            schemeId,
            request,
            "Profit sharing scheme update request cannot be null.");

        if (requestError is not null)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                requestError);
        }

        var schemeResult = await GetForUpdateAsync(
            organizationId,
            schemeId,
            cancellationToken);

        if (schemeResult.IsFailure)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                schemeResult.Error);
        }

        var scheme = schemeResult.Value;

        try
        {
            scheme.UpdateDraft(
                request.Name,
                request.Description,
                request.Participants.ToDefinitions(),
                request.PriorityRules.ToDefinitions(),
                request.ResidualMethod,
                request.ResidualRecipientCode,
                request.ResidualShares.ToDefinitions());
        }
        catch (ArgumentException exception)
        {
            return ValidationFailure(exception.Message);
        }
        catch (OverflowException exception)
        {
            return ValidationFailure(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return InvalidStatusFailure(exception.Message);
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProfitSharingSchemeResponse>.Success(
            scheme.ToResponse());
    }

    public async Task<Result<ProfitSharingSchemeResponse>>
        CreateNextVersionAsync(
            Guid organizationId,
            Guid sourceSchemeId,
            CancellationToken cancellationToken = default)
    {
        var sourceResult = await GetForUpdateAsync(
            organizationId,
            sourceSchemeId,
            cancellationToken);

        if (sourceResult.IsFailure)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                sourceResult.Error);
        }

        var source = sourceResult.Value;

        if (source.Status != ProfitSharingSchemeStatus.Active)
        {
            return InvalidStatusFailure(
                "Only an active scheme can create the next " +
                "version.");
        }

        if (await _schemeRepository.HasDraftAsync(
                organizationId,
                source.SchemeFamilyId,
                cancellationToken: cancellationToken))
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.DraftAlreadyExists(
                    source.SchemeFamilyId));
        }

        ProfitSharingScheme nextVersion;

        try
        {
            nextVersion =
                ProfitSharingScheme.CreateNextVersion(source);
        }
        catch (InvalidOperationException exception)
        {
            return InvalidStatusFailure(exception.Message);
        }
        catch (OverflowException exception)
        {
            return ValidationFailure(exception.Message);
        }

        _schemeRepository.Add(nextVersion);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<ProfitSharingSchemeResponse>.Success(
            nextVersion.ToResponse());
    }

    public async Task<Result<ProfitSharingSchemeResponse>>
        ActivateAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken = default)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                schemeId);

        if (identifierError is not null)
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<ProfitSharingSchemeResponse>.Failure(
                ProfitSharingSchemeErrors.OrganizationNotFound(
                    organizationId));
        }

        var activation =
            await _activationProcessor.ActivateAsync(
                organizationId,
                schemeId,
                cancellationToken);

        if (activation.IsSuccess)
        {
            return Result<ProfitSharingSchemeResponse>.Success(
                activation.Scheme!.ToResponse());
        }

        return activation.Failure switch
        {
            ProfitSharingSchemeActivationFailure.SchemeNotFound =>
                Result<ProfitSharingSchemeResponse>.Failure(
                    ProfitSharingSchemeErrors.NotFound(schemeId)),
            ProfitSharingSchemeActivationFailure.InvalidStatus =>
                InvalidStatusFailure(
                    activation.Message ??
                    "The scheme cannot be activated."),
            ProfitSharingSchemeActivationFailure
                .ConcurrencyConflict =>
                Result<ProfitSharingSchemeResponse>.Failure(
                    ProfitSharingSchemeErrors
                        .ConcurrencyConflict(
                            activation.Message ??
                            "The scheme changed during " +
                            "activation.")),
            _ => throw new InvalidOperationException(
                "Unsupported scheme activation result.")
        };
    }

    private async Task<Result<ProfitSharingScheme>>
        GetForUpdateAsync(
            Guid organizationId,
            Guid schemeId,
            CancellationToken cancellationToken)
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                schemeId);

        if (identifierError is not null)
        {
            return Result<ProfitSharingScheme>.Failure(
                identifierError);
        }

        if (!await OrganizationExistsAsync(
                organizationId,
                cancellationToken))
        {
            return Result<ProfitSharingScheme>.Failure(
                ProfitSharingSchemeErrors.OrganizationNotFound(
                    organizationId));
        }

        var scheme =
            await _schemeRepository.GetByIdForUpdateAsync(
                organizationId,
                schemeId,
                cancellationToken);

        if (scheme is null)
        {
            return Result<ProfitSharingScheme>.Failure(
                ProfitSharingSchemeErrors.NotFound(schemeId));
        }

        return Result<ProfitSharingScheme>.Success(scheme);
    }

    private async Task<bool> OrganizationExistsAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        return organization is not null;
    }

    private static Error? ValidateFilter(
        ProfitSharingSchemeFilter? filter)
    {
        if (filter is null)
        {
            return null;
        }

        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return ProfitSharingSchemeErrors.Validation(
                "Profit sharing scheme status is unsupported.");
        }

        if (!string.IsNullOrWhiteSpace(filter.Code) &&
            filter.Code.Trim().Length >
                ProfitSharingScheme.MaxCodeLength)
        {
            return ProfitSharingSchemeErrors.Validation(
                "Profit sharing scheme code is too long.");
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        return request is null
            ? ProfitSharingSchemeErrors.Validation(
                nullRequestMessage)
            : null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid schemeId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError =
            ValidateIdentifiers(
                organizationId,
                schemeId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        return request is null
            ? ProfitSharingSchemeErrors.Validation(
                nullRequestMessage)
            : null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid schemeId)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        return schemeId == Guid.Empty
            ? ProfitSharingSchemeErrors.Validation(
                "Profit sharing scheme identifier cannot be " +
                "empty.")
            : null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        return organizationId == Guid.Empty
            ? ProfitSharingSchemeErrors.Validation(
                "Organization identifier cannot be empty.")
            : null;
    }

    private static string? NormalizeOptionalCode(string? code)
    {
        return string.IsNullOrWhiteSpace(code)
            ? null
            : code.Trim().ToUpperInvariant();
    }

    private static Result<ProfitSharingSchemeResponse>
        ValidationFailure(string message)
    {
        return Result<ProfitSharingSchemeResponse>.Failure(
            ProfitSharingSchemeErrors.Validation(message));
    }

    private static Result<ProfitSharingSchemeResponse>
        InvalidStatusFailure(string message)
    {
        return Result<ProfitSharingSchemeResponse>.Failure(
            ProfitSharingSchemeErrors
                .InvalidStatusTransition(message));
    }
}
