using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Harvests.Contracts;
using SiPacul.Application.Harvests.Services;
using SiPacul.Domain.Entities.Harvests;

namespace SiPacul.Api.Endpoints.Harvests;

public static class HarvestBatchEndpoints
{
    private const string GetByIdRouteName =
        "HarvestBatches.GetById";

    public static RouteGroupBuilder
        MapHarvestBatchEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/harvest-batches")
            .WithTags("Harvest Batches");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("HarvestBatches.Create")
            .RequireOrganizationPermission(
                Permissions.HarvestWrite)
            .Produces<HarvestBatchResponse>(
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
            .WithName("HarvestBatches.GetAll")
            .RequireOrganizationPermission(
                Permissions.HarvestRead)
            .Produces<
                IReadOnlyList<HarvestBatchResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{harvestBatchId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.HarvestRead)
            .Produces<HarvestBatchResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{harvestBatchId:guid}",
                UpdateDraftAsync)
            .WithName("HarvestBatches.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.HarvestWrite)
            .Produces<HarvestBatchResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{harvestBatchId:guid}/confirm",
                ConfirmAsync)
            .WithName("HarvestBatches.Confirm")
            .RequireOrganizationPermission(
                Permissions.HarvestWrite)
            .Produces<HarvestBatchResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{harvestBatchId:guid}/cancel",
                CancelAsync)
            .WithName("HarvestBatches.Cancel")
            .RequireOrganizationPermission(
                Permissions.HarvestWrite)
            .Produces<HarvestBatchResponse>(
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
        CreateHarvestBatchRequest request,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatch =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cropCycleId,
                        harvestBatchId =
                            harvestBatch.Id
                    },
                    harvestBatch));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        HarvestBatchStatus? status,
        DateOnly? harvestDateFrom,
        DateOnly? harvestDateTo,
        HarvestQuantityUnit? quantityUnit,
        string? qualityGrade,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var filter = new HarvestBatchFilter(
            status,
            harvestDateFrom,
            harvestDateTo,
            quantityUnit,
            qualityGrade);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatches =>
                Results.Ok(harvestBatches));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            harvestBatchId,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatch =>
                Results.Ok(harvestBatch));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        UpdateHarvestBatchRequest request,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            cropCycleId,
            harvestBatchId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatch =>
                Results.Ok(harvestBatch));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(
            organizationId,
            cropCycleId,
            harvestBatchId,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatch =>
                Results.Ok(harvestBatch));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid harvestBatchId,
        CancelHarvestBatchRequest request,
        IHarvestBatchService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            cropCycleId,
            harvestBatchId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            harvestBatch =>
                Results.Ok(harvestBatch));
    }
}
