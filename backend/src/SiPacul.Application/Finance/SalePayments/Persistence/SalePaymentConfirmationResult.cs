using SiPacul.Domain.Entities.Finance;

namespace SiPacul.Application.Finance.SalePayments.Persistence;

public enum SalePaymentConfirmationFailure
{
    None = 0,

    PaymentNotFound = 1,

    SaleNotFound = 2,

    SaleNotConfirmed = 3,

    InvalidStatus = 4,

    PaymentDateBeforeSaleDate = 5,

    Overpayment = 6,

    ConcurrencyConflict = 7
}

public sealed record SalePaymentConfirmationResult(
    SalePayment? Payment,
    SalePaymentConfirmationFailure Failure,
    DateOnly? SaleDate = null,
    decimal ConfirmedPaidAmount = 0,
    decimal SaleTotalAmount = 0,
    string? Message = null)
{
    public bool IsSuccess =>
        Failure == SalePaymentConfirmationFailure.None &&
        Payment is not null;

    public static SalePaymentConfirmationResult Succeeded(
        SalePayment payment,
        decimal confirmedPaidAmount,
        decimal saleTotalAmount)
    {
        ArgumentNullException.ThrowIfNull(payment);

        return new SalePaymentConfirmationResult(
            payment,
            SalePaymentConfirmationFailure.None,
            ConfirmedPaidAmount: confirmedPaidAmount,
            SaleTotalAmount: saleTotalAmount);
    }

    public static SalePaymentConfirmationResult Failed(
        SalePaymentConfirmationFailure failure,
        DateOnly? saleDate = null,
        decimal confirmedPaidAmount = 0,
        decimal saleTotalAmount = 0,
        string? message = null)
    {
        if (failure ==
            SalePaymentConfirmationFailure.None)
        {
            throw new ArgumentOutOfRangeException(
                nameof(failure),
                "A failed confirmation must specify a failure.");
        }

        return new SalePaymentConfirmationResult(
            null,
            failure,
            saleDate,
            confirmedPaidAmount,
            saleTotalAmount,
            message);
    }
}
