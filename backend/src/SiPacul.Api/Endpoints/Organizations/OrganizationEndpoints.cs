using SiPacul.Api.Common.Http;
using SiPacul.Application.Organizations.Contracts;
using SiPacul.Application.Organizations.Services;

namespace SiPacul.Api.Endpoints.Organizations;

public static class OrganizationEndpoints
{
    private const string GetByIdRouteName =
        "Organizations.GetById";

    public static RouteGroupBuilder MapOrganizationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/organizations")
            .WithTags("Organizations");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("Organizations.Create")
            .Produces<OrganizationResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(
                string.Empty,
                GetAllAsync)
            .WithName("Organizations.GetAll")
            .Produces<
                IReadOnlyList<OrganizationResponse>>(
                StatusCodes.Status200OK);

        group.MapGet(
                "/{organizationId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<OrganizationResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{organizationId:guid}",
                UpdateAsync)
            .WithName("Organizations.Update")
            .Produces<OrganizationResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{organizationId:guid}/activate",
                ActivateAsync)
            .WithName("Organizations.Activate")
            .Produces<OrganizationResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPatch(
                "/{organizationId:guid}/deactivate",
                DeactivateAsync)
            .WithName("Organizations.Deactivate")
            .Produces<OrganizationResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> CreateAsync(
        CreateOrganizationRequest request,
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.CreateAsync(
                request,
                cancellationToken);

        return result.ToHttpResult(
            organization =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId =
                            organization.Id
                    },
                    organization));
    }

    private static async Task<IResult> GetAllAsync(
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.GetAllAsync(
                cancellationToken);

        return result.ToHttpResult(
            organizations =>
                Results.Ok(organizations));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.GetByIdAsync(
                organizationId,
                cancellationToken);

        return result.ToHttpResult(
            organization =>
                Results.Ok(organization));
    }

    private static async Task<IResult> UpdateAsync(
        Guid organizationId,
        UpdateOrganizationRequest request,
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.UpdateAsync(
                organizationId,
                request,
                cancellationToken);

        return result.ToHttpResult(
            organization =>
                Results.Ok(organization));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.ActivateAsync(
                organizationId,
                cancellationToken);

        return result.ToHttpResult(
            organization =>
                Results.Ok(organization));
    }

    private static async Task<IResult> DeactivateAsync(
        Guid organizationId,
        IOrganizationService organizationService,
        CancellationToken cancellationToken)
    {
        var result =
            await organizationService.DeactivateAsync(
                organizationId,
                cancellationToken);

        return result.ToHttpResult(
            organization =>
                Results.Ok(organization));
    }
}
