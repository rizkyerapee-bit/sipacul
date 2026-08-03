using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Cultivation.CropCycles.Contracts;
using SiPacul.Application.Cultivation.CropCycles.Services;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Api.Endpoints.Cultivation.CropCycles;

public static class CropCycleEndpoints
{
    private const string GetByIdRouteName =
        "CropCycles.GetById";

    public static RouteGroupBuilder MapCropCycleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles")
            .WithTags("Crop Cycles");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("CropCycles.Create")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
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
            .WithName("CropCycles.GetAll")
            .RequireOrganizationPermission(
                Permissions.CultivationRead)
            .Produces<IReadOnlyList<CropCycleResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{cropCycleId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.CultivationRead)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{cropCycleId:guid}",
                UpdatePlanAsync)
            .WithName("CropCycles.UpdatePlan")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{cropCycleId:guid}/start",
                StartAsync)
            .WithName("CropCycles.Start")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{cropCycleId:guid}/complete",
                CompleteAsync)
            .WithName("CropCycles.Complete")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{cropCycleId:guid}/cancel",
                CancelAsync)
            .WithName("CropCycles.Cancel")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{cropCycleId:guid}/notes",
                UpdateNotesAsync)
            .WithName("CropCycles.UpdateNotes")
            .RequireOrganizationPermission(
                Permissions.CultivationWrite)
            .Produces<CropCycleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateCropCycleRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cropCycleId = cropCycle.Id
                    },
                    cropCycle));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        CropCycleStatus? status,
        Guid? commodityId,
        Guid? landId,
        Guid? landPlotId,
        DateOnly? plannedStartFrom,
        DateOnly? plannedStartTo,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var filter = new CropCycleFilter(
            status,
            commodityId,
            landId,
            landPlotId,
            plannedStartFrom,
            plannedStartTo);

        var result = await service.GetAllAsync(
            organizationId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            cropCycles => Results.Ok(cropCycles));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }

    private static async Task<IResult> UpdatePlanAsync(
        Guid organizationId,
        Guid cropCycleId,
        UpdateCropCyclePlanRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdatePlanAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }

    private static async Task<IResult> StartAsync(
        Guid organizationId,
        Guid cropCycleId,
        StartCropCycleRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }

    private static async Task<IResult> CompleteAsync(
        Guid organizationId,
        Guid cropCycleId,
        CompleteCropCycleRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancelCropCycleRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }

    private static async Task<IResult> UpdateNotesAsync(
        Guid organizationId,
        Guid cropCycleId,
        UpdateCropCycleNotesRequest request,
        ICropCycleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateNotesAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            cropCycle => Results.Ok(cropCycle));
    }
}
