using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Services;
using SiPacul.Domain.Entities.Finance.ProfitSharing;

namespace SiPacul.Api.Endpoints.Finance.ProfitSharing;

public static class ProfitSharingSettlementEndpoints
{
    private const string GetByIdRouteName =
        "ProfitSharingSettlements.GetById";

    public static RouteGroupBuilder
        MapProfitSharingSettlementEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/" +
                "profit-sharing-settlements")
            .WithTags("Profit Sharing Settlements");

        group.MapPost(string.Empty, CreateDraftAsync)
            .WithName(
                "ProfitSharingSettlements.CreateDraft")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSettlementResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName(
                "ProfitSharingSettlements.GetAll")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<
                IReadOnlyList<
                    ProfitSharingSettlementResponse>>(
                        StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{settlementId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<ProfitSharingSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{settlementId:guid}",
                UpdateDraftAsync)
            .WithName(
                "ProfitSharingSettlements.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{settlementId:guid}/finalize",
                FinalizeAsync)
            .WithName(
                "ProfitSharingSettlements.Finalize")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingFinalize)
            .Produces<ProfitSharingSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{settlementId:guid}/void",
                VoidAsync)
            .WithName(
                "ProfitSharingSettlements.Void")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingVoid)
            .Produces<ProfitSharingSettlementResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> CreateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        CreateProfitSharingSettlementRequest request,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateDraftAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            settlement =>
                Results.CreatedAtRoute(
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
        ProfitSharingSettlementStatus? status,
        DateOnly? settlementDateFrom,
        DateOnly? settlementDateTo,
        string? managingPartnerCode,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var filter =
            new ProfitSharingSettlementFilter(
                status,
                settlementDateFrom,
                settlementDateTo,
                managingPartnerCode);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            settlements =>
                Results.Ok(settlements));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            settlementId,
            cancellationToken);

        return result.ToHttpResult(
            settlement =>
                Results.Ok(settlement));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        UpdateProfitSharingSettlementRequest request,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            cropCycleId,
            settlementId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            settlement =>
                Results.Ok(settlement));
    }

    private static async Task<IResult> FinalizeAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.FinalizeAsync(
            organizationId,
            cropCycleId,
            settlementId,
            cancellationToken);

        return result.ToHttpResult(
            settlement =>
                Results.Ok(settlement));
    }

    private static async Task<IResult> VoidAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid settlementId,
        VoidProfitSharingSettlementRequest request,
        IProfitSharingSettlementService service,
        CancellationToken cancellationToken)
    {
        var result = await service.VoidAsync(
            organizationId,
            cropCycleId,
            settlementId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            settlement =>
                Results.Ok(settlement));
    }
}
