using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Contracts;
using SiPacul.Application.Finance.ProfitSharing.WaterfallSettlements.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Settlements;

namespace SiPacul.Api.Endpoints.Finance.ProfitSharing.WaterfallSettlements;

public static class ProfitSharingWaterfallSettlementEndpoints
{
    private const string GetByIdRouteName =
        "ProfitSharingWaterfallSettlements.GetById";

    public static RouteGroupBuilder
        MapProfitSharingWaterfallSettlementEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/" +
                "profit-sharing-waterfall-settlements")
            .WithTags("Profit Sharing Waterfall Settlements");

        group.MapPost(string.Empty, FinalizeAsync)
            .WithName("ProfitSharingWaterfallSettlements.Finalize")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingFinalize)
            .Produces<ProfitSharingWaterfallSettlementResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("ProfitSharingWaterfallSettlements.GetAll")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<
                IReadOnlyList<ProfitSharingWaterfallSettlementResponse>>(
                    StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{settlementId:guid}", GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<ProfitSharingWaterfallSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{settlementId:guid}/void", VoidAsync)
            .WithName("ProfitSharingWaterfallSettlements.Void")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingVoid)
            .Produces<ProfitSharingWaterfallSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> FinalizeAsync(
        Guid organizationId,
        Guid cropCycleId,
        FinalizeProfitSharingWaterfallSettlementRequest request,
        IProfitSharingWaterfallSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.FinalizeAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            settlement => Results.CreatedAtRoute(
                GetByIdRouteName,
                new
                {
                    organizationId,
                    cropCycleId,
                    settlementId = settlement.Id
                },
                settlement));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        ProfitSharingWaterfallSettlementStatus? status,
        DateOnly? settlementDateFrom,
        DateOnly? settlementDateTo,
        IProfitSharingWaterfallSettlementService service,
        CancellationToken cancellationToken)
    {
        var filter = new ProfitSharingWaterfallSettlementFilter(
            status,
            settlementDateFrom,
            settlementDateTo);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            settlements => Results.Ok(settlements));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        IProfitSharingWaterfallSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            settlementId,
            cancellationToken);

        return result.ToHttpResult(
            settlement => Results.Ok(settlement));
    }

    private static async Task<IResult> VoidAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        VoidProfitSharingWaterfallSettlementRequest request,
        IProfitSharingWaterfallSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.VoidAsync(
            organizationId,
            cropCycleId,
            settlementId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            settlement => Results.Ok(settlement));
    }
}
