using SiPacul.Domain.Entities.Finance;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance;

public sealed class SalePaymentTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid SaleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly DateOnly PaymentDate =
        new(2027, 5, 20);

    [Fact]
    public void Create_WithValidValues_ShouldCreateDraft()
    {
        var payment = CreatePayment();

        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(
            OrganizationId,
            payment.OrganizationId);

        Assert.Equal(SaleId, payment.SaleId);
        Assert.Equal("PAY-001", payment.Code);
        Assert.Equal(PaymentDate, payment.PaymentDate);
        Assert.Equal(1000000m, payment.Amount);

        Assert.Equal(
            SalePaymentMethod.BankTransfer,
            payment.PaymentMethod);

        Assert.Equal(
            "TRF-001",
            payment.ReferenceNumber);

        Assert.Equal(
            "Pembeli Utama",
            payment.ReceivedFrom);

        Assert.Equal(
            "Pembayaran tahap pertama",
            payment.Notes);

        Assert.Equal(
            SalePaymentStatus.Draft,
            payment.Status);

        Assert.Null(payment.ConfirmedAt);
        Assert.Null(payment.CancellationReason);
        Assert.False(payment.IsCollectedRevenue);
    }

    [Fact]
    public void Create_ShouldNormalizeValues()
    {
        var payment =
            SalePayment.Create(
                OrganizationId,
                SaleId,
                "  pay.abc_01-x  ",
                PaymentDate,
                1250.125m,
                SalePaymentMethod.Cash,
                "  CASH-001  ",
                "  Pembeli Eceran  ",
                "  Dibayar tunai  ");

        Assert.Equal(
            "PAY.ABC_01-X",
            payment.Code);

        Assert.Equal(1250.13m, payment.Amount);

        Assert.Equal(
            "CASH-001",
            payment.ReferenceNumber);

        Assert.Equal(
            "Pembeli Eceran",
            payment.ReceivedFrom);

        Assert.Equal(
            "Dibayar tunai",
            payment.Notes);
    }

    [Fact]
    public void Create_WithBlankOptionalText_ShouldUseNull()
    {
        var payment =
            SalePayment.Create(
                OrganizationId,
                SaleId,
                "PAY-NULL",
                PaymentDate,
                100,
                SalePaymentMethod.Other,
                " ",
                null,
                " ");

        Assert.Null(payment.ReferenceNumber);
        Assert.Null(payment.ReceivedFrom);
        Assert.Null(payment.Notes);
    }

    [Fact]
    public void Create_ShouldRoundAmountAwayFromZero()
    {
        var payment =
            SalePayment.Create(
                OrganizationId,
                SaleId,
                "PAY-ROUND",
                PaymentDate,
                10.005m,
                SalePaymentMethod.Cash,
                null,
                null,
                null);

        Assert.Equal(10.01m, payment.Amount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public void Create_WithNonPositiveRoundedAmount_ShouldThrow(
        double amount)
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    "PAY-AMOUNT",
                    PaymentDate,
                    (decimal)amount,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-PAY")]
    [InlineData("_PAY")]
    [InlineData(".PAY")]
    [InlineData("PAY/001")]
    [InlineData("PAY 001")]
    [InlineData("PAY@001")]
    public void Create_WithInvalidCode_ShouldThrow(
        string code)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    code,
                    PaymentDate,
                    100,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongCode_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    "A" + new string('B', 40),
                    PaymentDate,
                    100,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData(true, false, "organizationId")]
    [InlineData(false, true, "saleId")]
    public void Create_WithEmptyIdentifier_ShouldThrow(
        bool emptyOrganization,
        bool emptySale,
        string parameterName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                SalePayment.Create(
                    emptyOrganization
                        ? Guid.Empty
                        : OrganizationId,
                    emptySale
                        ? Guid.Empty
                        : SaleId,
                    "PAY-ID",
                    PaymentDate,
                    100,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Create_WithDefaultDate_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    "PAY-DATE",
                    default,
                    100,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal(
            "paymentDate",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedPaymentMethod_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    "PAY-METHOD",
                    PaymentDate,
                    100,
                    (SalePaymentMethod)999,
                    null,
                    null,
                    null));

        Assert.Equal(
            "paymentMethod",
            exception.ParamName);
    }

    [Theory]
    [InlineData("referenceNumber", 101)]
    [InlineData("receivedFrom", 151)]
    [InlineData("notes", 1001)]
    public void Create_WithTooLongOptionalText_ShouldThrow(
        string field,
        int length)
    {
        var value = new string('X', length);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                SalePayment.Create(
                    OrganizationId,
                    SaleId,
                    "PAY-TEXT",
                    PaymentDate,
                    100,
                    SalePaymentMethod.Cash,
                    field == "referenceNumber"
                        ? value
                        : null,
                    field == "receivedFrom"
                        ? value
                        : null,
                    field == "notes"
                        ? value
                        : null));

        Assert.Equal(field, exception.ParamName);
    }

    [Theory]
    [InlineData(SalePaymentMethod.Cash)]
    [InlineData(SalePaymentMethod.BankTransfer)]
    [InlineData(SalePaymentMethod.Other)]
    public void Create_WithSupportedPaymentMethod_ShouldPreserveIt(
        SalePaymentMethod paymentMethod)
    {
        var payment =
            SalePayment.Create(
                OrganizationId,
                SaleId,
                $"PAY-{(int)paymentMethod}",
                PaymentDate,
                100,
                paymentMethod,
                null,
                null,
                null);

        Assert.Equal(
            paymentMethod,
            payment.PaymentMethod);
    }

    [Fact]
    public void UpdateDraft_WithValidValues_ShouldUpdate()
    {
        var payment = CreatePayment();

        payment.UpdateDraft(
            new DateOnly(2027, 5, 25),
            250000.125m,
            SalePaymentMethod.Cash,
            "  CASH-002  ",
            "  Pembeli Cabang  ",
            "  Pelunasan sebagian  ");

        Assert.Equal(
            new DateOnly(2027, 5, 25),
            payment.PaymentDate);

        Assert.Equal(250000.13m, payment.Amount);

        Assert.Equal(
            SalePaymentMethod.Cash,
            payment.PaymentMethod);

        Assert.Equal(
            "CASH-002",
            payment.ReferenceNumber);

        Assert.Equal(
            "Pembeli Cabang",
            payment.ReceivedFrom);

        Assert.Equal(
            "Pelunasan sebagian",
            payment.Notes);

        Assert.NotNull(payment.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_ShouldNotChangeCodeOrOwnership()
    {
        var payment = CreatePayment();

        payment.UpdateDraft(
            PaymentDate.AddDays(1),
            200,
            SalePaymentMethod.Other,
            null,
            null,
            null);

        Assert.Equal("PAY-001", payment.Code);

        Assert.Equal(
            OrganizationId,
            payment.OrganizationId);

        Assert.Equal(SaleId, payment.SaleId);
    }

    [Fact]
    public void UpdateDraft_WithBlankOptionalText_ShouldUseNull()
    {
        var payment = CreatePayment();

        payment.UpdateDraft(
            PaymentDate,
            1000000,
            SalePaymentMethod.BankTransfer,
            " ",
            null,
            " ");

        Assert.Null(payment.ReferenceNumber);
        Assert.Null(payment.ReceivedFrom);
        Assert.Null(payment.Notes);
    }

    [Fact]
    public void UpdateDraft_WithSameValues_ShouldNotSetUpdatedAt()
    {
        var payment = CreatePayment();

        payment.UpdateDraft(
            PaymentDate,
            1000000,
            SalePaymentMethod.BankTransfer,
            "TRF-001",
            "Pembeli Utama",
            "Pembayaran tahap pertama");

        Assert.Null(payment.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_WithDefaultDate_ShouldThrow()
    {
        var payment = CreatePayment();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                payment.UpdateDraft(
                    default,
                    100,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal(
            "paymentDate",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public void UpdateDraft_WithInvalidAmount_ShouldThrow(
        double amount)
    {
        var payment = CreatePayment();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                payment.UpdateDraft(
                    PaymentDate,
                    (decimal)amount,
                    SalePaymentMethod.Cash,
                    null,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithUnsupportedPaymentMethod_ShouldThrow()
    {
        var payment = CreatePayment();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                payment.UpdateDraft(
                    PaymentDate,
                    100,
                    (SalePaymentMethod)999,
                    null,
                    null,
                    null));

        Assert.Equal(
            "paymentMethod",
            exception.ParamName);
    }

    [Fact]
    public void Confirm_ShouldRecognizeCollectedRevenue()
    {
        var payment = CreatePayment();
        var before = DateTime.UtcNow;

        payment.Confirm();

        var after = DateTime.UtcNow;

        Assert.Equal(
            SalePaymentStatus.Confirmed,
            payment.Status);

        Assert.True(payment.IsCollectedRevenue);
        Assert.NotNull(payment.ConfirmedAt);

        Assert.InRange(
            payment.ConfirmedAt!.Value,
            before,
            after);

        Assert.NotNull(payment.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var payment = CreateConfirmedPayment();

        Assert.Throws<InvalidOperationException>(
            payment.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenConfirmed_ShouldThrow()
    {
        var payment = CreateConfirmedPayment();

        Assert.Throws<InvalidOperationException>(() =>
            payment.UpdateDraft(
                PaymentDate,
                100,
                SalePaymentMethod.Cash,
                null,
                null,
                null));
    }

    [Fact]
    public void Cancel_FromDraft_ShouldCancel()
    {
        var payment = CreatePayment();

        payment.Cancel(
            "  Pembayaran tidak jadi diterima  ");

        Assert.Equal(
            SalePaymentStatus.Cancelled,
            payment.Status);

        Assert.Equal(
            "Pembayaran tidak jadi diterima",
            payment.CancellationReason);

        Assert.Null(payment.ConfirmedAt);
        Assert.False(payment.IsCollectedRevenue);
        Assert.NotNull(payment.UpdatedAt);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldPreserveConfirmation()
    {
        var payment = CreateConfirmedPayment();

        var confirmedAt = payment.ConfirmedAt;

        payment.Cancel(
            "Pembayaran dikoreksi");

        Assert.Equal(
            SalePaymentStatus.Cancelled,
            payment.Status);

        Assert.Equal(
            confirmedAt,
            payment.ConfirmedAt);

        Assert.False(payment.IsCollectedRevenue);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Cancel_WithBlankReason_ShouldThrow(
        string reason)
    {
        var payment = CreatePayment();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                payment.Cancel(reason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WithTooLongReason_ShouldThrow()
    {
        var payment = CreatePayment();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                payment.Cancel(
                    new string(
                        'X',
                        SalePayment
                            .MaxCancellationReasonLength +
                        1)));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var payment = CreatePayment();

        payment.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            payment.Cancel("Batal lagi"));
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrow()
    {
        var payment = CreatePayment();

        payment.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(
            payment.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenCancelled_ShouldThrow()
    {
        var payment = CreatePayment();

        payment.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            payment.UpdateDraft(
                PaymentDate,
                100,
                SalePaymentMethod.Cash,
                null,
                null,
                null));
    }

    private static SalePayment CreatePayment()
    {
        return SalePayment.Create(
            OrganizationId,
            SaleId,
            "  pay-001  ",
            PaymentDate,
            1000000,
            SalePaymentMethod.BankTransfer,
            "  TRF-001  ",
            "  Pembeli Utama  ",
            "  Pembayaran tahap pertama  ");
    }

    private static SalePayment CreateConfirmedPayment()
    {
        var payment = CreatePayment();

        payment.Confirm();

        return payment;
    }
}
