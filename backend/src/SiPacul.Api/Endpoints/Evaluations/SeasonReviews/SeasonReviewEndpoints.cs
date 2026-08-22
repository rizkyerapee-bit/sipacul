using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Evaluations.SeasonReviews.Contracts;
using SiPacul.Application.Evaluations.SeasonReviews.Services;
using SiPacul.Application.Security.Authorization;

namespace SiPacul.Api.Endpoints.Evaluations.SeasonReviews;

public static class SeasonReviewEndpoints
{
    private const string GetByIdRouteName = "SeasonReviews.GetById";

    public static RouteGroupBuilder MapSeasonReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/organizations/{organizationId:guid}/season-reviews")
            .WithTags("Season Reviews");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("SeasonReviews.Create")
            .RequireOrganizationPermission(Permissions.CultivationWrite)
            .Produces<SeasonReviewResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{reviewId:guid}", GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(Permissions.CultivationRead)
            .Produces<SeasonReviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/by-crop-cycle/{cropCycleId:guid}", GetByCropCycleAsync)
            .WithName("SeasonReviews.GetByCropCycle")
            .RequireOrganizationPermission(Permissions.CultivationRead)
            .Produces<SeasonReviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{reviewId:guid}", UpdateAsync)
            .WithName("SeasonReviews.Update")
            .RequireOrganizationPermission(Permissions.CultivationWrite)
            .Produces<SeasonReviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{reviewId:guid}/finalize", FinalizeAsync)
            .WithName("SeasonReviews.Finalize")
            .RequireOrganizationPermission(Permissions.CultivationWrite)
            .Produces<SeasonReviewResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> CreateAsync(Guid organizationId, CreateSeasonReviewRequest request,
        ISeasonReviewService service, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(organizationId, request, cancellationToken);
        return result.ToHttpResult(review => Results.CreatedAtRoute(GetByIdRouteName,
            new { organizationId, reviewId = review.Id }, review));
    }

    private static async Task<IResult> GetByIdAsync(Guid organizationId, Guid reviewId,
        ISeasonReviewService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(organizationId, reviewId, cancellationToken);
        return result.ToHttpResult(review => Results.Ok(review));
    }

    private static async Task<IResult> GetByCropCycleAsync(Guid organizationId, Guid cropCycleId,
        ISeasonReviewService service, CancellationToken cancellationToken)
    {
        var result = await service.GetByCropCycleAsync(organizationId, cropCycleId, cancellationToken);
        return result.ToHttpResult(review => Results.Ok(review));
    }

    private static async Task<IResult> UpdateAsync(Guid organizationId, Guid reviewId,
        UpdateSeasonReviewRequest request, ISeasonReviewService service, CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(organizationId, reviewId, request, cancellationToken);
        return result.ToHttpResult(review => Results.Ok(review));
    }

    private static async Task<IResult> FinalizeAsync(Guid organizationId, Guid reviewId,
        ISeasonReviewService service, CancellationToken cancellationToken)
    {
        var result = await service.FinalizeAsync(organizationId, reviewId, cancellationToken);
        return result.ToHttpResult(review => Results.Ok(review));
    }
}
