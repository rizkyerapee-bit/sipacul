using SiPacul.Api.Common.Http;
using SiPacul.Api.Security;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Organizations.Members;
using SiPacul.Application.Organizations.Members.Contracts;
using SiPacul.Application.Organizations.Members.Services;
using SiPacul.Application.Security.Authorization;
using SiPacul.Shared.Results;

namespace SiPacul.Api.Endpoints.Organizations.Members;

public static class OrganizationMemberEndpoints
{
    private const string GetByIdRouteName =
        "OrganizationMembers.GetById";

    public static RouteGroupBuilder
        MapOrganizationMemberEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/members")
            .WithTags("Organization Members")
            .RequireAuthorization();

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("OrganizationMembers.GetAll")
            .Produces<
                IReadOnlyList<OrganizationMemberResponse>>(
                    StatusCodes.Status200OK)
            .RequireOrganizationPermission(
                Permissions.MembersRead);

        group.MapGet(
                "/{membershipId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .Produces<OrganizationMemberResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MembersRead);

        group.MapPost(string.Empty, CreateAsync)
            .WithName("OrganizationMembers.Create")
            .Produces<OrganizationMemberResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status409Conflict)
            .RequireOrganizationPermission(
                Permissions.MembersManage)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        group.MapPatch(
                "/{membershipId:guid}/role",
                ChangeRoleAsync)
            .WithName("OrganizationMembers.ChangeRole")
            .Produces<OrganizationMemberResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MembersManage)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        group.MapPatch(
                "/{membershipId:guid}/activate",
                ActivateAsync)
            .WithName("OrganizationMembers.Activate")
            .Produces<OrganizationMemberResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MembersManage)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        group.MapPatch(
                "/{membershipId:guid}/suspend",
                SuspendAsync)
            .WithName("OrganizationMembers.Suspend")
            .Produces<OrganizationMemberResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireOrganizationPermission(
                Permissions.MembersManage)
            .AddEndpointFilter<AntiforgeryEndpointFilter>();

        return group;
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.GetAllAsync(
            organizationId,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            members => Results.Ok(members));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid membershipId,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.GetByIdAsync(
            organizationId,
            membershipId,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            member => Results.Ok(member));
    }

    private static async Task<IResult> CreateAsync(
        Guid organizationId,
        CreateOrganizationMemberRequest request,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.CreateAsync(
            organizationId,
            request,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            member => Results.CreatedAtRoute(
                GetByIdRouteName,
                new
                {
                    organizationId,
                    membershipId = member.MembershipId
                },
                member));
    }

    private static async Task<IResult> ChangeRoleAsync(
        Guid organizationId,
        Guid membershipId,
        UpdateOrganizationMemberRoleRequest request,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.ChangeRoleAsync(
            organizationId,
            membershipId,
            request,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            member => Results.Ok(member));
    }

    private static async Task<IResult> ActivateAsync(
        Guid organizationId,
        Guid membershipId,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.ActivateAsync(
            organizationId,
            membershipId,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            member => Results.Ok(member));
    }

    private static async Task<IResult> SuspendAsync(
        Guid organizationId,
        Guid membershipId,
        IOrganizationMemberService memberService,
        CancellationToken cancellationToken)
    {
        var result = await memberService.SuspendAsync(
            organizationId,
            membershipId,
            cancellationToken);

        return ToMemberHttpResult(
            result,
            member => Results.Ok(member));
    }

    private static IResult ToMemberHttpResult<T>(
        Result<T> result,
        Func<T, IResult> onSuccess)
    {
        if (result.IsFailure &&
            result.Error.Code ==
                OrganizationMemberErrors.OwnerProtectedCode)
        {
            return Results.Forbid();
        }

        return result.ToHttpResult(onSuccess);
    }
}
