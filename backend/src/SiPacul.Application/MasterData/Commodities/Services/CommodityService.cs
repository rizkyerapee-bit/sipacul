using SiPacul.Application.Common.Persistence;
using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Application.MasterData.Commodities.Mappings;
using SiPacul.Application.MasterData.Commodities.Persistence;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Common.ValueObjects;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.Commodities.Services;

public sealed class CommodityService :
    ICommodityService
{
    private readonly ICommodityRepository
        _commodityRepository;

    private readonly ICommodityCategoryRepository
        _categoryRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CommodityService(
        ICommodityRepository commodityRepository,
        ICommodityCategoryRepository categoryRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _commodityRepository = commodityRepository;
        _categoryRepository = categoryRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommodityResponse>>
        CreateAsync(
            Guid organizationId,
            CreateCommodityRequest request,
            CancellationToken cancellationToken = default)
    {
        var organizationIdError =
            ValidateOrganizationId(organizationId);

        if (organizationIdError is not null)
        {
            return Result<CommodityResponse>.Failure(
                organizationIdError);
        }

        if (request is null)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.Validation(
                    "Commodity request cannot be null."));
        }

        CommodityCode code;
        Commodity commodity;

        try
        {
            code = CommodityCode.Create(
                request.Code);

            commodity = Commodity.Create(
                organizationId,
                code,
                request.Name,
                request.CommodityCategoryId,
                request.ScientificName,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.Validation(
                    exception.Message));
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.OrganizationNotFound(
                    organizationId));
        }

        var categoryExists =
            await CategoryExistsAsync(
                organizationId,
                commodity.CommodityCategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.CategoryNotFound(
                    organizationId,
                    commodity.CommodityCategoryId));
        }

        var codeExists =
            await _commodityRepository.CodeExistsAsync(
                organizationId,
                code,
                cancellationToken);

        if (codeExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.CodeAlreadyExists(
                    code.Value));
        }

        _commodityRepository.Add(commodity);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CommodityResponse>.Success(
            commodity.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CommodityResponse>>>
        GetAllAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default)
    {
        var organizationError =
            await GetOrganizationErrorAsync(
                organizationId,
                cancellationToken);

        if (organizationError is not null)
        {
            return Result<
                IReadOnlyList<CommodityResponse>>
                .Failure(organizationError);
        }

        var commodities =
            await _commodityRepository.GetAllAsync(
                organizationId,
                cancellationToken);

        IReadOnlyList<CommodityResponse> responses =
            commodities
                .Select(commodity =>
                    commodity.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CommodityResponse>>
            .Success(responses);
    }

    public async Task<Result<CommodityResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            commodityId);

        if (identifierError is not null)
        {
            return Result<CommodityResponse>.Failure(
                identifierError);
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.OrganizationNotFound(
                    organizationId));
        }

        var commodity =
            await _commodityRepository.GetByIdAsync(
                organizationId,
                commodityId,
                cancellationToken);

        if (commodity is null)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.NotFound(
                    organizationId,
                    commodityId));
        }

        return Result<CommodityResponse>.Success(
            commodity.ToResponse());
    }

    public async Task<Result<CommodityResponse>>
        UpdateAsync(
            Guid organizationId,
            Guid commodityId,
            UpdateCommodityRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            commodityId);

        if (identifierError is not null)
        {
            return Result<CommodityResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.Validation(
                    "Commodity request cannot be null."));
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.OrganizationNotFound(
                    organizationId));
        }

        var commodity =
            await _commodityRepository.GetByIdForUpdateAsync(
                organizationId,
                commodityId,
                cancellationToken);

        if (commodity is null)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.NotFound(
                    organizationId,
                    commodityId));
        }

        var categoryExists =
            await CategoryExistsAsync(
                organizationId,
                request.CommodityCategoryId,
                cancellationToken);

        if (!categoryExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.CategoryNotFound(
                    organizationId,
                    request.CommodityCategoryId));
        }

        var normalizedName =
            request.Name?.Trim() ?? string.Empty;

        var previousName = commodity.Name;

        var previousCategoryId =
            commodity.CommodityCategoryId;

        var previousScientificName =
            commodity.ScientificName;

        var previousDescription =
            commodity.Description;

        try
        {
            commodity.Update(
                normalizedName,
                request.CommodityCategoryId,
                request.ScientificName,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.Validation(
                    exception.Message));
        }

        var hasChanged =
            previousName != commodity.Name ||
            previousCategoryId !=
                commodity.CommodityCategoryId ||
            previousScientificName !=
                commodity.ScientificName ||
            previousDescription !=
                commodity.Description;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CommodityResponse>.Success(
            commodity.ToResponse());
    }

    public Task<Result<CommodityResponse>>
        ActivateAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            commodityId,
            true,
            cancellationToken);
    }

    public Task<Result<CommodityResponse>>
        DeactivateAsync(
            Guid organizationId,
            Guid commodityId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            commodityId,
            false,
            cancellationToken);
    }

    private async Task<Result<CommodityResponse>>
        SetActiveStatusAsync(
            Guid organizationId,
            Guid commodityId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            commodityId);

        if (identifierError is not null)
        {
            return Result<CommodityResponse>.Failure(
                identifierError);
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.OrganizationNotFound(
                    organizationId));
        }

        var commodity =
            await _commodityRepository.GetByIdForUpdateAsync(
                organizationId,
                commodityId,
                cancellationToken);

        if (commodity is null)
        {
            return Result<CommodityResponse>.Failure(
                CommodityErrors.NotFound(
                    organizationId,
                    commodityId));
        }

        var previousStatus = commodity.IsActive;

        if (shouldBeActive)
        {
            commodity.Activate();
        }
        else
        {
            commodity.Deactivate();
        }

        if (previousStatus != commodity.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CommodityResponse>.Success(
            commodity.ToResponse());
    }

    private async Task<Error?> GetOrganizationErrorAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var validationError =
            ValidateOrganizationId(organizationId);

        if (validationError is not null)
        {
            return validationError;
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        return organizationExists
            ? null
            : CommodityErrors.OrganizationNotFound(
                organizationId);
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

    private async Task<bool> CategoryExistsAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            return false;
        }

        var category =
            await _categoryRepository.GetByIdAsync(
                organizationId,
                categoryId,
                cancellationToken);

        return category is not null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid commodityId)
    {
        var organizationError =
            ValidateOrganizationId(organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (commodityId == Guid.Empty)
        {
            return CommodityErrors.Validation(
                "Commodity identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return CommodityErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }
}
