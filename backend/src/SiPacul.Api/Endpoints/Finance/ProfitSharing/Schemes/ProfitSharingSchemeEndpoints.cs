using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Contracts;
using SiPacul.Application.Finance.ProfitSharing.Schemes.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Domain.Entities.Finance.ProfitSharing.V2.Schemes;

namespace SiPacul.Api.Endpoints.Finance.ProfitSharing.Schemes;

public static class ProfitSharingSchemeEndpoints
{
    private const string GetByIdRouteName =
        "ProfitSharingSchemes.GetById";

    public static RouteGroupBuilder
        MapProfitSharingSchemeEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/" +
                "profit-sharing-schemes")
            .WithTags("Profit Sharing Schemes");

        group.MapPost(string.Empty, CreateDraftAsync)
            .WithName("ProfitSharingSchemes.CreateDraft")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSchemeResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("ProfitSharingSchemes.GetAll")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<IReadOnlyList<ProfitSharingSchemeResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{schemeId:guid}", GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.ProfitSharingRead)
            .Produces<ProfitSharingSchemeResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{schemeId:guid}", UpdateDraftAsync)
            .WithName("ProfitSharingSchemes.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSchemeResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost(
                "/{sourceSchemeId:guid}/versions",
                CreateNextVersionAsync)
            .WithName("ProfitSharingSchemes.CreateNextVersion")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingWrite)
            .Produces<ProfitSharingSchemeResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{schemeId:guid}/activate", ActivateAsync)
            .WithName("ProfitSharingSchemes.Activate")
            .RequireOrganizationPermission(
                Permissions.ProfitSharingFinalize)
            .Produces<ProfitSharingSchemeResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return group;
    }

    private static async Task<IResult> CreateDraftAsync(
        Guid organizationId,
        CreateProfitSharingSchemeRequest request,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateDraftAsync(
            organizationId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            scheme => Results.CreatedAtRoute(
                GetByIdRouteName,
                new
                {
                    organizationId,
                    schemeId = scheme.Id
                },
                scheme));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        ProfitSharingSchemeStatus? status,
        string? code,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(
            organizationId,
            new ProfitSharingSchemeFilter(status, code),
            cancellationToken);

        return result.ToHttpResult(
            schemes => Results.Ok(schemes));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid schemeId,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            schemeId,
            cancellationToken);

        return result.ToHttpResult(
            scheme => Results.Ok(scheme));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid schemeId,
        UpdateProfitSharingSchemeDraftRequest request,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            schemeId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            scheme => Results.Ok(scheme));
    }

    private static async Task<IResult> CreateNextVersionAsync(
        Guid organizationId,
        Guid sourceSchemeId,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateNextVersionAsync(
            organizationId,
            sourceSchemeId,
            cancellationToken);

        return result.ToHttpResult(
            scheme => Results.CreatedAtRoute(
                GetByIdRouteName,
                new
                {
                    organizationId,
                    schemeId = scheme.Id
                },
                scheme));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid schemeId,
        IProfitSharingSchemeService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ActivateAsync(
            organizationId,
            schemeId,
            cancellationToken);

        return result.ToHttpResult(
            scheme => Results.Ok(scheme));
    }
}
