using SiPacul.Domain.Entities.Finance;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Finance;

public sealed class CultivationExpenseTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly DateOnly ExpenseDate =
        new(2027, 1, 10);

    [Fact]
    public void Create_WithValidValues_ShouldCreateDraft()
    {
        var expense = CreateExpense();

        Assert.NotEqual(Guid.Empty, expense.Id);
        Assert.Equal(OrganizationId, expense.OrganizationId);
        Assert.Equal(CropCycleId, expense.CropCycleId);
        Assert.Equal("EXP-001", expense.Code);
        Assert.Equal(ExpenseDate, expense.ExpenseDate);

        Assert.Equal(
            CultivationExpenseCategory.LandLease,
            expense.Category);

        Assert.Equal(
            "Sewa lahan satu musim",
            expense.Description);

        Assert.Equal(12500000m, expense.Amount);
        Assert.Equal("Pemilik Lahan", expense.PayeeName);
        Assert.Equal("AGR-001", expense.ReferenceNumber);

        Assert.Equal(
            "https://example.test/evidence/agr-001",
            expense.EvidenceUrl);

        Assert.Equal(
            "Dibayar melalui transfer",
            expense.Notes);

        Assert.Equal(
            CultivationExpenseStatus.Draft,
            expense.Status);

        Assert.Null(expense.ConfirmedAt);
        Assert.Null(expense.CancellationReason);
        Assert.False(expense.IsRecognizedCost);
    }

    [Fact]
    public void Create_ShouldNormalizeValues()
    {
        var expense =
            CultivationExpense.Create(
                OrganizationId,
                CropCycleId,
                "  exp.abc_01-x  ",
                ExpenseDate,
                CultivationExpenseCategory.Transport,
                "  Transport hasil panen  ",
                1250.125m,
                "  Koperasi Angkut  ",
                "  REF-01  ",
                "  https://example.test/ref-01  ",
                "  Dibayar tunai  ");

        Assert.Equal("EXP.ABC_01-X", expense.Code);

        Assert.Equal(
            "Transport hasil panen",
            expense.Description);

        Assert.Equal(1250.13m, expense.Amount);
        Assert.Equal("Koperasi Angkut", expense.PayeeName);
        Assert.Equal("REF-01", expense.ReferenceNumber);

        Assert.Equal(
            "https://example.test/ref-01",
            expense.EvidenceUrl);

        Assert.Equal("Dibayar tunai", expense.Notes);
    }

    [Fact]
    public void Create_WithBlankOptionalText_ShouldUseNull()
    {
        var expense =
            CultivationExpense.Create(
                OrganizationId,
                CropCycleId,
                "EXP-NULL",
                ExpenseDate,
                CultivationExpenseCategory.Other,
                "Biaya lain",
                100,
                " ",
                null,
                "",
                "   ");

        Assert.Null(expense.PayeeName);
        Assert.Null(expense.ReferenceNumber);
        Assert.Null(expense.EvidenceUrl);
        Assert.Null(expense.Notes);
    }

    [Fact]
    public void Create_ShouldRoundAmountAwayFromZero()
    {
        var expense =
            CultivationExpense.Create(
                OrganizationId,
                CropCycleId,
                "EXP-ROUND",
                ExpenseDate,
                CultivationExpenseCategory.Seed,
                "Pembelian benih",
                10.005m,
                null,
                null,
                null,
                null);

        Assert.Equal(10.01m, expense.Amount);
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
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-AMOUNT",
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    (decimal)amount,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-EXP")]
    [InlineData("_EXP")]
    [InlineData(".EXP")]
    [InlineData("EXP/001")]
    [InlineData("EXP 001")]
    [InlineData("EXP@001")]
    public void Create_WithInvalidCode_ShouldThrow(string code)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    code,
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongCode_ShouldThrow()
    {
        var code = "A" + new string('B', 40);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    code,
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("code", exception.ParamName);
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
                CultivationExpense.Create(
                    emptyOrganization
                        ? Guid.Empty
                        : OrganizationId,
                    emptyCropCycle
                        ? Guid.Empty
                        : CropCycleId,
                    "EXP-ID",
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    null,
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
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-DATE",
                    default,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal(
            "expenseDate",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithUnsupportedCategory_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-CATEGORY",
                    ExpenseDate,
                    (CultivationExpenseCategory)999,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("category", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithBlankDescription_ShouldThrow(
        string description)
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-DESC",
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    description,
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal(
            "description",
            exception.ParamName);
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldThrow()
    {
        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-DESC-LONG",
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    new string(
                        'X',
                        CultivationExpense
                            .MaxDescriptionLength +
                        1),
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal(
            "description",
            exception.ParamName);
    }

    [Theory]
    [InlineData("payeeName", 151)]
    [InlineData("referenceNumber", 101)]
    [InlineData("evidenceUrl", 1001)]
    [InlineData("notes", 1001)]
    public void Create_WithTooLongOptionalText_ShouldThrow(
        string field,
        int length)
    {
        var value = new string('X', length);

        var exception =
            Assert.Throws<ArgumentException>(() =>
                CultivationExpense.Create(
                    OrganizationId,
                    CropCycleId,
                    "EXP-TEXT",
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    field == "payeeName"
                        ? value
                        : null,
                    field == "referenceNumber"
                        ? value
                        : null,
                    field == "evidenceUrl"
                        ? value
                        : null,
                    field == "notes"
                        ? value
                        : null));

        Assert.Equal(field, exception.ParamName);
    }

    [Theory]
    [InlineData(CultivationExpenseCategory.LandLease)]
    [InlineData(CultivationExpenseCategory.Seed)]
    [InlineData(CultivationExpenseCategory.Fertilizer)]
    [InlineData(CultivationExpenseCategory.Pesticide)]
    [InlineData(CultivationExpenseCategory.Labor)]
    [InlineData(CultivationExpenseCategory.Equipment)]
    [InlineData(CultivationExpenseCategory.Irrigation)]
    [InlineData(CultivationExpenseCategory.Fuel)]
    [InlineData(CultivationExpenseCategory.Transport)]
    [InlineData(CultivationExpenseCategory.Storage)]
    [InlineData(CultivationExpenseCategory.Harvest)]
    [InlineData(CultivationExpenseCategory.PostHarvest)]
    [InlineData(CultivationExpenseCategory.Marketing)]
    [InlineData(CultivationExpenseCategory.Administration)]
    [InlineData(CultivationExpenseCategory.Other)]
    public void Create_WithSupportedCategory_ShouldPreserveCategory(
        CultivationExpenseCategory category)
    {
        var expense =
            CultivationExpense.Create(
                OrganizationId,
                CropCycleId,
                $"EXP-{(int)category}",
                ExpenseDate,
                category,
                "Biaya",
                100,
                null,
                null,
                null,
                null);

        Assert.Equal(category, expense.Category);
    }

    [Fact]
    public void UpdateDraft_WithValidValues_ShouldUpdate()
    {
        var expense = CreateExpense();

        expense.UpdateDraft(
            new DateOnly(2027, 1, 11),
            CultivationExpenseCategory.Administration,
            "  Administrasi kontrak  ",
            250000.125m,
            "  Kantor Desa  ",
            "  ADM-002  ",
            "  https://example.test/adm-002  ",
            "  Bukti lengkap  ");

        Assert.Equal(
            new DateOnly(2027, 1, 11),
            expense.ExpenseDate);

        Assert.Equal(
            CultivationExpenseCategory.Administration,
            expense.Category);

        Assert.Equal(
            "Administrasi kontrak",
            expense.Description);

        Assert.Equal(250000.13m, expense.Amount);
        Assert.Equal("Kantor Desa", expense.PayeeName);
        Assert.Equal("ADM-002", expense.ReferenceNumber);

        Assert.Equal(
            "https://example.test/adm-002",
            expense.EvidenceUrl);

        Assert.Equal("Bukti lengkap", expense.Notes);
        Assert.NotNull(expense.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_ShouldNotChangeCodeOrOwnership()
    {
        var expense = CreateExpense();

        expense.UpdateDraft(
            ExpenseDate,
            CultivationExpenseCategory.Other,
            "Biaya diperbarui",
            100,
            null,
            null,
            null,
            null);

        Assert.Equal("EXP-001", expense.Code);
        Assert.Equal(OrganizationId, expense.OrganizationId);
        Assert.Equal(CropCycleId, expense.CropCycleId);
    }

    [Fact]
    public void UpdateDraft_WithBlankOptionalText_ShouldUseNull()
    {
        var expense = CreateExpense();

        expense.UpdateDraft(
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan satu musim",
            12500000,
            " ",
            "",
            " ",
            null);

        Assert.Null(expense.PayeeName);
        Assert.Null(expense.ReferenceNumber);
        Assert.Null(expense.EvidenceUrl);
        Assert.Null(expense.Notes);
    }

    [Fact]
    public void UpdateDraft_WithSameValues_ShouldNotSetUpdatedAt()
    {
        var expense = CreateExpense();

        expense.UpdateDraft(
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "Sewa lahan satu musim",
            12500000,
            "Pemilik Lahan",
            "AGR-001",
            "https://example.test/evidence/agr-001",
            "Dibayar melalui transfer");

        Assert.Null(expense.UpdatedAt);
    }

    [Fact]
    public void UpdateDraft_WithDefaultDate_ShouldThrow()
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                expense.UpdateDraft(
                    default,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal(
            "expenseDate",
            exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithUnsupportedCategory_ShouldThrow()
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                expense.UpdateDraft(
                    ExpenseDate,
                    (CultivationExpenseCategory)999,
                    "Biaya",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("category", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public void UpdateDraft_WithInvalidAmount_ShouldThrow(
        double amount)
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                expense.UpdateDraft(
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    "Biaya",
                    (decimal)amount,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal("amount", exception.ParamName);
    }

    [Fact]
    public void UpdateDraft_WithBlankDescription_ShouldThrow()
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                expense.UpdateDraft(
                    ExpenseDate,
                    CultivationExpenseCategory.Other,
                    " ",
                    100,
                    null,
                    null,
                    null,
                    null));

        Assert.Equal(
            "description",
            exception.ParamName);
    }

    [Fact]
    public void Confirm_ShouldRecognizeExpense()
    {
        var expense = CreateExpense();
        var before = DateTime.UtcNow;

        expense.Confirm();

        var after = DateTime.UtcNow;

        Assert.Equal(
            CultivationExpenseStatus.Confirmed,
            expense.Status);

        Assert.True(expense.IsRecognizedCost);
        Assert.NotNull(expense.ConfirmedAt);

        Assert.InRange(
            expense.ConfirmedAt!.Value,
            before,
            after);

        Assert.NotNull(expense.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrow()
    {
        var expense = CreateConfirmedExpense();

        Assert.Throws<InvalidOperationException>(
            expense.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenConfirmed_ShouldThrow()
    {
        var expense = CreateConfirmedExpense();

        Assert.Throws<InvalidOperationException>(() =>
            expense.UpdateDraft(
                ExpenseDate,
                CultivationExpenseCategory.Other,
                "Biaya",
                100,
                null,
                null,
                null,
                null));
    }

    [Fact]
    public void Cancel_FromDraft_ShouldCancel()
    {
        var expense = CreateExpense();

        expense.Cancel("  Biaya tidak jadi dikeluarkan  ");

        Assert.Equal(
            CultivationExpenseStatus.Cancelled,
            expense.Status);

        Assert.Equal(
            "Biaya tidak jadi dikeluarkan",
            expense.CancellationReason);

        Assert.Null(expense.ConfirmedAt);
        Assert.False(expense.IsRecognizedCost);
        Assert.NotNull(expense.UpdatedAt);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldPreserveConfirmation()
    {
        var expense = CreateConfirmedExpense();
        var confirmedAt = expense.ConfirmedAt;

        expense.Cancel("Bukti pembayaran dibatalkan");

        Assert.Equal(
            CultivationExpenseStatus.Cancelled,
            expense.Status);

        Assert.Equal(
            confirmedAt,
            expense.ConfirmedAt);

        Assert.False(expense.IsRecognizedCost);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Cancel_WithBlankReason_ShouldThrow(
        string reason)
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                expense.Cancel(reason));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WithTooLongReason_ShouldThrow()
    {
        var expense = CreateExpense();

        var exception =
            Assert.Throws<ArgumentException>(() =>
                expense.Cancel(
                    new string(
                        'X',
                        CultivationExpense
                            .MaxCancellationReasonLength +
                        1)));

        Assert.Equal(
            "cancellationReason",
            exception.ParamName);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldThrow()
    {
        var expense = CreateExpense();
        expense.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            expense.Cancel("Batal lagi"));
    }

    [Fact]
    public void Confirm_WhenCancelled_ShouldThrow()
    {
        var expense = CreateExpense();
        expense.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(
            expense.Confirm);
    }

    [Fact]
    public void UpdateDraft_WhenCancelled_ShouldThrow()
    {
        var expense = CreateExpense();
        expense.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            expense.UpdateDraft(
                ExpenseDate,
                CultivationExpenseCategory.Other,
                "Biaya",
                100,
                null,
                null,
                null,
                null));
    }

    private static CultivationExpense CreateExpense()
    {
        return CultivationExpense.Create(
            OrganizationId,
            CropCycleId,
            "  exp-001  ",
            ExpenseDate,
            CultivationExpenseCategory.LandLease,
            "  Sewa lahan satu musim  ",
            12500000,
            "  Pemilik Lahan  ",
            "  AGR-001  ",
            "  https://example.test/evidence/agr-001  ",
            "  Dibayar melalui transfer  ");
    }

    private static CultivationExpense CreateConfirmedExpense()
    {
        var expense = CreateExpense();
        expense.Confirm();

        return expense;
    }
}
