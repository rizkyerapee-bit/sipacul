using SiPacul.Api.Common.Http;
using SiPacul.Application.Finance.Profitability.Contracts;
using SiPacul.Application.Finance.Profitability.Services;

namespace SiPacul.Api.Endpoints.Finance.Profitability;

public static class ProfitabilityEndpoints
{
    public static RouteGroupBuilder
        MapProfitabilityEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/profitability")
            .WithTags("Profitability");

        group.MapGet(
                string.Empty,
                GetCropCycleReportAsync)
            .WithName(
                "Profitability.GetCropCycleReport")
            .Produces<CropCycleProfitabilityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult>
        GetCropCycleReportAsync(
            Guid organizationId,
            Guid cropCycleId,
            IProfitabilityService service,
            CancellationToken cancellationToken)
    {
        var result =
            await service.GetCropCycleReportAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        return result.ToHttpResult(
            report => Results.Ok(report));
    }
}
