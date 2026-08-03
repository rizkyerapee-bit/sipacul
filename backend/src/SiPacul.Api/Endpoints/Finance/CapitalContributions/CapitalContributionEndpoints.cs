using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Finance.CapitalContributions.Contracts;
using SiPacul.Application.Finance.CapitalContributions.Services;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Api.Endpoints.Finance.CapitalContributions;

public static class CapitalContributionEndpoints
{
    private const string GetByIdRouteName =
        "CapitalContributions.GetById";

    public static RouteGroupBuilder
        MapCapitalContributionEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/capital-contributions")
            .WithTags("Capital Contributions");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CapitalContributions.Create")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CapitalContributionResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("CapitalContributions.GetAll")
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<
                IReadOnlyList<CapitalContributionResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{contributionId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<CapitalContributionResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{contributionId:guid}",
                UpdateDraftAsync)
            .WithName(
                "CapitalContributions.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CapitalContributionResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{contributionId:guid}/confirm",
                ConfirmAsync)
            .WithName("CapitalContributions.Confirm")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CapitalContributionResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{contributionId:guid}/cancel",
                CancelAsync)
            .WithName("CapitalContributions.Cancel")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CapitalContributionResponse>(
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
        CreateCapitalContributionRequest request,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            contribution =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cropCycleId,
                        contributionId = contribution.Id
                    },
                    contribution));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CapitalContributionStatus? status,
        CapitalContributorRole? contributorRole,
        DateOnly? contributionDateFrom,
        DateOnly? contributionDateTo,
        string? contributorCode,
        string? contributorName,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var filter = new CapitalContributionFilter(
            status,
            contributorRole,
            contributionDateFrom,
            contributionDateTo,
            contributorCode,
            contributorName);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            contributions => Results.Ok(contributions));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            contributionId,
            cancellationToken);

        return result.ToHttpResult(
            contribution => Results.Ok(contribution));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        UpdateCapitalContributionRequest request,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            cropCycleId,
            contributionId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            contribution => Results.Ok(contribution));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(
            organizationId,
            cropCycleId,
            contributionId,
            cancellationToken);

        return result.ToHttpResult(
            contribution => Results.Ok(contribution));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid contributionId,
        CancelCapitalContributionRequest request,
        ICapitalContributionService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            cropCycleId,
            contributionId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            contribution => Results.Ok(contribution));
    }
}
