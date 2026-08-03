using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Sales.Contracts;
using SiPacul.Application.Sales.Services;
using SiPacul.Domain.Entities.Sales;

namespace SiPacul.Api.Endpoints.Sales;

public static class SaleEndpoints
{
    private const string GetByIdRouteName =
        "Sales.GetById";

    public static RouteGroupBuilder MapSaleEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/sales")
            .WithTags("Sales");

        group.MapPost(
                string.Empty,
                CreateAsync)
            .WithName("Sales.Create")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(
                string.Empty,
                GetAllAsync)
            .WithName("Sales.GetAll")
            .RequireOrganizationPermission(
                Permissions.SalesRead)
            .Produces<IReadOnlyList<SaleResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{saleId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.SalesRead)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{saleId:guid}",
                UpdateDraftAsync)
            .WithName("Sales.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPost(
                "/{saleId:guid}/lines",
                AddLineAsync)
            .WithName("Sales.AddLine")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPut(
                "/{saleId:guid}/lines/{saleLineId:guid}",
                UpdateLineAsync)
            .WithName("Sales.UpdateLine")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapDelete(
                "/{saleId:guid}/lines/{saleLineId:guid}",
                RemoveLineAsync)
            .WithName("Sales.RemoveLine")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{saleId:guid}/confirm",
                ConfirmAsync)
            .WithName("Sales.Confirm")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{saleId:guid}/cancel",
                CancelAsync)
            .WithName("Sales.Cancel")
            .RequireOrganizationPermission(
                Permissions.SalesWrite)
            .Produces<SaleResponse>(
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
        CreateSaleRequest request,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        saleId = sale.Id
                    },
                    sale));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        SaleStatus? status,
        DateOnly? saleDateFrom,
        DateOnly? saleDateTo,
        SalePaymentTerm? paymentTerm,
        string? buyerName,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var filter = new SaleFilter(
            status,
            saleDateFrom,
            saleDateTo,
            paymentTerm,
            buyerName);

        var result = await service.GetAllAsync(
            organizationId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            sales =>
                Results.Ok(sales));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            saleId,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid saleId,
        UpdateSaleRequest request,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            saleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> AddLineAsync(
        Guid organizationId,
        Guid saleId,
        AddSaleLineRequest request,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.AddLineAsync(
            organizationId,
            saleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> UpdateLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        UpdateSaleLineRequest request,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateLineAsync(
            organizationId,
            saleId,
            saleLineId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> RemoveLineAsync(
        Guid organizationId,
        Guid saleId,
        Guid saleLineId,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RemoveLineAsync(
            organizationId,
            saleId,
            saleLineId,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(
            organizationId,
            saleId,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid saleId,
        CancelSaleRequest request,
        ISaleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            saleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            sale =>
                Results.Ok(sale));
    }
}
