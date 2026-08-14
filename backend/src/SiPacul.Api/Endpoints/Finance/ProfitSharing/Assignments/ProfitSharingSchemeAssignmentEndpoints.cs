using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Assignments.Services;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Endpoints.Finance.ProfitSharing.Assignments;

public static class ProfitSharingSchemeAssignmentEndpoints
{
    public static RouteGroupBuilder
        MapProfitSharingSchemeAssignmentEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/profit-sharing-scheme")
            .WithTags("Profit Sharing Scheme Assignments");

        group.MapGet(string.Empty, GetAsync)
            .WithName("ProfitSharingSchemeAssignments.Get")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<ProfitSharingSchemeAssignmentResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut(string.Empty, AssignAsync)
            .WithName("ProfitSharingSchemeAssignments.Assign")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSchemeAssignmentResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        IProfitSharingSchemeAssignmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        return result.ToHttpResult(
            assignment => Results.Ok(assignment));
    }

    private static async Task<IResult> AssignAsync(
        Guid organizationId,
        Guid cropCycleId,
        AssignProfitSharingSchemeRequest request,
        IProfitSharingSchemeAssignmentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AssignAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            assignment => Results.Ok(assignment));
    }
}
