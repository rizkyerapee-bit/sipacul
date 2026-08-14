using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Previews.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Previews.Services;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Endpoints.Finance.ProfitSharing.Previews;

public static class ProfitSharingPreviewEndpoints
{
    public static RouteGroupBuilder MapProfitSharingPreviewEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/profit-sharing-preview")
            .WithTags("Profit Sharing Previews");

        group.MapGet(string.Empty, GetAsync)
            .WithName("ProfitSharingPreviews.Get")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<ProfitSharingPreviewResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid cropCycleId,
        IProfitSharingPreviewService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        return result.ToHttpResult(preview => Results.Ok(preview));
    }
}
