using SiPacul.Api.Common.Http;
using SiPacul.Application.MasterData.CommodityCategories.Contracts;
using SiPacul.Application.MasterData.CommodityCategories.Services;

namespace SiPacul.Api.Endpoints.MasterData.CommodityCategories;

public static class CommodityCategoryEndpoints
{
    private const string GetByIdRouteName =
        "CommodityCategories.GetById";

    public static RouteGroupBuilder
        MapCommodityCategoryEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/commodity-categories")
            .WithTags("Commodity Categories");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("CommodityCategories.Create")
            .Produces<CommodityCategoryResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(
                string.Empty,
                GetAllAsync)
            .WithName("CommodityCategories.GetAll")
            .Produces<
                IReadOnlyList<CommodityCategoryResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{categoryId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<CommodityCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{categoryId:guid}",
                UpdateAsync)
            .WithName("CommodityCategories.Update")
            .Produces<CommodityCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{categoryId:guid}/activate",
                ActivateAsync)
            .WithName("CommodityCategories.Activate")
            .Produces<CommodityCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{categoryId:guid}/deactivate",
                DeactivateAsync)
            .WithName("CommodityCategories.Deactivate")
            .Produces<CommodityCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateCommodityCategoryRequest request,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.CreateAsync(
                organizationId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            category =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        categoryId = category.Id
                    },
                    category));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.GetAllAsync(
                organizationId,
                cancellationToken);

        return result.ToHttpResult(
            categories =>
                Results.Ok(categories));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid categoryId,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.GetByIdAsync(
                organizationId,
                categoryId,
                cancellationToken);

        return result.ToHttpResult(
            category =>
                Results.Ok(category));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid categoryId,
        UpdateCommodityCategoryRequest request,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.UpdateAsync(
                organizationId,
                categoryId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            category =>
                Results.Ok(category));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid categoryId,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.ActivateAsync(
                organizationId,
                categoryId,
                cancellationToken);

        return result.ToHttpResult(
            category =>
                Results.Ok(category));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid organizationId,
        Guid categoryId,
        ICommodityCategoryService categoryService,
        CancellationToken cancellationToken)
    {
        var result =
            await categoryService.DeactivateAsync(
                organizationId,
                categoryId,
                cancellationToken);

        return result.ToHttpResult(
            category =>
                Results.Ok(category));
    }
}
