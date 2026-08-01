using SiPacul.Domain.Entities.Finance;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance;

public sealed class SaleReceivableSummaryTests
{
    [Fact]
    public void Calculate_WithNoPayment_ShouldBeUnpaid()
    {
        var summary =
            SaleReceivableSummary.Calculate(
                1000000,
                0);

        Assert.Equal(
            1000000m,
            summary.SaleTotalAmount);

        Assert.Equal(
            0m,
            summary.ConfirmedPaidAmount);

        Assert.Equal(
            1000000m,
            summary.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.Unpaid,
            summary.PaymentState);

        Assert.False(summary.IsFullyPaid);
        Assert.False(summary.HasCollectedRevenue);
    }

    [Fact]
    public void Calculate_WithPartialPayment_ShouldBePartiallyPaid()
    {
        var summary =
            SaleReceivableSummary.Calculate(
                1000000,
                250000);

        Assert.Equal(
            250000m,
            summary.ConfirmedPaidAmount);

        Assert.Equal(
            750000m,
            summary.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.PartiallyPaid,
            summary.PaymentState);

        Assert.False(summary.IsFullyPaid);
        Assert.True(summary.HasCollectedRevenue);
    }

    [Fact]
    public void Calculate_WithFullPayment_ShouldBePaid()
    {
        var summary =
            SaleReceivableSummary.Calculate(
                1000000,
                1000000);

        Assert.Equal(
            0m,
            summary.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.Paid,
            summary.PaymentState);

        Assert.True(summary.IsFullyPaid);
        Assert.True(summary.HasCollectedRevenue);
    }

    [Fact]
    public void Calculate_WithZeroValueSale_ShouldBePaid()
    {
        var summary =
            SaleReceivableSummary.Calculate(
                0,
                0);

        Assert.Equal(
            0m,
            summary.OutstandingReceivable);

        Assert.Equal(
            SalePaymentState.Paid,
            summary.PaymentState);

        Assert.True(summary.IsFullyPaid);
        Assert.False(summary.HasCollectedRevenue);
    }

    [Fact]
    public void Calculate_ShouldRoundMoneyAwayFromZero()
    {
        var summary =
            SaleReceivableSummary.Calculate(
                100.005m,
                10.005m);

        Assert.Equal(
            100.01m,
            summary.SaleTotalAmount);

        Assert.Equal(
            10.01m,
            summary.ConfirmedPaidAmount);

        Assert.Equal(
            90.00m,
            summary.OutstandingReceivable);
    }

    [Theory]
    [InlineData(-1, 0, "saleTotalAmount")]
    [InlineData(100, -1, "confirmedPaidAmount")]
    public void Calculate_WithNegativeMoney_ShouldThrow(
        double saleTotal,
        double confirmedPaid,
        string parameterName)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SaleReceivableSummary.Calculate(
                    (decimal)saleTotal,
                    (decimal)confirmedPaid));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Calculate_WhenPaymentExceedsTotal_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SaleReceivableSummary.Calculate(
                    100,
                    100.01m));

        Assert.Equal(
            "confirmedPaidAmount",
            exception.ParamName);
    }

    [Fact]
    public void Calculate_WhenRoundedPaymentExceedsTotal_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SaleReceivableSummary.Calculate(
                    100.004m,
                    100.005m));

        Assert.Equal(
            "confirmedPaidAmount",
            exception.ParamName);
    }

    [Theory]
    [InlineData(1, SalePaymentState.Unpaid)]
    [InlineData(2, SalePaymentState.PartiallyPaid)]
    [InlineData(3, SalePaymentState.Paid)]
    public void PaymentState_ShouldUseStableNumericValues(
        int expectedValue,
        SalePaymentState state)
    {
        Assert.Equal(expectedValue, (int)state);
    }
}
