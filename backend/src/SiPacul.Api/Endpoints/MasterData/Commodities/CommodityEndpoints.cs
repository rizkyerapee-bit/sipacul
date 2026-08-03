using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.MasterData.Commodities.Contracts;
using SiPacul.Application.MasterData.Commodities.Services;

namespace SiPacul.Api.Endpoints.MasterData.Commodities;

public static class CommodityEndpoints
{
    private const string GetByIdRouteName =
        "Commodities.GetById";

    public static RouteGroupBuilder MapCommodityEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/commodities")
            .WithTags("Commodities");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("Commodities.Create")
            .Produces<CommodityResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict)
            .RequireOrganizationPermission(
                Permissions.MasterDataWrite);

        group.MapGet(
                string.Empty,
                GetAllAsync)
            .WithName("Commodities.GetAll")
            .Produces<IReadOnlyList<CommodityResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MasterDataRead);

        group.MapGet(
                "/{commodityId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<CommodityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MasterDataRead);

        group.MapPut(
                "/{commodityId:guid}",
                UpdateAsync)
            .WithName("Commodities.Update")
            .Produces<CommodityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MasterDataWrite);

        group.MapPatch(
                "/{commodityId:guid}/activate",
                ActivateAsync)
            .WithName("Commodities.Activate")
            .Produces<CommodityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MasterDataWrite);

        group.MapPatch(
                "/{commodityId:guid}/deactivate",
                DeactivateAsync)
            .WithName("Commodities.Deactivate")
            .Produces<CommodityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MasterDataWrite);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateCommodityRequest request,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.CreateAsync(
                organizationId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            commodity =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        commodityId = commodity.Id
                    },
                    commodity));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.GetAllAsync(
                organizationId,
                cancellationToken);

        return result.ToHttpResult(
            commodities =>
                Results.Ok(commodities));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid commodityId,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.GetByIdAsync(
                organizationId,
                commodityId,
                cancellationToken);

        return result.ToHttpResult(
            commodity =>
                Results.Ok(commodity));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid commodityId,
        UpdateCommodityRequest request,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.UpdateAsync(
                organizationId,
                commodityId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            commodity =>
                Results.Ok(commodity));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid commodityId,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.ActivateAsync(
                organizationId,
                commodityId,
                cancellationToken);

        return result.ToHttpResult(
            commodity =>
                Results.Ok(commodity));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid organizationId,
        Guid commodityId,
        ICommodityService commodityService,
        CancellationToken cancellationToken)
    {
        var result =
            await commodityService.DeactivateAsync(
                organizationId,
                commodityId,
                cancellationToken);

        return result.ToHttpResult(
            commodity =>
                Results.Ok(commodity));
    }
}
