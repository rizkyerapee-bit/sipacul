using SiPacul.Shared.Results;

namespace SiPacul.Application.Finance.SalePayments;

public static class SalePaymentErrors
{
    public const string ValidationCode =
        "SalePayments.Validation";

    public const string OrganizationNotFoundCode =
        "SalePayments.OrganizationNotFound";

    public const string SaleNotFoundCode =
        "SalePayments.SaleNotFound";

    public const string NotFoundCode =
        "SalePayments.NotFound";

    public const string CodeAlreadyExistsCode =
        "SalePayments.CodeAlreadyExists";

    public const string SaleNotConfirmedCode =
        "SalePayments.SaleNotConfirmed";

    public const string InvalidStatusTransitionCode =
        "SalePayments.InvalidStatusTransition";

    public const string PaymentDateBeforeSaleDateCode =
        "SalePayments.PaymentDateBeforeSaleDate";

    public const string OverpaymentCode =
        "SalePayments.Overpayment";

    public const string ConfirmationConcurrencyCode =
        "SalePayments.ConfirmationConcurrency";

    public static Error Validation(string message)
    {
        return Error.Validation(
            ValidationCode,
            message);
    }

    public static Error OrganizationNotFound(
        Guid organizationId)
    {
        return Error.NotFound(
            OrganizationNotFoundCode,
            $"Organization '{organizationId}' was not found.");
    }

    public static Error SaleNotFound(Guid saleId)
    {
        return Error.NotFound(
            SaleNotFoundCode,
            $"Sale '{saleId}' was not found in this organization.");
    }

    public static Error NotFound(Guid paymentId)
    {
        return Error.NotFound(
            NotFoundCode,
            $"Sale payment '{paymentId}' was not found " +
            "for this sale.");
    }

    public static Error CodeAlreadyExists(string code)
    {
        return Error.Conflict(
            CodeAlreadyExistsCode,
            $"Sale payment code '{code}' already exists " +
            "in this organization.");
    }

    public static Error SaleNotConfirmed(Guid saleId)
    {
        return Error.Conflict(
            SaleNotConfirmedCode,
            $"Sale '{saleId}' must be Confirmed before " +
            "payments can be recorded or confirmed.");
    }

    public static Error InvalidStatusTransition(
        string message)
    {
        return Error.Conflict(
            InvalidStatusTransitionCode,
            message);
    }

    public static Error PaymentDateBeforeSaleDate(
        DateOnly paymentDate,
        DateOnly saleDate)
    {
        return Error.Validation(
            PaymentDateBeforeSaleDateCode,
            $"Payment date '{paymentDate:yyyy-MM-dd}' cannot " +
            $"be before sale date '{saleDate:yyyy-MM-dd}'.");
    }

    public static Error Overpayment(
        decimal attemptedPaidAmount,
        decimal saleTotalAmount)
    {
        return Error.Conflict(
            OverpaymentCode,
            $"Confirmed payment total " +
            $"'{attemptedPaidAmount:0.00}' cannot exceed " +
            $"sale total '{saleTotalAmount:0.00}'.");
    }

    public static Error ConfirmationConcurrency()
    {
        return Error.Conflict(
            ConfirmationConcurrencyCode,
            "The payment could not be confirmed because " +
            "the sale balance changed concurrently. " +
            "Reload the payments and try again.");
    }
}
