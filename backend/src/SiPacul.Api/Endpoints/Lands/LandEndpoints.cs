using SiPacul.Api.Common.Http;
using SiPacul.Application.Lands.Contracts;
using SiPacul.Application.Lands.Services;

namespace SiPacul.Api.Endpoints.Lands;

public static class LandEndpoints
{
    private const string GetByIdRouteName =
        "Lands.GetById";

    public static RouteGroupBuilder MapLandEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/lands")
            .WithTags("Lands");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("Lands.Create")
            .Produces<LandResponse>(
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
            .WithName("Lands.GetAll")
            .Produces<IReadOnlyList<LandResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{landId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{landId:guid}",
                UpdateAsync)
            .WithName("Lands.Update")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{landId:guid}/activate",
                ActivateAsync)
            .WithName("Lands.Activate")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{landId:guid}/deactivate",
                DeactivateAsync)
            .WithName("Lands.Deactivate")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPost(
                "/{landId:guid}/plots",
                AddPlotAsync)
            .WithName("Lands.AddPlot")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPut(
                "/{landId:guid}/plots/{plotId:guid}",
                UpdatePlotAsync)
            .WithName("Lands.UpdatePlot")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapDelete(
                "/{landId:guid}/plots/{plotId:guid}",
                RemovePlotAsync)
            .WithName("Lands.RemovePlot")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{landId:guid}/plots/" +
                "{plotId:guid}/activate",
                ActivatePlotAsync)
            .WithName("Lands.ActivatePlot")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{landId:guid}/plots/" +
                "{plotId:guid}/deactivate",
                DeactivatePlotAsync)
            .WithName("Lands.DeactivatePlot")
            .Produces<LandResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateLandRequest request,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            land =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        landId = land.Id
                    },
                    land));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(
            organizationId,
            cancellationToken);

        return result.ToHttpResult(
            lands => Results.Ok(lands));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid landId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            landId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        Guid landId,
        UpdateLandRequest request,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(
            organizationId,
            landId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid landId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(
            organizationId,
            landId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid organizationId,
        Guid landId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeactivateAsync(
            organizationId,
            landId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> AddPlotAsync(
        Guid organizationId,
        Guid landId,
        AddLandPlotRequest request,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AddPlotAsync(
            organizationId,
            landId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> UpdatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        UpdateLandPlotRequest request,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdatePlotAsync(
            organizationId,
            landId,
            plotId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> RemovePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemovePlotAsync(
            organizationId,
            landId,
            plotId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> ActivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ActivatePlotAsync(
            organizationId,
            landId,
            plotId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }

    private static async Task<IResult> DeactivatePlotAsync(
        Guid organizationId,
        Guid landId,
        Guid plotId,
        ILandService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeactivatePlotAsync(
            organizationId,
            landId,
            plotId,
            cancellationToken);

        return result.ToHttpResult(
            land => Results.Ok(land));
    }
}
