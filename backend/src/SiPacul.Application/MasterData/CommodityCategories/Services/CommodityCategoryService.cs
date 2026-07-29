using SiPacul.Application.Common.Persistence;
using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Application.MasterData.CommodityCategories.Mappings;
using SiPacul.Application.MasterData.CommodityCategories.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.MasterData;
using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.CommodityCategories.Services;

public sealed class CommodityCategoryService :
    ICommodityCategoryService
{
    private readonly ICommodityCategoryRepository
        _categoryRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CommodityCategoryService(
        ICommodityCategoryRepository categoryRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CommodityCategoryResponse>>
        CreateAsync(
            Guid organizationId,
            CreateCommodityCategoryRequest request,
            CancellationToken cancellationToken = default)
    {
        var validationError = ValidateOrganizationId(
            organizationId);

        if (validationError is not null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                validationError);
        }

        if (request is null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.Validation(
                    "Commodity category request cannot be null."));
        }

        CommodityCategory category;

        try
        {
            category = CommodityCategory.Create(
                organizationId,
                request.Name,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.Validation(
                    exception.Message));
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.OrganizationNotFound(
                    organizationId));
        }

        var nameExists =
            await _categoryRepository.NameExistsAsync(
                organizationId,
                category.Name,
                cancellationToken: cancellationToken);

        if (nameExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NameAlreadyExists(
                    category.Name));
        }

        _categoryRepository.Add(category);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CommodityCategoryResponse>.Success(
            category.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CommodityCategoryResponse>>>
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
                IReadOnlyList<CommodityCategoryResponse>>
                .Failure(organizationError);
        }

        var categories =
            await _categoryRepository.GetAllAsync(
                organizationId,
                cancellationToken);

        IReadOnlyList<CommodityCategoryResponse> responses =
            categories
                .Select(category =>
                    category.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CommodityCategoryResponse>>
            .Success(responses);
    }

    public async Task<Result<CommodityCategoryResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            categoryId);

        if (identifierError is not null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                identifierError);
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.OrganizationNotFound(
                    organizationId));
        }

        var category =
            await _categoryRepository.GetByIdAsync(
                organizationId,
                categoryId,
                cancellationToken);

        if (category is null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NotFound(
                    organizationId,
                    categoryId));
        }

        return Result<CommodityCategoryResponse>.Success(
            category.ToResponse());
    }

    public async Task<Result<CommodityCategoryResponse>>
        UpdateAsync(
            Guid organizationId,
            Guid categoryId,
            UpdateCommodityCategoryRequest request,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            categoryId);

        if (identifierError is not null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                identifierError);
        }

        if (request is null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.Validation(
                    "Commodity category request cannot be null."));
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.OrganizationNotFound(
                    organizationId));
        }

        var category =
            await _categoryRepository.GetByIdForUpdateAsync(
                organizationId,
                categoryId,
                cancellationToken);

        if (category is null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NotFound(
                    organizationId,
                    categoryId));
        }

        var normalizedRequestedName =
            request.Name?.Trim() ?? string.Empty;

        var nameExists =
            await _categoryRepository.NameExistsAsync(
                organizationId,
                normalizedRequestedName,
                categoryId,
                cancellationToken);

        if (nameExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NameAlreadyExists(
                    normalizedRequestedName));
        }

        var previousName = category.Name;
        var previousDescription = category.Description;

        try
        {
            category.Update(
                normalizedRequestedName,
                request.Description);
        }
        catch (ArgumentException exception)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.Validation(
                    exception.Message));
        }

        var hasChanged =
            previousName != category.Name ||
            previousDescription != category.Description;

        if (hasChanged)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CommodityCategoryResponse>.Success(
            category.ToResponse());
    }

    public Task<Result<CommodityCategoryResponse>>
        ActivateAsync(
            Guid organizationId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            categoryId,
            true,
            cancellationToken);
    }

    public Task<Result<CommodityCategoryResponse>>
        DeactivateAsync(
            Guid organizationId,
            Guid categoryId,
            CancellationToken cancellationToken = default)
    {
        return SetActiveStatusAsync(
            organizationId,
            categoryId,
            false,
            cancellationToken);
    }

    private async Task<Result<CommodityCategoryResponse>>
        SetActiveStatusAsync(
            Guid organizationId,
            Guid categoryId,
            bool shouldBeActive,
            CancellationToken cancellationToken)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            categoryId);

        if (identifierError is not null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                identifierError);
        }

        var organizationExists =
            await OrganizationExistsAsync(
                organizationId,
                cancellationToken);

        if (!organizationExists)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.OrganizationNotFound(
                    organizationId));
        }

        var category =
            await _categoryRepository.GetByIdForUpdateAsync(
                organizationId,
                categoryId,
                cancellationToken);

        if (category is null)
        {
            return Result<CommodityCategoryResponse>.Failure(
                CommodityCategoryErrors.NotFound(
                    organizationId,
                    categoryId));
        }

        var previousStatus = category.IsActive;

        if (shouldBeActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        if (previousStatus != category.IsActive)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<CommodityCategoryResponse>.Success(
            category.ToResponse());
    }

    private async Task<Error?> GetOrganizationErrorAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateOrganizationId(
            organizationId);

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
            : CommodityCategoryErrors.OrganizationNotFound(
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

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid categoryId)
    {
        var organizationError = ValidateOrganizationId(
            organizationId);

        if (organizationError is not null)
        {
            return organizationError;
        }

        if (categoryId == Guid.Empty)
        {
            return CommodityCategoryErrors.Validation(
                "Commodity category identifier cannot be empty.");
        }

        return null;
    }

    private static Error? ValidateOrganizationId(
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            return CommodityCategoryErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        return null;
    }
}
