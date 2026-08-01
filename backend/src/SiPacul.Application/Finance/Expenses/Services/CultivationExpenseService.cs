using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Cultivation.CropCycles.Persistence;
using SiPacul.Application.Finance.Expenses.Contracts;
using SiPacul.Application.Finance.Expenses.Mappings;
using SiPacul.Application.Finance.Expenses.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Domain.Entities.Cultivation;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.Expenses.Services;

public sealed class CultivationExpenseService :
    ICultivationExpenseService
{
    private readonly ICultivationExpenseRepository
        _expenseRepository;

    private readonly ICropCycleRepository
        _cropCycleRepository;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public CultivationExpenseService(
        ICultivationExpenseRepository expenseRepository,
        ICropCycleRepository cropCycleRepository,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _expenseRepository = expenseRepository;
        _cropCycleRepository = cropCycleRepository;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CultivationExpenseResponse>>
        CreateAsync(
            Guid organizationId,
            Guid cropCycleId,
            CreateCultivationExpenseRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            request,
            "Cultivation expense request cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(requestError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CultivationExpenseResponse>
                .Failure(parentResult.Error);
        }

        var dateError = ValidateExpenseDate(
            request.ExpenseDate,
            parentResult.Value);

        if (dateError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(dateError);
        }

        CultivationExpense expense;

        try
        {
            expense = CultivationExpense.Create(
                organizationId,
                cropCycleId,
                request.Code,
                request.ExpenseDate,
                request.Category,
                request.Description,
                request.Amount,
                request.PayeeName,
                request.ReferenceNumber,
                request.EvidenceUrl,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors.Validation(
                        exception.Message));
        }

        if (await _expenseRepository.CodeExistsAsync(
                organizationId,
                cropCycleId,
                expense.Code,
                cancellationToken))
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors
                        .CodeAlreadyExists(
                            expense.Code));
        }

        _expenseRepository.Add(expense);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationExpenseResponse>
            .Success(expense.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<CultivationExpenseResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid cropCycleId,
            CultivationExpenseFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<CultivationExpenseResponse>>
                .Failure(identifierError);
        }

        filter ??= new CultivationExpenseFilter();

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<CultivationExpenseResponse>>
                .Failure(filterError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<
                IReadOnlyList<CultivationExpenseResponse>>
                .Failure(parentResult.Error);
        }

        var expenses = await _expenseRepository.GetAllAsync(
            organizationId,
            cropCycleId,
            filter.Status,
            filter.Category,
            filter.ExpenseDateFrom,
            filter.ExpenseDateTo,
            NormalizePayeeFilter(filter.PayeeName),
            cancellationToken);

        IReadOnlyList<CultivationExpenseResponse> responses =
            expenses
                .Select(expense => expense.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<CultivationExpenseResponse>>
            .Success(responses);
    }

    public async Task<Result<CultivationExpenseResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            expenseId);

        if (identifierError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(identifierError);
        }

        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<CultivationExpenseResponse>
                .Failure(parentResult.Error);
        }

        var expense = await _expenseRepository.GetByIdAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        if (expense is null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors.NotFound(
                        expenseId));
        }

        return Result<CultivationExpenseResponse>
            .Success(expense.ToResponse());
    }

    public async Task<Result<CultivationExpenseResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            UpdateCultivationExpenseRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            expenseId,
            request,
            "Cultivation expense update request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationExpenseResponse>
                .Failure(contextResult.Error);
        }

        var dateError = ValidateExpenseDate(
            request.ExpenseDate,
            contextResult.Value.CropCycle);

        if (dateError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(dateError);
        }

        try
        {
            contextResult.Value.Expense.UpdateDraft(
                request.ExpenseDate,
                request.Category,
                request.Description,
                request.Amount,
                request.PayeeName,
                request.ReferenceNumber,
                request.EvidenceUrl,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationExpenseResponse>
            .Success(
                contextResult.Value.Expense.ToResponse());
    }

    public async Task<Result<CultivationExpenseResponse>>
        ConfirmAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            expenseId);

        if (identifierError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(identifierError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationExpenseResponse>
                .Failure(contextResult.Error);
        }

        try
        {
            contextResult.Value.Expense.Confirm();
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationExpenseResponse>
            .Success(
                contextResult.Value.Expense.ToResponse());
    }

    public async Task<Result<CultivationExpenseResponse>>
        CancelAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancelCultivationExpenseRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            cropCycleId,
            expenseId,
            request,
            "Cultivation expense cancellation request " +
            "cannot be null.");

        if (requestError is not null)
        {
            return Result<CultivationExpenseResponse>
                .Failure(requestError);
        }

        var contextResult = await GetMutationContextAsync(
            organizationId,
            cropCycleId,
            expenseId,
            cancellationToken);

        if (contextResult.IsFailure)
        {
            return Result<CultivationExpenseResponse>
                .Failure(contextResult.Error);
        }

        try
        {
            contextResult.Value.Expense.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors.Validation(
                        exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<CultivationExpenseResponse>
                .Failure(
                    CultivationExpenseErrors
                        .InvalidStatusTransition(
                            exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<CultivationExpenseResponse>
            .Success(
                contextResult.Value.Expense.ToResponse());
    }

    private async Task<Result<CropCycle>> GetParentAsync(
        Guid organizationId,
        Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null ||
            organization.IsDeleted)
        {
            return Result<CropCycle>.Failure(
                CultivationExpenseErrors
                    .OrganizationNotFound(
                        organizationId));
        }

        var cropCycle =
            await _cropCycleRepository.GetByIdAsync(
                organizationId,
                cropCycleId,
                cancellationToken);

        if (cropCycle is null)
        {
            return Result<CropCycle>.Failure(
                CultivationExpenseErrors
                    .CropCycleNotFound(
                        cropCycleId));
        }

        return Result<CropCycle>.Success(cropCycle);
    }

    private async Task<Result<MutationContext>>
        GetMutationContextAsync(
            Guid organizationId,
            Guid cropCycleId,
            Guid expenseId,
            CancellationToken cancellationToken)
    {
        var parentResult = await GetParentAsync(
            organizationId,
            cropCycleId,
            cancellationToken);

        if (parentResult.IsFailure)
        {
            return Result<MutationContext>.Failure(
                parentResult.Error);
        }

        var expense =
            await _expenseRepository.GetByIdForUpdateAsync(
                organizationId,
                cropCycleId,
                expenseId,
                cancellationToken);

        if (expense is null)
        {
            return Result<MutationContext>.Failure(
                CultivationExpenseErrors.NotFound(
                    expenseId));
        }

        return Result<MutationContext>.Success(
            new MutationContext(
                parentResult.Value,
                expense));
    }

    private static Error? ValidateExpenseDate(
        DateOnly expenseDate,
        CropCycle cropCycle)
    {
        if (expenseDate == default)
        {
            return CultivationExpenseErrors.Validation(
                "Expense date must be provided.");
        }

        var earliestDate =
            cropCycle.PlannedStartDate.AddYears(-1);

        var terminalDate =
            cropCycle.ActualHarvestDate ??
            cropCycle.ExpectedHarvestDate;

        var latestDate =
            terminalDate.AddYears(1);

        if (expenseDate < earliestDate ||
            expenseDate > latestDate)
        {
            return CultivationExpenseErrors.DateOutOfRange(
                expenseDate,
                earliestDate,
                latestDate);
        }

        return null;
    }

    private static Error? ValidateFilter(
        CultivationExpenseFilter filter)
    {
        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return CultivationExpenseErrors.Validation(
                "Cultivation expense status is not supported.");
        }

        if (filter.Category.HasValue &&
            !Enum.IsDefined(filter.Category.Value))
        {
            return CultivationExpenseErrors.Validation(
                "Cultivation expense category is not supported.");
        }

        if (filter.ExpenseDateFrom.HasValue &&
            filter.ExpenseDateTo.HasValue &&
            filter.ExpenseDateFrom.Value >
                filter.ExpenseDateTo.Value)
        {
            return CultivationExpenseErrors.Validation(
                "Expense date from cannot be after " +
                "expense date to.");
        }

        return null;
    }

    private static string? NormalizePayeeFilter(
        string? payeeName)
    {
        return string.IsNullOrWhiteSpace(payeeName)
            ? null
            : payeeName.Trim();
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CultivationExpenseErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid cropCycleId,
        Guid expenseId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            cropCycleId,
            expenseId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return CultivationExpenseErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid cropCycleId,
        Guid? expenseId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return CultivationExpenseErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (cropCycleId == Guid.Empty)
        {
            return CultivationExpenseErrors.Validation(
                "Crop cycle identifier cannot be empty.");
        }

        if (expenseId.HasValue &&
            expenseId.Value == Guid.Empty)
        {
            return CultivationExpenseErrors.Validation(
                "Cultivation expense identifier cannot be empty.");
        }

        return null;
    }

    private sealed record MutationContext(
        CropCycle CropCycle,
        CultivationExpense Expense);
}
