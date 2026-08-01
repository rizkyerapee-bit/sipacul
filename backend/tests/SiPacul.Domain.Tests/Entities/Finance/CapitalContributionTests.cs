using SiPacul.Domain.Entities.Finance;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance;

public sealed class CapitalContributionTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly DateOnly ContributionDate =
        new(2027, 1, 8);

    [Fact]
    public void Create_WithValidValues_ShouldCreateDraft()
    {
        var contribution = CreateContribution();

        Assert.NotEqual(Guid.Empty, contribution.Id);
        Assert.Equal(
            OrganizationId,
            contribution.OrganizationId);

        Assert.Equal(
            CropCycleId,
            contribution.CropCycleId);

        Assert.Equal("CAP-001", contribution.Code);

        Assert.Equal(
            ContributionDate,
            contribution.ContributionDate);

        Assert.Equal(
            "INV-001",
            contribution.ContributorCode);

        Assert.Equal(
            "Investor Utama",
            contribution.ContributorName);

        Assert.Equal(
            CapitalContributorRole.Investor,
            contribution.ContributorRole);

        Assert.Equal(
            10000000m,
            contribution.Amount);

        Assert.Equal(
            CapitalContributionPaymentMethod.BankTransfer,
            contribution.PaymentMethod);

        Assert.Equal(
            "TRF-001",
            contribution.ReferenceNumber);

        Assert.Equal(
            "Modal tahap pertama",
            contribution.Notes);

        Assert.Equal(
            CapitalContributionStatus.Draft,
            contribution.Status);

        Assert.Null(contribution.ConfirmedAt);
        Assert.Null(contribution.CancellationReason);
        Assert.False(contribution.IsConfirmedCapital);
        Assert.True(contribution.IsInvestorCapital);
        Assert.False(contribution.IsPartnerCapital);
    }

    [Fact]
    public void Create_ShouldNormalizeValues()
    {
        var contribution =
            CapitalContribution.Create(
                OrganizationId,
                CropCycleId,
                "  cap.abc_01-x  ",
                ContributionDate,
                "  inv.budi_01-x  ",
                "  Budi Santoso  ",
                CapitalContributorRole.Investor,
                1250.125m,
                CapitalContributionPaymentMethod.Cash,
                "  CASH-001  ",
                "  Setoran tunai  ");

        Assert.Equal(
            "CAP.ABC_01-X",
            contribution.Code);

        Assert.Equal(
            "INV.BUDI_01-X",
            contribution.ContributorCode);

        Assert.Equal(
            "Budi Santoso",
            contribution.ContributorName);

        Assert.Equal(1250.13m, contribution.Amount);

        Assert.Equal(
            "CASH-001",
            contribution.ReferenceNumber);

        Assert.Equal(
            "Setoran tunai",
            contribution.Notes);
    }

    [Fact]
    public void Create_WithBlankOptionalText_ShouldUseNull()
    {
        var contribution =
            CapitalContribution.Create(
                OrganizationId,
                CropCycleId,
                "CAP-NULL",
                ContributionDate,
                "MITRA-001",
                "Mitra Utama",
                CapitalContributorRole.Partner,
                100,
                CapitalContributionPaymentMethod.Other,
                " ",
                null);

        Assert.Null(contribution.ReferenceNumber);
        Assert.Null(contribution.Notes);
    }

    [Fact]
    public void Create_ShouldRoundAmountAwayFromZero()
    {
        var contribution =
            CapitalContribution.Create(
                OrganizationId,
                CropCycleId,
                "CAP-ROUND",
                ContributionDate,
                "INV-ROUND",
                "Investor",
                CapitalContributorRole.Investor,
                10.005m,
                CapitalContributionPaymentMethod.Cash,
                null,
                null);

        Assert.Equal(10.01m, contribution.Amount);
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
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-AMOUNT",
                    ContributionDate,
                    "INV-AMOUNT",
                    "Investor",
                    CapitalContributorRole.Investor,
                    (decimal)amount,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-CAP")]
    [InlineData("_CAP")]
    [InlineData(".CAP")]
    [InlineData("CAP/001")]
    [InlineData("CAP 001")]
    [InlineData("CAP@001")]
    public void Create_WithInvalidTransactionCode_ShouldThrow(
        string code)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    code,
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-INV")]
    [InlineData("_INV")]
    [InlineData(".INV")]
    [InlineData("INV/001")]
    [InlineData("INV 001")]
    [InlineData("INV@001")]
    public void Create_WithInvalidContributorCode_ShouldThrow(
        string contributorCode)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-001",
                    ContributionDate,
                    contributorCode,
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorCode",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongTransactionCode_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "A" + new string('B', 40),
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongContributorCode_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-001",
                    ContributionDate,
                    "A" + new string('B', 40),
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorCode",
            exception.ParamName);
    }

    [Theory]
    [InlineData(true, false, "organizationId")]
    [InlineData(false, true, "cropCycleId")]
    public void Create_WithEmptyIdentifier_ShouldThrow(
        bool emptyOrganization,
        bool emptyCropCycle,
        string parameterName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    emptyOrganization
                        ? Guid.Empty
                        : OrganizationId,
                    emptyCropCycle
                        ? Guid.Empty
                        : CropCycleId,
                    "CAP-ID",
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    public void Create_WithDefaultDate_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-DATE",
                    default,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributionDate",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedRole_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-ROLE",
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    (CapitalContributorRole)999,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorRole",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedPaymentMethod_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-METHOD",
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    (CapitalContributionPaymentMethod)999,
                    null,
                    null));

        Assert.Equal(
            "paymentMethod",
            exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithBlankContributorName_ShouldThrow(
        string contributorName)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-NAME",
                    ContributionDate,
                    "INV-001",
                    contributorName,
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorName",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongContributorName_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-NAME-LONG",
                    ContributionDate,
                    "INV-001",
                    new string(
                        'X',
                        CapitalContribution
                            .MaxContributorNameLength +
                        1),
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorName",
            exception.ParamName);
    }

    [Theory]
    [InlineData("referenceNumber", 101)]
    [InlineData("notes", 1001)]
    public void Create_WithTooLongOptionalText_ShouldThrow(
        string field,
        int length)
    {
        var value = new string('X', length);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CapitalContribution.Create(
                    OrganizationId,
                    CropCycleId,
                    "CAP-TEXT",
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    field == "referenceNumber"
                        ? value
                        : null,
                    field == "notes"
                        ? value
                        : null));

        Assert.Equal(field, exception.ParamName);
    }

    [Theory]
    [InlineData(CapitalContributorRole.Investor, true, false)]
    [InlineData(CapitalContributorRole.Partner, false, true)]
    public void Create_ShouldExposeRoleFlags(
        CapitalContributorRole role,
        bool expectedInvestor,
        bool expectedPartner)
    {
        var contribution =
            CapitalContribution.Create(
                OrganizationId,
                CropCycleId,
                $"CAP-{(int)role}",
                ContributionDate,
                $"CONTRIBUTOR-{(int)role}",
                "Contributor",
                role,
                100,
                CapitalContributionPaymentMethod.Cash,
                null,
                null);

        Assert.Equal(
            expectedInvestor,
            contribution.IsInvestorCapital);

        Assert.Equal(
            expectedPartner,
            contribution.IsPartnerCapital);
    }

    [Theory]
    [InlineData(CapitalContributionPaymentMethod.Cash)]
    [InlineData(CapitalContributionPaymentMethod.BankTransfer)]
    [InlineData(CapitalContributionPaymentMethod.Other)]
    public void Create_WithSupportedPaymentMethod_ShouldPreserveIt(
        CapitalContributionPaymentMethod paymentMethod)
    {
        var contribution =
            CapitalContribution.Create(
                OrganizationId,
                CropCycleId,
                $"CAP-{(int)paymentMethod}",
                ContributionDate,
                "INV-001",
                "Investor",
                CapitalContributorRole.Investor,
                100,
                paymentMethod,
                null,
                null);

        Assert.Equal(
            paymentMethod,
            contribution.PaymentMethod);
    }

    [Fact]
    public void UpdateDraft_WithValidValues_ShouldUpdate()
    {
        var contribution = CreateContribution();

        contribution.UpdateDraft(
            new DateOnly(2027, 1, 15),
            "  mitra-001  ",
            "  Mitra Pengelola  ",
            CapitalContributorRole.Partner,
            2500000.125m,
            CapitalContributionPaymentMethod.Cash,
            "  CASH-002  ",
            "  Modal Mitra  ");

        Assert.Equal(
            new DateOnly(2027, 1, 15),
            contribution.ContributionDate);

        Assert.Equal(
            "MITRA-001",
            contribution.ContributorCode);

        Assert.Equal(
            "Mitra Pengelola",
            contribution.ContributorName);

        Assert.Equal(
            CapitalContributorRole.Partner,
            contribution.ContributorRole);

        Assert.Equal(
            2500000.13m,
            contribution.Amount);

        Assert.Equal(
            CapitalContributionPaymentMethod.Cash,
            contribution.PaymentMethod);

        Assert.Equal(
            "CASH-002",
            contribution.ReferenceNumber);

        Assert.Equal(
            "Modal Mitra",
            contribution.Notes);

        Assert.True(contribution.IsPartnerCapital);
        Assert.False(contribution.IsInvestorCapital);
        Assert.NotNull(contribution.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_ShouldNotChangeCodeOrOwnership()
    {
        var contribution = CreateContribution();

        contribution.UpdateDraft(
            ContributionDate,
            "MITRA-001",
            "Mitra",
            CapitalContributorRole.Partner,
            100,
            CapitalContributionPaymentMethod.Other,
            null,
            null);

        Assert.Equal("CAP-001", contribution.Code);

        Assert.Equal(
            OrganizationId,
            contribution.OrganizationId);

        Assert.Equal(
            CropCycleId,
            contribution.CropCycleId);
    }

    [Fact]
    public void UpdateDraft_WithBlankOptionalText_ShouldUseNull()
    {
        var contribution = CreateContribution();

        contribution.UpdateDraft(
            ContributionDate,
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor,
            10000000,
            CapitalContributionPaymentMethod.BankTransfer,
            " ",
            null);

        Assert.Null(contribution.ReferenceNumber);
        Assert.Null(contribution.Notes);
    }

    [Fact]
    public void UpdateDraft_WithSameValues_ShouldNotSetUpdatedAt()
    {
        var contribution = CreateContribution();

        contribution.UpdateDraft(
            ContributionDate,
            "INV-001",
            "Investor Utama",
            CapitalContributorRole.Investor,
            10000000,
            CapitalContributionPaymentMethod.BankTransfer,
            "TRF-001",
            "Modal tahap pertama");

        Assert.Null(contribution.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_WithDefaultDate_ShouldThrow()
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                contribution.UpdateDraft(
                    default,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributionDate",
            exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithUnsupportedRole_ShouldThrow()
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                contribution.UpdateDraft(
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    (CapitalContributorRole)999,
                    100,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal(
            "contributorRole",
            exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithUnsupportedPaymentMethod_ShouldThrow()
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                contribution.UpdateDraft(
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    100,
                    (CapitalContributionPaymentMethod)999,
                    null,
                    null));

        Assert.Equal(
            "paymentMethod",
            exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public void UpdateDraft_WithInvalidAmount_ShouldThrow(
        double amount)
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                contribution.UpdateDraft(
                    ContributionDate,
                    "INV-001",
                    "Investor",
                    CapitalContributorRole.Investor,
                    (decimal)amount,
                    CapitalContributionPaymentMethod.Cash,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void Confirm_ShouldRecognizeCapital()
    {
        var contribution = CreateContribution();
        var before = DateTime.UtcNow;

        contribution.Confirm();

        var after = DateTime.UtcNow;

        Assert.Equal(
            CapitalContributionStatus.Confirmed,
            contribution.Status);

        Assert.True(contribution.IsConfirmedCapital);
        Assert.NotNull(contribution.ConfirmedAt);

        Assert.InRange(
            contribution.ConfirmedAt!.Value,
            before,
            after);

        Assert.NotNull(contribution.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var contribution = CreateConfirmedContribution();

        Assert.Throws<InvalidOperationException>(
            contribution.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenConfirmed_ShouldThrow()
    {
        var contribution = CreateConfirmedContribution();

        Assert.Throws<InvalidOperationException>(() =>
            contribution.UpdateDraft(
                ContributionDate,
                "INV-001",
                "Investor",
                CapitalContributorRole.Investor,
                100,
                CapitalContributionPaymentMethod.Cash,
                null,
                null));
    }

    [Fact]
    public void Cancel_FromDraft_ShouldCancel()
    {
        var contribution = CreateContribution();

        contribution.Cancel(
            "  Modal tidak jadi diterapkan  ");

        Assert.Equal(
            CapitalContributionStatus.Cancelled,
            contribution.Status);

        Assert.Equal(
            "Modal tidak jadi diterapkan",
            contribution.CancellationReason);

        Assert.Null(contribution.ConfirmedAt);
        Assert.False(contribution.IsConfirmedCapital);
        Assert.NotNull(contribution.UpdatedAt);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldPreserveConfirmation()
    {
        var contribution =
            CreateConfirmedContribution();

        var confirmedAt =
            contribution.ConfirmedAt;

        contribution.Cancel(
            "Kontribusi dikoreksi");

        Assert.Equal(
            CapitalContributionStatus.Cancelled,
            contribution.Status);

        Assert.Equal(
            confirmedAt,
            contribution.ConfirmedAt);

        Assert.False(contribution.IsConfirmedCapital);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Cancel_WithBlankReason_ShouldThrow(
        string reason)
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                contribution.Cancel(reason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WithTooLongReason_ShouldThrow()
    {
        var contribution = CreateContribution();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                contribution.Cancel(
                    new string(
                        'X',
                        CapitalContribution
                            .MaxCancellationReasonLength +
                        1)));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var contribution = CreateContribution();

        contribution.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            contribution.Cancel("Batal lagi"));
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrow()
    {
        var contribution = CreateContribution();

        contribution.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(
            contribution.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenCancelled_ShouldThrow()
    {
        var contribution = CreateContribution();

        contribution.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            contribution.UpdateDraft(
                ContributionDate,
                "INV-001",
                "Investor",
                CapitalContributorRole.Investor,
                100,
                CapitalContributionPaymentMethod.Cash,
                null,
                null));
    }

    private static CapitalContribution CreateContribution()
    {
        return CapitalContribution.Create(
            OrganizationId,
            CropCycleId,
            "  cap-001  ",
            ContributionDate,
            "  inv-001  ",
            "  Investor Utama  ",
            CapitalContributorRole.Investor,
            10000000,
            CapitalContributionPaymentMethod.BankTransfer,
            "  TRF-001  ",
            "  Modal tahap pertama  ");
    }

    private static CapitalContribution
        CreateConfirmedContribution()
    {
        var contribution = CreateContribution();

        contribution.Confirm();

        return contribution;
    }
}
