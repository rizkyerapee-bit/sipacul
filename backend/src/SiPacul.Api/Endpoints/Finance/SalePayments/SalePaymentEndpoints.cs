using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Application.Finance.SalePayments.Services;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Api.Endpoints.Finance.SalePayments;

public static class SalePaymentEndpoints
{
    private const string GetByIdRouteName =
        "SalePayments.GetById";

    public static RouteGroupBuilder
        MapSalePaymentEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/sales/" +
                "{saleId:guid}/payments")
            .WithTags("Sale Payments");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("SalePayments.Create")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<SalePaymentResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("SalePayments.GetAll")
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<
                IReadOnlyList<SalePaymentResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/receivable",
                GetReceivableAsync)
            .WithName("SalePayments.GetReceivable")
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<SaleReceivableResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(
                "/{paymentId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<SalePaymentResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{paymentId:guid}",
                UpdateDraftAsync)
            .WithName("SalePayments.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<SalePaymentResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{paymentId:guid}/confirm",
                ConfirmAsync)
            .WithName("SalePayments.Confirm")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<SalePaymentResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{paymentId:guid}/cancel",
                CancelAsync)
            .WithName("SalePayments.Cancel")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<SalePaymentResponse>(
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
        Guid saleId,
        CreateSalePaymentRequest request,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            saleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            payment =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        saleId,
                        paymentId = payment.Id
                    },
                    payment));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid saleId,
        SalePaymentStatus? status,
        SalePaymentMethod? paymentMethod,
        DateOnly? paymentDateFrom,
        DateOnly? paymentDateTo,
        string? receivedFrom,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var filter = new SalePaymentFilter(
            status,
            paymentMethod,
            paymentDateFrom,
            paymentDateTo,
            receivedFrom);

        var result = await service.GetAllAsync(
            organizationId,
            saleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            payments => Results.Ok(payments));
    }

    private static async Task<IResult> GetReceivableAsync(
        Guid organizationId,
        Guid saleId,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetReceivableAsync(
            organizationId,
            saleId,
            cancellationToken);

        return result.ToHttpResult(
            receivable => Results.Ok(receivable));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            saleId,
            paymentId,
            cancellationToken);

        return result.ToHttpResult(
            payment => Results.Ok(payment));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        UpdateSalePaymentRequest request,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            saleId,
            paymentId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            payment => Results.Ok(payment));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(
            organizationId,
            saleId,
            paymentId,
            cancellationToken);

        return result.ToHttpResult(
            payment => Results.Ok(payment));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        CancelSalePaymentRequest request,
        ISalePaymentService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            saleId,
            paymentId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            payment => Results.Ok(payment));
    }
}
