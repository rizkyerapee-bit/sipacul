using SiPacul.Application.Common.Persistence;
using SiPacul.Application.Finance.SalePayments.Contracts;
using SiPacul.Application.Finance.SalePayments.Mappings;
using SiPacul.Application.Finance.SalePayments.Persistence;
using SiPacul.Application.Organizations.Persistence;
using SiPacul.Application.Sales.Persistence;
using SiPacul.Domain.Entities.Finance;
using SiPacul.Domain.Entities.Sales;
using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.SalePayments.Services;

public sealed class SalePaymentService :
    ISalePaymentService
{
    private readonly ISalePaymentRepository
        _paymentRepository;

    private readonly ISaleRepository _saleRepository;

    private readonly ISalePaymentConfirmationProcessor
        _confirmationProcessor;

    private readonly IOrganizationRepository
        _organizationRepository;

    private readonly IUnitOfWork _unitOfWork;

    public SalePaymentService(
        ISalePaymentRepository paymentRepository,
        ISaleRepository saleRepository,
        ISalePaymentConfirmationProcessor confirmationProcessor,
        IOrganizationRepository organizationRepository,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _saleRepository = saleRepository;
        _confirmationProcessor = confirmationProcessor;
        _organizationRepository = organizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SalePaymentResponse>>
        CreateAsync(
            Guid organizationId,
            Guid saleId,
            CreateSalePaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            request,
            "Sale payment request cannot be null.");

        if (requestError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            true,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SalePaymentResponse>.Failure(
                saleResult.Error);
        }

        var sale = saleResult.Value;

        var dateError = ValidatePaymentDate(
            request.PaymentDate,
            sale.SaleDate);

        if (dateError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                dateError);
        }

        SalePayment payment;

        try
        {
            payment = SalePayment.Create(
                organizationId,
                saleId,
                request.Code,
                request.PaymentDate,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.ReceivedFrom,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.Validation(
                    exception.Message));
        }

        if (await _paymentRepository.CodeExistsAsync(
                organizationId,
                payment.Code,
                cancellationToken))
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.CodeAlreadyExists(
                    payment.Code));
        }

        _paymentRepository.Add(payment);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SalePaymentResponse>.Success(
            payment.ToResponse());
    }

    public async Task<
        Result<IReadOnlyList<SalePaymentResponse>>>
        GetAllAsync(
            Guid organizationId,
            Guid saleId,
            SalePaymentFilter? filter = null,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return Result<
                IReadOnlyList<SalePaymentResponse>>
                .Failure(identifierError);
        }

        filter ??= new SalePaymentFilter();

        var filterError = ValidateFilter(filter);

        if (filterError is not null)
        {
            return Result<
                IReadOnlyList<SalePaymentResponse>>
                .Failure(filterError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            false,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<
                IReadOnlyList<SalePaymentResponse>>
                .Failure(saleResult.Error);
        }

        var payments =
            await _paymentRepository.GetAllAsync(
                organizationId,
                saleId,
                filter.Status,
                filter.PaymentMethod,
                filter.PaymentDateFrom,
                filter.PaymentDateTo,
                NormalizeTextFilter(
                    filter.ReceivedFrom),
                cancellationToken);

        IReadOnlyList<SalePaymentResponse> responses =
            payments
                .Select(payment => payment.ToResponse())
                .ToArray();

        return Result<
            IReadOnlyList<SalePaymentResponse>>
            .Success(responses);
    }

    public async Task<Result<SalePaymentResponse>>
        GetByIdAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId,
            paymentId);

        if (identifierError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                identifierError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            false,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SalePaymentResponse>.Failure(
                saleResult.Error);
        }

        var payment =
            await _paymentRepository.GetByIdAsync(
                organizationId,
                saleId,
                paymentId,
                cancellationToken);

        if (payment is null)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.NotFound(paymentId));
        }

        return Result<SalePaymentResponse>.Success(
            payment.ToResponse());
    }

    public async Task<Result<SalePaymentResponse>>
        UpdateDraftAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            UpdateSalePaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            paymentId,
            request,
            "Sale payment update request cannot be null.");

        if (requestError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            true,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SalePaymentResponse>.Failure(
                saleResult.Error);
        }

        var dateError = ValidatePaymentDate(
            request.PaymentDate,
            saleResult.Value.SaleDate);

        if (dateError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                dateError);
        }

        var payment =
            await _paymentRepository.GetByIdForUpdateAsync(
                organizationId,
                saleId,
                paymentId,
                cancellationToken);

        if (payment is null)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.NotFound(paymentId));
        }

        var previousDate = payment.PaymentDate;
        var previousAmount = payment.Amount;
        var previousMethod = payment.PaymentMethod;
        var previousReference = payment.ReferenceNumber;
        var previousReceivedFrom = payment.ReceivedFrom;
        var previousNotes = payment.Notes;

        try
        {
            payment.UpdateDraft(
                request.PaymentDate,
                request.Amount,
                request.PaymentMethod,
                request.ReferenceNumber,
                request.ReceivedFrom,
                request.Notes);
        }
        catch (ArgumentException exception)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.InvalidStatusTransition(
                    exception.Message));
        }

        if (previousDate != payment.PaymentDate ||
            previousAmount != payment.Amount ||
            previousMethod != payment.PaymentMethod ||
            previousReference != payment.ReferenceNumber ||
            previousReceivedFrom != payment.ReceivedFrom ||
            previousNotes != payment.Notes)
        {
            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        return Result<SalePaymentResponse>.Success(
            payment.ToResponse());
    }

    public async Task<Result<SalePaymentResponse>>
        ConfirmAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId,
            paymentId);

        if (identifierError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                identifierError);
        }

        var result = await _confirmationProcessor
            .ConfirmAsync(
                organizationId,
                saleId,
                paymentId,
                cancellationToken);

        if (result.IsSuccess)
        {
            return Result<SalePaymentResponse>.Success(
                result.Payment!.ToResponse());
        }

        return Result<SalePaymentResponse>.Failure(
            MapConfirmationFailure(
                paymentId,
                saleId,
                result));
    }

    public async Task<Result<SalePaymentResponse>>
        CancelAsync(
            Guid organizationId,
            Guid saleId,
            Guid paymentId,
            CancelSalePaymentRequest request,
            CancellationToken cancellationToken = default)
    {
        var requestError = ValidateRequest(
            organizationId,
            saleId,
            paymentId,
            request,
            "Sale payment cancellation request cannot be null.");

        if (requestError is not null)
        {
            return Result<SalePaymentResponse>.Failure(
                requestError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            false,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SalePaymentResponse>.Failure(
                saleResult.Error);
        }

        var payment =
            await _paymentRepository.GetByIdForUpdateAsync(
                organizationId,
                saleId,
                paymentId,
                cancellationToken);

        if (payment is null)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.NotFound(paymentId));
        }

        try
        {
            payment.Cancel(
                request.CancellationReason);
        }
        catch (ArgumentException exception)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.Validation(
                    exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return Result<SalePaymentResponse>.Failure(
                SalePaymentErrors.InvalidStatusTransition(
                    exception.Message));
        }

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        return Result<SalePaymentResponse>.Success(
            payment.ToResponse());
    }

    public async Task<Result<SaleReceivableResponse>>
        GetReceivableAsync(
            Guid organizationId,
            Guid saleId,
            CancellationToken cancellationToken = default)
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return Result<SaleReceivableResponse>.Failure(
                identifierError);
        }

        var saleResult = await GetSaleAsync(
            organizationId,
            saleId,
            true,
            cancellationToken);

        if (saleResult.IsFailure)
        {
            return Result<SaleReceivableResponse>.Failure(
                saleResult.Error);
        }

        var sale = saleResult.Value;

        var confirmedPaidAmount =
            await _paymentRepository
                .GetConfirmedPaidAmountAsync(
                    organizationId,
                    saleId,
                    null,
                    cancellationToken);

        SaleReceivableSummary summary;

        try
        {
            summary = SaleReceivableSummary.Calculate(
                sale.TotalAmount,
                confirmedPaidAmount);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Result<SaleReceivableResponse>.Failure(
                SalePaymentErrors.Overpayment(
                    confirmedPaidAmount,
                    sale.TotalAmount));
        }

        return Result<SaleReceivableResponse>.Success(
            sale.ToReceivableResponse(summary));
    }

    private async Task<Result<Sale>> GetSaleAsync(
        Guid organizationId,
        Guid saleId,
        bool requireConfirmed,
        CancellationToken cancellationToken)
    {
        var organization =
            await _organizationRepository.GetByIdAsync(
                organizationId,
                cancellationToken);

        if (organization is null)
        {
            return Result<Sale>.Failure(
                SalePaymentErrors.OrganizationNotFound(
                    organizationId));
        }

        var sale = await _saleRepository.GetByIdAsync(
            organizationId,
            saleId,
            cancellationToken);

        if (sale is null)
        {
            return Result<Sale>.Failure(
                SalePaymentErrors.SaleNotFound(
                    saleId));
        }

        if (requireConfirmed &&
            sale.Status != SaleStatus.Confirmed)
        {
            return Result<Sale>.Failure(
                SalePaymentErrors.SaleNotConfirmed(
                    saleId));
        }

        return Result<Sale>.Success(sale);
    }

    private static Error? ValidatePaymentDate(
        DateOnly paymentDate,
        DateOnly saleDate)
    {
        if (paymentDate < saleDate)
        {
            return SalePaymentErrors
                .PaymentDateBeforeSaleDate(
                    paymentDate,
                    saleDate);
        }

        return null;
    }

    private static Error MapConfirmationFailure(
        Guid paymentId,
        Guid saleId,
        SalePaymentConfirmationResult result)
    {
        return result.Failure switch
        {
            SalePaymentConfirmationFailure
                .PaymentNotFound =>
                SalePaymentErrors.NotFound(paymentId),

            SalePaymentConfirmationFailure
                .SaleNotFound =>
                SalePaymentErrors.SaleNotFound(saleId),

            SalePaymentConfirmationFailure
                .SaleNotConfirmed =>
                SalePaymentErrors.SaleNotConfirmed(saleId),

            SalePaymentConfirmationFailure
                .InvalidStatus =>
                SalePaymentErrors
                    .InvalidStatusTransition(
                        result.Message ??
                        "Only a draft sale payment " +
                        "can be confirmed."),

            SalePaymentConfirmationFailure
                .PaymentDateBeforeSaleDate =>
                SalePaymentErrors
                    .PaymentDateBeforeSaleDate(
                        result.Payment?.PaymentDate ??
                            default,
                        result.SaleDate ??
                            default),

            SalePaymentConfirmationFailure
                .Overpayment =>
                SalePaymentErrors.Overpayment(
                    result.ConfirmedPaidAmount,
                    result.SaleTotalAmount),

            SalePaymentConfirmationFailure
                .ConcurrencyConflict =>
                SalePaymentErrors
                    .ConfirmationConcurrency(),

            _ => SalePaymentErrors.Validation(
                "Sale payment confirmation failed.")
        };
    }

    private static Error? ValidateFilter(
        SalePaymentFilter filter)
    {
        if (filter.Status.HasValue &&
            !Enum.IsDefined(filter.Status.Value))
        {
            return SalePaymentErrors.Validation(
                "Sale payment status is not supported.");
        }

        if (filter.PaymentMethod.HasValue &&
            !Enum.IsDefined(filter.PaymentMethod.Value))
        {
            return SalePaymentErrors.Validation(
                "Sale payment method is not supported.");
        }

        if (filter.PaymentDateFrom.HasValue &&
            filter.PaymentDateTo.HasValue &&
            filter.PaymentDateFrom.Value >
                filter.PaymentDateTo.Value)
        {
            return SalePaymentErrors.Validation(
                "Payment date-from cannot be after " +
                "payment date-to.");
        }

        if (filter.ReceivedFrom is not null &&
            string.IsNullOrWhiteSpace(
                filter.ReceivedFrom))
        {
            return SalePaymentErrors.Validation(
                "Received-from filter cannot be blank.");
        }

        return null;
    }

    private static string? NormalizeTextFilter(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid saleId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return SalePaymentErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateRequest<TRequest>(
        Guid organizationId,
        Guid saleId,
        Guid paymentId,
        TRequest? request,
        string nullRequestMessage)
        where TRequest : class
    {
        var identifierError = ValidateIdentifiers(
            organizationId,
            saleId,
            paymentId);

        if (identifierError is not null)
        {
            return identifierError;
        }

        if (request is null)
        {
            return SalePaymentErrors.Validation(
                nullRequestMessage);
        }

        return null;
    }

    private static Error? ValidateIdentifiers(
        Guid organizationId,
        Guid saleId,
        Guid? paymentId = null)
    {
        if (organizationId == Guid.Empty)
        {
            return SalePaymentErrors.Validation(
                "Organization identifier cannot be empty.");
        }

        if (saleId == Guid.Empty)
        {
            return SalePaymentErrors.Validation(
                "Sale identifier cannot be empty.");
        }

        if (paymentId.HasValue &&
            paymentId.Value == Guid.Empty)
        {
            return SalePaymentErrors.Validation(
                "Sale payment identifier cannot be empty.");
        }

        return null;
    }
}
