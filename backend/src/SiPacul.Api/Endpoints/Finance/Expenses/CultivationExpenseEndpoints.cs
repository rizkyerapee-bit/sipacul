using SiPacul.Api.Common.Http;
using SiPacul.Api.Security.Authorization;
using SiPacul.Application.Security.Authorization;
using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Application.Finance.Expenses.Services;
using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Api.Endpoints.Finance.Expenses;

public static class CultivationExpenseEndpoints
{
    private const string GetByIdRouteName =
        "CultivationExpenses.GetById";

    public static RouteGroupBuilder
        MapCultivationExpenseEndpoints(
            this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup(
                "/api/v1/organizations/" +
                "{organizationId:guid}/crop-cycles/" +
                "{cropCycleId:guid}/expenses")
            .WithTags("Cultivation Expenses");

        group.MapPost(string.Empty, CreateAsync)
            .WithName("CultivationExpenses.Create")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CultivationExpenseResponse>(
                StatusCodes.Status201Created)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("CultivationExpenses.GetAll")
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<
                IReadOnlyList<CultivationExpenseResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/{expenseId:guid}",
                GetByIdAsync)
            .WithName(GetByIdRouteName)
            .RequireOrganizationPermission(
                Permissions.FinanceRead)
            .Produces<CultivationExpenseResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapPut(
                "/{expenseId:guid}",
                UpdateDraftAsync)
            .WithName(
                "CultivationExpenses.UpdateDraft")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CultivationExpenseResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{expenseId:guid}/confirm",
                ConfirmAsync)
            .WithName("CultivationExpenses.Confirm")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CultivationExpenseResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        group.MapPatch(
                "/{expenseId:guid}/cancel",
                CancelAsync)
            .WithName("CultivationExpenses.Cancel")
            .RequireOrganizationPermission(
                Permissions.FinanceWrite)
            .Produces<CultivationExpenseResponse>(
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
        CreateCultivationExpenseRequest request,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            organizationId,
            cropCycleId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            expense =>
                Results.CreatedAtRoute(
                    GetByIdRouteName,
                    new
                    {
                        organizationId,
                        cropCycleId,
                        expenseId = expense.Id
                    },
                    expense));
    }

    private static async Task<IResult> GetAllAsync(
        Guid organizationId,
        Guid cropCycleId,
        CultivationExpenseStatus? status,
        CultivationExpenseCategory? category,
        DateOnly? expenseDateFrom,
        DateOnly? expenseDateTo,
        string? payeeName,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var filter = new CultivationExpenseFilter(
            status,
            category,
            expenseDateFrom,
            expenseDateTo,
            payeeName);

        var result = await service.GetAllAsync(
            organizationId,
            cropCycleId,
            filter,
            cancellationToken);

        return result.ToHttpResult(
            expenses => Results.Ok(expenses));
    }

    private static async Task<IResult> GetByIdAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        return result.ToHttpResult(
            expense => Results.Ok(expense));
    }

    private static async Task<IResult> UpdateDraftAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        UpdateCultivationExpenseRequest request,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateDraftAsync(
            organizationId,
            cropCycleId,
            expenseId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            expense => Results.Ok(expense));
    }

    private static async Task<IResult> ConfirmAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        return result.ToHttpResult(
            expense => Results.Ok(expense));
    }

    private static async Task<IResult> CancelAsync(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        CancelCultivationExpenseRequest request,
        ICultivationExpenseService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(
            organizationId,
            cropCycleId,
            expenseId,
            request,
            cancellationToken);

        return result.ToHttpResult(
            expense => Results.Ok(expense));
    }
}
