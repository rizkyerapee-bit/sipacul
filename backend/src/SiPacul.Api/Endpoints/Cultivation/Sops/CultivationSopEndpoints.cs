using SiPacul.Api.Common.Http;
using SiPacul.Application.Cultivation.Sops.Contracts;
using SiPacul.Application.Cultivation.Sops.Services;

namespace SiPacul.Api.Endpoints.Cultivation.Sops;

public static class CultivationSopEndpoints
{
    private const string GetByIdRouteName =
        "CultivationSops.GetById";

    public static RouteGroupBuilder MapCultivationSopEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/cultivation-sops")
            .WithTags("Cultivation SOPs");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("CultivationSops.Create")
            .Produces<CultivationSopResponse>(
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
            .WithName("CultivationSops.GetAll")
            .Produces<IReadOnlyList<CultivationSopResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{cultivationSopId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{cultivationSopId:guid}",
                UpdateAsync)
            .WithName("CultivationSops.Update")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{cultivationSopId:guid}/activate",
                ActivateAsync)
            .WithName("CultivationSops.Activate")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{cultivationSopId:guid}/deactivate",
                DeactivateAsync)
            .WithName("CultivationSops.Deactivate")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPost(
                "/{cultivationSopId:guid}/steps",
                AddStepAsync)
            .WithName("CultivationSops.AddStep")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{cultivationSopId:guid}/steps/" +
                "{stepId:guid}",
                UpdateStepAsync)
            .WithName("CultivationSops.UpdateStep")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapDelete(
                "/{cultivationSopId:guid}/steps/" +
                "{stepId:guid}",
                RemoveStepAsync)
            .WithName("CultivationSops.RemoveStep")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{cultivationSopId:guid}/steps/" +
                "{stepId:guid}/move",
                MoveStepAsync)
            .WithName("CultivationSops.MoveStep")
            .Produces<CultivationSopResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateCultivationSopRequest request,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cultivationSopId =
                            cultivationSop.Id
                    },
                    cultivationSop));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid? commodityId,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(
            organizationId,
            commodityId,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSops =>
                Results.Ok(cultivationSops));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cultivationSopId,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cultivationSopId,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        UpdateCultivationSopRequest request,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            organizationId,
            cultivationSopId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(
            organizationId,
            cultivationSopId,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid organizationId,
        Guid cultivationSopId,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(
            organizationId,
            cultivationSopId,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> AddStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        AddCultivationSopStepRequest request,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AddStepAsync(
            organizationId,
            cultivationSopId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> UpdateStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        UpdateCultivationSopStepRequest request,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateStepAsync(
            organizationId,
            cultivationSopId,
            stepId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> RemoveStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveStepAsync(
            organizationId,
            cultivationSopId,
            stepId,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }

    private static async Task<IResult> MoveStepAsync(
        Guid organizationId,
        Guid cultivationSopId,
        Guid stepId,
        MoveCultivationSopStepRequest request,
        ICultivationSopService service,
        CancellationToken cancellationToken)
    {
        var result = await service.MoveStepAsync(
            organizationId,
            cultivationSopId,
            stepId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cultivationSop =>
                Results.Ok(cultivationSop));
    }
}
