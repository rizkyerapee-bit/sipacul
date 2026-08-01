using SiPacul.Domain.Entities.Harvests;
using SiPacul.Domain.Entities.Sales;
using Xunit;

namespace SiPacul.Domain.Tests.Entities.Sales;

public sealed class SaleTests
{
    private static readonly Guid OrganizationId =
        Guid.Parse(
            "10000000-0000-0000-0000-000000000001");

    private static readonly Guid HarvestBatchId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000001");

    private static readonly Guid SecondHarvestBatchId =
        Guid.Parse(
            "20000000-0000-0000-0000-000000000002");

    private static readonly Guid CropCycleId =
        Guid.Parse(
            "30000000-0000-0000-0000-000000000001");

    private static readonly Guid CommodityId =
        Guid.Parse(
            "40000000-0000-0000-0000-000000000001");

    private static readonly DateOnly SaleDate =
        new(2027, 5, 10);

    [Fact]
    public void Create_WithValidValues_ShouldNormalizeHeader()
    {
        var sale = CreateSale();

        Assert.NotEqual(Guid.Empty, sale.Id);
        Assert.Equal(OrganizationId, sale.OrganizationId);
        Assert.Equal("SALE-2027-0001", sale.Code);
        Assert.Equal(SaleDate, sale.SaleDate);
        Assert.Equal("Koperasi Tani", sale.BuyerName);
        Assert.Equal("08123456789", sale.BuyerPhone);
        Assert.Equal("Jl. Pertanian 10", sale.BuyerAddress);
        Assert.Equal(SalePaymentTerm.Cash, sale.PaymentTerm);
        Assert.Null(sale.DueDate);
        Assert.Equal(0m, sale.DiscountAmount);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal(0m, sale.TotalAmount);
        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.False(sale.IsRevenue);
        Assert.Empty(sale.Lines);
        Assert.Equal("Catatan penjualan", sale.Notes);
    }

    [Fact]
    public void Create_WithBlankOptionalTexts_ShouldUseNull()
    {
        var sale = Sale.Create(
            OrganizationId,
            "SALE-001",
            SaleDate,
            "Pembeli",
            " ",
            null,
            SalePaymentTerm.Cash,
            null,
            0,
            " ");

        Assert.Null(sale.BuyerPhone);
        Assert.Null(sale.BuyerAddress);
        Assert.Null(sale.Notes);
    }

    [Fact]
    public void Create_WithEmptyOrganization_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                Guid.Empty,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithEmptyCode_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                " ",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithUnsupportedCodeCharacters_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE 001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithCodeOverLimit_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                new string(
                    'A',
                    Sale.MaxCodeLength + 1),
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithDefaultDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                default,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithEmptyBuyerName_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                " ",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithBuyerNameOverLimit_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                new string(
                    'A',
                    Sale.MaxBuyerNameLength + 1),
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithBuyerPhoneOverLimit_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                new string(
                    '1',
                    Sale.MaxBuyerPhoneLength + 1),
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithBuyerAddressOverLimit_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                new string(
                    'A',
                    Sale.MaxBuyerAddressLength + 1),
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithUnsupportedPaymentTerm_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                (SalePaymentTerm)999,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_CreditWithoutDueDate_ShouldThrow()
    {
        Assert.Throws<ArgumentException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Credit,
                null,
                0,
                null));
    }

    [Fact]
    public void Create_WithDueDateBeforeSaleDate_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                SaleDate.AddDays(-1),
                0,
                null));
    }

    [Fact]
    public void Create_CashWithSameDayDueDate_ShouldSucceed()
    {
        var sale = Sale.Create(
            OrganizationId,
            "SALE-001",
            SaleDate,
            "Pembeli",
            null,
            null,
            SalePaymentTerm.Cash,
            SaleDate,
            0,
            null);

        Assert.Equal(SaleDate, sale.DueDate);
    }

    [Fact]
    public void Create_CreditWithSameDayDueDate_ShouldSucceed()
    {
        var sale = Sale.Create(
            OrganizationId,
            "SALE-001",
            SaleDate,
            "Pembeli",
            null,
            null,
            SalePaymentTerm.Credit,
            SaleDate,
            0,
            null);

        Assert.Equal(SalePaymentTerm.Credit, sale.PaymentTerm);
        Assert.Equal(SaleDate, sale.DueDate);
    }

    [Fact]
    public void Create_WithNegativeDiscount_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                -1,
                null));
    }

    [Fact]
    public void Create_WithDiscountBeforeLines_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Sale.Create(
                OrganizationId,
                "SALE-001",
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                1,
                null));
    }

    [Fact]
    public void AddLine_WithValidValues_ShouldCalculateAmounts()
    {
        var sale = CreateSale();

        var line = AddDefaultLine(sale);

        Assert.NotEqual(Guid.Empty, line.Id);
        Assert.Equal(OrganizationId, line.OrganizationId);
        Assert.Equal(sale.Id, line.SaleId);
        Assert.Equal(HarvestBatchId, line.HarvestBatchId);
        Assert.Equal("HRV-001", line.HarvestBatchCodeSnapshot);
        Assert.Equal(CropCycleId, line.CropCycleIdSnapshot);
        Assert.Equal("CC-001", line.CropCycleCodeSnapshot);
        Assert.Equal(CommodityId, line.CommodityIdSnapshot);
        Assert.Equal("PADI", line.CommodityCodeSnapshot);
        Assert.Equal("Padi", line.CommodityNameSnapshot);
        Assert.Equal("Grade A", line.QualityGradeSnapshot);
        Assert.Equal(10.1235m, line.Quantity);
        Assert.Equal(HarvestQuantityUnit.Kilogram, line.QuantityUnit);
        Assert.Equal(2500.13m, line.UnitPrice);
        Assert.Equal(100.13m, line.LineDiscount);
        Assert.Equal(25209.94m, line.LineTotal);
        Assert.Equal(25209.94m, sale.Subtotal);
        Assert.Equal(25209.94m, sale.TotalAmount);
        Assert.Single(sale.Lines);
    }

    [Fact]
    public void AddLine_WithMultipleLines_ShouldSumTotals()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        sale.AddLine(
            SecondHarvestBatchId,
            "HRV-002",
            CropCycleId,
            "CC-001",
            CommodityId,
            "PADI",
            "Padi",
            "Grade B",
            5,
            HarvestQuantityUnit.Kilogram,
            2000,
            500,
            null);

        Assert.Equal(34709.94m, sale.Subtotal);
        Assert.Equal(34709.94m, sale.TotalAmount);
        Assert.Equal(2, sale.Lines.Count);
    }

    [Fact]
    public void AddLine_WithDuplicateHarvestBatch_ShouldThrow()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        Assert.Throws<InvalidOperationException>(() =>
            AddDefaultLine(sale));
    }

    [Fact]
    public void AddLine_WithEmptyHarvestBatchId_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentException>(() =>
            AddDefaultLine(
                sale,
                harvestBatchId: Guid.Empty));
    }

    [Fact]
    public void AddLine_WithEmptyCropCycleSnapshotId_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentException>(() =>
            AddDefaultLine(
                sale,
                cropCycleId: Guid.Empty));
    }

    [Fact]
    public void AddLine_WithEmptyCommoditySnapshotId_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentException>(() =>
            AddDefaultLine(
                sale,
                commodityId: Guid.Empty));
    }

    [Fact]
    public void AddLine_WithUnsupportedUnit_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AddDefaultLine(
                sale,
                quantityUnit:
                    (HarvestQuantityUnit)999));
    }

    [Fact]
    public void AddLine_WithZeroQuantity_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AddDefaultLine(
                sale,
                quantity: 0));
    }

    [Fact]
    public void AddLine_WithNegativeUnitPrice_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AddDefaultLine(
                sale,
                unitPrice: -1));
    }

    [Fact]
    public void AddLine_WithNegativeDiscount_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AddDefaultLine(
                sale,
                lineDiscount: -1));
    }

    [Fact]
    public void AddLine_WithDiscountAboveGross_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            AddDefaultLine(
                sale,
                quantity: 1,
                unitPrice: 100,
                lineDiscount: 101));
    }

    [Fact]
    public void AddLine_ShouldNormalizeSnapshotsAndNotes()
    {
        var sale = CreateSale();

        var line = sale.AddLine(
            HarvestBatchId,
            "  HRV-001  ",
            CropCycleId,
            "  CC-001  ",
            CommodityId,
            "  PADI  ",
            "  Padi  ",
            "  Grade A  ",
            1,
            HarvestQuantityUnit.Kilogram,
            100,
            0,
            "  Catatan baris  ");

        Assert.Equal("HRV-001", line.HarvestBatchCodeSnapshot);
        Assert.Equal("CC-001", line.CropCycleCodeSnapshot);
        Assert.Equal("PADI", line.CommodityCodeSnapshot);
        Assert.Equal("Padi", line.CommodityNameSnapshot);
        Assert.Equal("Grade A", line.QualityGradeSnapshot);
        Assert.Equal("Catatan baris", line.Notes);
    }

    [Fact]
    public void UpdateLine_WithValidValues_ShouldRecalculateTotals()
    {
        var sale = CreateSale();
        var line = AddDefaultLine(sale);

        sale.UpdateLine(
            line.Id,
            20,
            3000,
            1000,
            "Diperbarui");

        Assert.Equal(20m, line.Quantity);
        Assert.Equal(3000m, line.UnitPrice);
        Assert.Equal(1000m, line.LineDiscount);
        Assert.Equal(59000m, line.LineTotal);
        Assert.Equal(59000m, sale.Subtotal);
        Assert.Equal(59000m, sale.TotalAmount);
        Assert.Equal("Diperbarui", line.Notes);
        Assert.NotNull(line.UpdatedAt);
    }

    [Fact]
    public void UpdateLine_WhenMissing_ShouldThrow()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateLine(
                Guid.NewGuid(),
                1,
                100,
                0,
                null));
    }

    [Fact]
    public void UpdateLine_WhenNewSubtotalBelowSaleDiscount_ShouldThrow()
    {
        var sale = CreateSale();
        var line = AddDefaultLine(sale);

        sale.UpdateDraft(
            SaleDate,
            "Pembeli",
            null,
            null,
            SalePaymentTerm.Cash,
            null,
            25000,
            null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.UpdateLine(
                line.Id,
                1,
                100,
                0,
                null));

        Assert.Equal(25209.94m, sale.Subtotal);
        Assert.Equal(25209.94m, line.LineTotal);
    }

    [Fact]
    public void RemoveLine_ShouldRecalculateTotals()
    {
        var sale = CreateSale();
        var first = AddDefaultLine(sale);

        var second = sale.AddLine(
            SecondHarvestBatchId,
            "HRV-002",
            CropCycleId,
            "CC-001",
            CommodityId,
            "PADI",
            "Padi",
            null,
            5,
            HarvestQuantityUnit.Kilogram,
            2000,
            0,
            null);

        sale.RemoveLine(first.Id);

        Assert.Single(sale.Lines);
        Assert.Equal(second.Id, sale.Lines.Single().Id);
        Assert.Equal(10000m, sale.Subtotal);
        Assert.Equal(10000m, sale.TotalAmount);
    }

    [Fact]
    public void RemoveLine_WhenMissing_ShouldThrow()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        Assert.Throws<InvalidOperationException>(() =>
            sale.RemoveLine(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveLine_WhenSubtotalWouldFallBelowDiscount_ShouldThrow()
    {
        var sale = CreateSale();
        var line = AddDefaultLine(sale);

        sale.UpdateDraft(
            SaleDate,
            "Pembeli",
            null,
            null,
            SalePaymentTerm.Cash,
            null,
            100,
            null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.RemoveLine(line.Id));

        Assert.Single(sale.Lines);
    }

    [Fact]
    public void UpdateDraft_WithValidValues_ShouldUpdateHeaderAndDiscount()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        sale.UpdateDraft(
            SaleDate.AddDays(1),
            "  Pedagang Besar  ",
            "  089999  ",
            "  Pasar Induk  ",
            SalePaymentTerm.Credit,
            SaleDate.AddDays(30),
            209.94m,
            "  Kredit 30 hari  ");

        Assert.Equal(SaleDate.AddDays(1), sale.SaleDate);
        Assert.Equal("Pedagang Besar", sale.BuyerName);
        Assert.Equal("089999", sale.BuyerPhone);
        Assert.Equal("Pasar Induk", sale.BuyerAddress);
        Assert.Equal(SalePaymentTerm.Credit, sale.PaymentTerm);
        Assert.Equal(SaleDate.AddDays(30), sale.DueDate);
        Assert.Equal(209.94m, sale.DiscountAmount);
        Assert.Equal(25000m, sale.TotalAmount);
        Assert.Equal("Kredit 30 hari", sale.Notes);
    }

    [Fact]
    public void UpdateDraft_WithInvalidCreditDueDate_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentException>(() =>
            sale.UpdateDraft(
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Credit,
                null,
                0,
                null));
    }

    [Fact]
    public void UpdateDraft_WithDiscountEqualSubtotal_ShouldAllowZeroTotal()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        sale.UpdateDraft(
            SaleDate,
            "Pembeli",
            null,
            null,
            SalePaymentTerm.Cash,
            null,
            sale.Subtotal,
            null);

        Assert.Equal(0m, sale.TotalAmount);
    }

    [Fact]
    public void UpdateDraft_WithDiscountAboveSubtotal_ShouldThrow()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sale.UpdateDraft(
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                sale.Subtotal + 0.01m,
                null));
    }

    [Fact]
    public void Confirm_WithoutLines_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<InvalidOperationException>(
            sale.Confirm);
    }

    [Fact]
    public void Confirm_WithLine_ShouldSetConfirmedState()
    {
        var sale = CreateSale();
        AddDefaultLine(sale);

        sale.Confirm();

        Assert.Equal(SaleStatus.Confirmed, sale.Status);
        Assert.NotNull(sale.ConfirmedAt);
        Assert.True(sale.IsRevenue);
    }

    [Fact]
    public void Confirm_Twice_ShouldThrow()
    {
        var sale = CreateConfirmedSale();

        Assert.Throws<InvalidOperationException>(
            sale.Confirm);
    }

    [Fact]
    public void UpdateDraft_AfterConfirmation_ShouldThrow()
    {
        var sale = CreateConfirmedSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateDraft(
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));
    }

    [Fact]
    public void AddLine_AfterConfirmation_ShouldThrow()
    {
        var sale = CreateConfirmedSale();

        Assert.Throws<InvalidOperationException>(() =>
            sale.AddLine(
                SecondHarvestBatchId,
                "HRV-002",
                CropCycleId,
                "CC-001",
                CommodityId,
                "PADI",
                "Padi",
                null,
                1,
                HarvestQuantityUnit.Kilogram,
                100,
                0,
                null));
    }

    [Fact]
    public void UpdateLine_AfterConfirmation_ShouldThrow()
    {
        var sale = CreateConfirmedSale();
        var line = sale.Lines.Single();

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateLine(
                line.Id,
                1,
                100,
                0,
                null));
    }

    [Fact]
    public void RemoveLine_AfterConfirmation_ShouldThrow()
    {
        var sale = CreateConfirmedSale();
        var line = sale.Lines.Single();

        Assert.Throws<InvalidOperationException>(() =>
            sale.RemoveLine(line.Id));
    }

    [Fact]
    public void Cancel_FromDraft_ShouldSetCancelledState()
    {
        var sale = CreateSale();

        sale.Cancel("  Transaksi batal  ");

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal("Transaksi batal", sale.CancellationReason);
        Assert.False(sale.IsRevenue);
    }

    [Fact]
    public void Cancel_FromConfirmed_ShouldPreserveConfirmationHistory()
    {
        var sale = CreateConfirmedSale();
        var confirmedAt = sale.ConfirmedAt;

        sale.Cancel("Pembeli membatalkan");

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal(confirmedAt, sale.ConfirmedAt);
        Assert.False(sale.IsRevenue);
        Assert.Single(sale.Lines);
    }

    [Fact]
    public void Cancel_WithEmptyReason_ShouldThrow()
    {
        var sale = CreateSale();

        Assert.Throws<ArgumentException>(() =>
            sale.Cancel(" "));
    }

    [Fact]
    public void Cancel_Twice_ShouldThrow()
    {
        var sale = CreateSale();
        sale.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            sale.Cancel("Batal lagi"));
    }

    [Fact]
    public void CancelledSale_ShouldRemainTerminal()
    {
        var sale = CreateSale();
        sale.Cancel("Batal");

        Assert.Throws<InvalidOperationException>(() =>
            sale.UpdateDraft(
                SaleDate,
                "Pembeli",
                null,
                null,
                SalePaymentTerm.Cash,
                null,
                0,
                null));

        Assert.Throws<InvalidOperationException>(
            sale.Confirm);

        Assert.Throws<InvalidOperationException>(() =>
            AddDefaultLine(sale));
    }

    [Theory]
    [InlineData(HarvestQuantityUnit.Kilogram)]
    [InlineData(HarvestQuantityUnit.Ton)]
    [InlineData(HarvestQuantityUnit.Quintal)]
    [InlineData(HarvestQuantityUnit.Piece)]
    [InlineData(HarvestQuantityUnit.Bunch)]
    [InlineData(HarvestQuantityUnit.Sack)]
    [InlineData(HarvestQuantityUnit.Crate)]
    [InlineData(HarvestQuantityUnit.Liter)]
    public void AddLine_WithSupportedUnit_ShouldPreserveUnit(
        HarvestQuantityUnit unit)
    {
        var sale = CreateSale();

        var line = AddDefaultLine(
            sale,
            quantityUnit: unit);

        Assert.Equal(unit, line.QuantityUnit);
    }

    private static Sale CreateSale()
    {
        return Sale.Create(
            OrganizationId,
            "  sale-2027-0001  ",
            SaleDate,
            "  Koperasi Tani  ",
            "  08123456789  ",
            "  Jl. Pertanian 10  ",
            SalePaymentTerm.Cash,
            null,
            0,
            "  Catatan penjualan  ");
    }

    private static Sale CreateConfirmedSale()
    {
        var sale = CreateSale();

        AddDefaultLine(sale);
        sale.Confirm();

        return sale;
    }

    private static SaleLine AddDefaultLine(
        Sale sale,
        Guid? harvestBatchId = null,
        Guid? cropCycleId = null,
        Guid? commodityId = null,
        decimal quantity = 10.12345m,
        HarvestQuantityUnit quantityUnit =
            HarvestQuantityUnit.Kilogram,
        decimal unitPrice = 2500.125m,
        decimal lineDiscount = 100.125m)
    {
        return sale.AddLine(
            harvestBatchId ?? HarvestBatchId,
            "HRV-001",
            cropCycleId ?? CropCycleId,
            "CC-001",
            commodityId ?? CommodityId,
            "PADI",
            "Padi",
            "Grade A",
            quantity,
            quantityUnit,
            unitPrice,
            lineDiscount,
            null);
    }
}
