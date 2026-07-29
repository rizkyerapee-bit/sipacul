using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Shared.Results;

namespace SiPacul.Application.MasterData.CommodityCategories.Services;

public interface ICommodityCategoryService
{
    Task<Result<CommodityCategoryResponse>> CreateAsync(
        Guid organizationId,
        CreateCommodityCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<CommodityCategoryResponse>>> GetAllAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityCategoryResponse>> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityCategoryResponse>> UpdateAsync(
        Guid organizationId,
        Guid categoryId,
        UpdateCommodityCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityCategoryResponse>> ActivateAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<Result<CommodityCategoryResponse>> DeactivateAsync(
        Guid organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default);
}
