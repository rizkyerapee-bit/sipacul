using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Evaluations.SeasonHistories.Contracts;
using SiPacul.Application.Evaluations.SeasonHistories.Services;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Endpoints.Evaluations.SeasonHistories;

public static class SeasonHistoryEndpoints
{
    public static RouteGroupBuilder MapSeasonHistoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/lands/" +
                "{landId:guid}/season-history")
            .WithTags("Season Histories");

        group.MapGet(
                string.Empty,
                GetAsync)
            .WithName("SeasonHistories.Get")
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<LandSeasonHistoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status500InternalServerError);

        return group;
    }

    private static async Task<IResult> GetAsync(
        Guid organizationId,
        Guid landId,
        Guid? landPlotId,
        bool? includeNonTerminal,
        int? page,
        int? pageSize,
        ISeasonHistoryService service,
        CancellationToken cancellationToken)
    {
        var filter = new SeasonHistoryFilter(
            landPlotId,
            includeNonTerminal ?? false,
            page ?? 1,
            pageSize ?? 20);

        var result = await service.GetAsync(
            organizationId,
            landId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            history => Results.Ok(history));
    }
}
