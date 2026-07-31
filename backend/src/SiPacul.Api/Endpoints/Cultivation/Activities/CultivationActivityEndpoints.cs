using SiPacul.Api.Common.Http;
using SiPacul.Application.Cultivation.Activities.Contracts;
using SiPacul.Application.Cultivation.Activities.Services;
using SiPacul.Domain.Entities.Cultivation;

namespace SiPacul.Api.Endpoints.Cultivation.Activities;

public static class CultivationActivityEndpoints
{
    private const string GetByIdRouteName =
        "CultivationActivities.GetById";

    public static RouteGroupBuilder
        MapCultivationActivityEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/activities")
            .WithTags("Cultivation Activities");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("CultivationActivities.Create")
            .Produces<CultivationActivityResponse>(
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
            .WithName("CultivationActivities.GetAll")
            .Produces<
                IReadOnlyList<CultivationActivityResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{activityId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{activityId:guid}",
                UpdatePlanAsync)
            .WithName(
                "CultivationActivities.UpdatePlan")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{activityId:guid}/start",
                StartAsync)
            .WithName("CultivationActivities.Start")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{activityId:guid}/complete",
                CompleteAsync)
            .WithName("CultivationActivities.Complete")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{activityId:guid}/cancel",
                CancelAsync)
            .WithName("CultivationActivities.Cancel")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{activityId:guid}/notes",
                UpdateNotesAsync)
            .WithName(
                "CultivationActivities.UpdateNotes")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPost(
                "/{activityId:guid}/resources",
                AddResourceAsync)
            .WithName(
                "CultivationActivities.AddResource")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPut(
                "/{activityId:guid}/resources/" +
                "{resourceId:guid}",
                UpdateResourceAsync)
            .WithName(
                "CultivationActivities.UpdateResource")
            .Produces<CultivationActivityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapDelete(
                "/{activityId:guid}/resources/" +
                "{resourceId:guid}",
                RemoveResourceAsync)
            .WithName(
                "CultivationActivities.RemoveResource")
            .Produces<CultivationActivityResponse>(
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
        Guid cropCycleId,
        CreateCultivationActivityRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cropCycleId,
                        activityId = activity.Id
                    },
                    activity));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CultivationActivityStatus? status,
        CultivationActivityType? activityType,
        DateOnly? plannedFrom,
        DateOnly? plannedTo,
        Guid? cultivationSopStepId,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var filter = new CultivationActivityFilter(
            status,
            activityType,
            plannedFrom,
            plannedTo,
            cultivationSopStepId);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            activities => Results.Ok(activities));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            activityId,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> UpdatePlanAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        UpdateCultivationActivityPlanRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdatePlanAsync(
            organizationId,
            cropCycleId,
            activityId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> StartAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        StartCultivationActivityRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.StartAsync(
            organizationId,
            cropCycleId,
            activityId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> CompleteAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CompleteCultivationActivityRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CompleteAsync(
            organizationId,
            cropCycleId,
            activityId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        CancelCultivationActivityRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            cropCycleId,
            activityId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> UpdateNotesAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        UpdateCultivationActivityNotesRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result =
            await service.UpdateExecutionNotesAsync(
                organizationId,
                cropCycleId,
                activityId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> AddResourceAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        AddCultivationActivityResourceRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AddResourceAsync(
            organizationId,
            cropCycleId,
            activityId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> UpdateResourceAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        Guid resourceId,
        UpdateCultivationActivityResourceRequest request,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateResourceAsync(
            organizationId,
            cropCycleId,
            activityId,
            resourceId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }

    private static async Task<IResult> RemoveResourceAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid activityId,
        Guid resourceId,
        ICultivationActivityService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveResourceAsync(
            organizationId,
            cropCycleId,
            activityId,
            resourceId,
            cancellationToken);

        return result.ToHttpResult(
            activity => Results.Ok(activity));
    }
}
