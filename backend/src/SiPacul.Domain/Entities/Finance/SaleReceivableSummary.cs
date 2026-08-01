namespace SiPacul.Domain.Entities.Finance;

public readonly record struct SaleReceivableSummary
{
    private SaleReceivableSummary(
        decimal saleTotalAmount,
        decimal confirmedPaidAmount,
        decimal outstandingReceivable,
        SalePaymentState paymentState)
    {
        SaleTotalAmount = saleTotalAmount;
        ConfirmedPaidAmount = confirmedPaidAmount;
        OutstandingReceivable = outstandingReceivable;
        PaymentState = paymentState;
    }

    public decimal SaleTotalAmount { get; }

    public decimal ConfirmedPaidAmount { get; }

    public decimal OutstandingReceivable { get; }

    public SalePaymentState PaymentState { get; }

    public bool IsFullyPaid =>
        PaymentState == SalePaymentState.Paid;

    public bool HasCollectedRevenue =>
        ConfirmedPaidAmount > 0;

    public static SaleReceivableSummary Calculate(
        decimal saleTotalAmount,
        decimal confirmedPaidAmount)
    {
        var normalizedSaleTotal =
            NormalizeMoney(
                saleTotalAmount,
                nameof(saleTotalAmount));

        var normalizedConfirmedPaid =
            NormalizeMoney(
                confirmedPaidAmount,
                nameof(confirmedPaidAmount));

        if (normalizedConfirmedPaid >
            normalizedSaleTotal)
        {
            throw new ArgumentOutOfRangeException(
                nameof(confirmedPaidAmount),
                "Confirmed paid amount cannot exceed " +
                "the sale total amount.");
        }

        var outstandingReceivable =
            Math.Round(
                normalizedSaleTotal -
                normalizedConfirmedPaid,
                2,
                MidpointRounding.AwayFromZero);

        var paymentState =
            ResolvePaymentState(
                normalizedSaleTotal,
                normalizedConfirmedPaid);

        return new SaleReceivableSummary(
            normalizedSaleTotal,
            normalizedConfirmedPaid,
            outstandingReceivable,
            paymentState);
    }

    private static decimal NormalizeMoney(
        decimal value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Money value cannot be negative.");
        }

        return Math.Round(
            value,
            2,
            MidpointRounding.AwayFromZero);
    }

    private static SalePaymentState ResolvePaymentState(
        decimal saleTotalAmount,
        decimal confirmedPaidAmount)
    {
        if (saleTotalAmount == 0 ||
            confirmedPaidAmount ==
                saleTotalAmount)
        {
            return SalePaymentState.Paid;
        }

        if (confirmedPaidAmount == 0)
        {
            return SalePaymentState.Unpaid;
        }

        return SalePaymentState.PartiallyPaid;
    }
}
