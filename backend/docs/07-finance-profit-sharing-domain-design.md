# Sprint 18 - Finance and Profit-Sharing Domain Design

## 1. Tujuan

Sprint 18 mengubah data operasional SiPacul menjadi informasi keuangan
per Siklus Budidaya.

Modul harus dapat:

- menghitung biaya aktual;
- menghitung pendapatan dari Sale Confirmed;
- membedakan pendapatan dan pembayaran pelanggan;
- mencatat modal Investor dan Mitra;
- menghitung laba, impas, atau rugi;
- menghitung pengembalian modal;
- menghitung pembagian keuntungan;
- menyimpan settlement yang dapat diaudit;
- menyediakan data untuk evaluasi musim pada Sprint 19.

Istilah antarmuka:

```text
CultivationExpense       = Biaya Budidaya
CapitalContribution      = Kontribusi Modal
SalePayment              = Pembayaran Penjualan
ProfitabilityReport      = Laporan Laba Rugi
ProfitSharingSettlement  = Penyelesaian Bagi Hasil
ProfitSharingAllocation  = Alokasi Bagi Hasil
```

Nama kode tetap menggunakan bahasa Inggris agar konsisten dengan backend.

Modul tetap organization-scoped dan disiapkan untuk multi-tenant SaaS.

---

## 2. Ruang lingkup

Sprint 18 mencakup:

- biaya dari CultivationActivityResource;
- biaya tambahan langsung pada Crop Cycle;
- kontribusi modal Investor dan Mitra;
- pembayaran Sale tunai dan kredit;
- saldo piutang;
- alokasi Sale lintas Crop Cycle;
- alokasi diskon header Sale;
- laporan profitabilitas;
- pengembalian modal;
- pembagian keuntungan;
- kondisi profit, break-even, dan loss;
- settlement draft, finalized, dan voided;
- source locking setelah settlement final;
- persistence PostgreSQL;
- HTTP API;
- automated tests dan E2E.

Ditunda:

- general ledger;
- double-entry journal;
- chart of accounts;
- rekonsiliasi bank;
- pajak;
- supplier payable;
- payment gateway;
- penyusutan aset;
- inventory valuation;
- foreign currency;
- transfer uang otomatis.

Seluruh nilai uang MVP menggunakan IDR.

---

## 3. Prinsip keuangan MVP

### 3.1 Pisahkan tiga jenis arus

```text
Revenue             = berasal dari Sale Confirmed
Customer Payment    = berasal dari SalePayment Confirmed
Capital Contribution = modal, bukan revenue
```

Kontribusi modal tidak menambah laba.

### 3.2 Pisahkan profit share dan capital recovery

```text
Profit Share
=
bagian keuntungan

Capital Recovery
=
pengembalian modal

Total Payout
=
Capital Recovery + Profit Share
```

Pemisahan ini mencegah modal pokok hilang dari perhitungan pembayaran.

### 3.3 Hanya transaksi Confirmed yang dihitung

```text
Draft      -> belum masuk laporan final
Confirmed  -> masuk laporan
Cancelled  -> dikeluarkan dari laporan
```

### 3.4 Tidak ada hard delete

Transaksi yang dibatalkan tetap disimpan untuk audit dan histori musim.

---

## 4. Struktur domain

Aggregate root baru:

```text
CultivationExpense
CapitalContribution
SalePayment
ProfitSharingSettlement
```

Child entity:

```text
ProfitSharingAllocation
```

Read model:

```text
CropCycleProfitabilityReport
```

Hubungan utama:

```text
Organization
  CropCycle
    CultivationActivity
      CultivationActivityResource
    CultivationExpense
    CapitalContribution
    ProfitSharingSettlement
      ProfitSharingAllocation

  Sale
    SaleLine
      CropCycleIdSnapshot
    SalePayment
```

`CropCycleProfitabilityReport` dihitung dari sumber transaksi dan tidak
menjadi aggregate mutable.

---

## 5. Enumeration

### 5.1 CultivationExpenseStatus

```csharp
public enum CultivationExpenseStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

### 5.2 CultivationExpenseCategory

```csharp
public enum CultivationExpenseCategory
{
    LandLease = 1,
    Seed = 2,
    Fertilizer = 3,
    Pesticide = 4,
    Labor = 5,
    Equipment = 6,
    Irrigation = 7,
    Fuel = 8,
    Transport = 9,
    Storage = 10,
    Harvest = 11,
    PostHarvest = 12,
    Marketing = 13,
    Administration = 14,
    Other = 15
}
```

### 5.3 CapitalContributorRole

```csharp
public enum CapitalContributorRole
{
    Investor = 1,
    Partner = 2
}
```

Istilah antarmuka untuk `Partner` adalah Mitra.

### 5.4 CapitalContributionStatus

```csharp
public enum CapitalContributionStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

### 5.5 SalePaymentMethod

```csharp
public enum SalePaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Other = 3
}
```

### 5.6 SalePaymentStatus

```csharp
public enum SalePaymentStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

### 5.7 ProfitabilityOutcome

```csharp
public enum ProfitabilityOutcome
{
    Loss = 1,
    BreakEven = 2,
    Profit = 3
}
```

### 5.8 ProfitSharingSettlementStatus

```csharp
public enum ProfitSharingSettlementStatus
{
    Draft = 1,
    Finalized = 2,
    Voided = 3
}
```

---

## 6. Sumber biaya aktual

Total biaya berasal dari dua sumber.

```text
TotalCultivationCost
=
ActivityResourceCost
+
ConfirmedManualExpense
```

### 6.1 Activity resource cost

Sumber:

```text
CultivationActivityResource.TotalCost
```

Aturan:

```text
Activity Planned
-> belum dianggap biaya aktual

Activity InProgress
-> dianggap biaya aktual

Activity Completed
-> dianggap biaya aktual

Activity Cancelled dengan ActualStartDate
-> dianggap biaya aktual

Activity Cancelled tanpa ActualStartDate
-> dianggap rencana yang tidak terlaksana
```

### 6.2 Manual expense

`CultivationExpense` dipakai untuk biaya aktual yang tidak sudah dicatat
sebagai resource aktivitas.

Contoh:

- sewa lahan;
- administrasi kontrak;
- transport;
- storage;
- pascapanen;
- pemasaran;
- biaya lain yang tidak terikat ke satu aktivitas.

### 6.3 Double counting

Biaya yang sudah menjadi activity resource tidak boleh dicatat ulang
sebagai manual expense.

MVP menampilkan peringatan, tetapi tidak mencoba mengenali duplikasi
semantik secara otomatis.

---

## 7. CultivationExpense aggregate

### 7.1 Properti

```text
Id
OrganizationId
CropCycleId
Code
ExpenseDate
Category
Description
Amount
PayeeName
ReferenceNumber
EvidenceUrl
Notes
Status
ConfirmedAt
CancellationReason
audit fields
```

### 7.2 Kode

Kode:

- wajib;
- trim;
- uppercase;
- maksimal 40 karakter;
- immutable;
- unik dalam OrganizationId + CropCycleId.

Pola:

```regex
^[A-Z0-9][A-Z0-9._-]{0,39}$
```

### 7.3 Nilai dan precision

```text
Amount > 0
numeric(18,2)
MidpointRounding.AwayFromZero
```

### 7.4 Batas teks

```text
Code                 40
Description         250
PayeeName           150
ReferenceNumber     100
EvidenceUrl        1000
CancellationReason  500
Notes               1000
```

### 7.5 Lifecycle

```text
Draft -> Confirmed
Draft -> Cancelled
Confirmed -> Cancelled
```

Draft dapat diubah.

Confirmed:

- masuk biaya aktual;
- immutable;
- dapat dibatalkan sebelum settlement Finalized aktif.

Cancelled:

- wajib mempunyai alasan;
- tidak masuk biaya aktual;
- tetap dapat dibaca.

---

## 8. CapitalContribution aggregate

### 8.1 Tujuan

Capital contribution mencatat modal yang benar-benar diterapkan pada satu
Crop Cycle.

Kontribusi digunakan untuk:

- memeriksa sumber pendanaan biaya;
- menghitung rasio modal;
- menghitung capital recovery;
- menghitung capital profit;
- menghitung capital loss.

### 8.2 Properti

```text
Id
OrganizationId
CropCycleId
Code
ContributionDate
ContributorCode
ContributorName
ContributorRole
Amount
PaymentMethod
ReferenceNumber
Notes
Status
ConfirmedAt
CancellationReason
audit fields
```

### 8.3 Contributor identity

`ContributorCode` menjadi identitas stabil dalam settlement.

Contoh:

```text
INV-001
INV-BUDI
MITRA-001
MITRA-SLAMET
```

Batas:

```text
ContributorCode  40
ContributorName  150
```

Settlement mengelompokkan contribution berdasarkan:

```text
ContributorRole + ContributorCode
```

### 8.4 Aturan nilai

```text
Amount > 0
numeric(18,2)
```

Amount adalah modal yang diterapkan pada Crop Cycle, bukan saldo umum
perusahaan.

### 8.5 Lifecycle

```text
Draft -> Confirmed
Draft -> Cancelled
Confirmed -> Cancelled
```

Confirmed contribution:

- masuk total modal;
- immutable;
- dapat dibatalkan sebelum settlement Finalized.

### 8.6 Rekonsiliasi modal

Sebelum settlement dapat difinalisasi:

```text
ConfirmedInvestorCapital
+
ConfirmedPartnerCapital
=
TotalCultivationCost
```

Contribution yang lebih besar dari biaya harus dikoreksi.

Contribution yang lebih kecil dari biaya berarti funding gap dan
settlement belum dapat difinalisasi.

---

## 9. SalePayment aggregate

### 9.1 Tujuan

Sale Confirmed mengakui revenue.

SalePayment Confirmed mencatat uang yang benar-benar diterima.

```text
Recognized Revenue
Collected Revenue
Outstanding Receivable
```

harus dapat dibedakan.

### 9.2 Properti

```text
Id
OrganizationId
SaleId
Code
PaymentDate
Amount
PaymentMethod
ReferenceNumber
ReceivedFrom
Notes
Status
ConfirmedAt
CancellationReason
audit fields
```

### 9.3 Aturan kode

Kode unik dalam organisasi.

Contoh:

```text
PAY-2027-0001
TRF-2027-0001
CASH-2027-0001
```

### 9.4 Aturan pembayaran

Payment hanya untuk Sale Confirmed.

```text
Amount > 0
PaymentDate >= SaleDate
```

Confirmed payment tidak boleh membuat total pembayaran melebihi
Sale.TotalAmount.

```text
ConfirmedPaidAmount
=
Sum(Confirmed SalePayment.Amount)
```

```text
OutstandingReceivable
=
Sale.TotalAmount - ConfirmedPaidAmount
```

### 9.5 Cash dan credit

Sale Cash tetap memakai SalePayment agar penerimaan uang mempunyai bukti
dan tanggal.

Sale Credit dapat mempunyai beberapa payment.

Payment state dihitung:

```text
Unpaid
PartiallyPaid
Paid
```

### 9.6 Lifecycle

```text
Draft -> Confirmed
Draft -> Cancelled
Confirmed -> Cancelled
```

Sale tidak dapat dibatalkan selama mempunyai payment Confirmed.

---

## 10. Alokasi revenue ke Crop Cycle

Satu Sale dapat memuat batch dari beberapa Crop Cycle.

Revenue dihitung per SaleLine.

### 10.1 Header discount

Sale.DiscountAmount dialokasikan proporsional.

```text
RawAllocatedDiscount
=
Sale.DiscountAmount
x
SaleLine.LineTotal
/
Sale.Subtotal
```

Setiap hasil dibulatkan dua desimal.

Remainder dialokasikan ke line terakhir berdasarkan urutan SaleLine.Id.

### 10.2 Net revenue line

```text
NetRevenueLine
=
SaleLine.LineTotal
-
AllocatedSaleDiscount
```

### 10.3 Revenue per Crop Cycle

```text
RecognizedRevenuePerCropCycle
=
Sum(NetRevenueLine)
where Sale.Status = Confirmed
and SaleLine.CropCycleIdSnapshot = target CropCycle
```

### 10.4 Payment allocation

Payment di tingkat Sale dialokasikan proporsional ke net revenue line.

```text
AllocatedPaymentToLine
=
ConfirmedPaymentAmount
x
NetRevenueLine
/
Sale.TotalAmount
```

Ketika Sale lunas:

```text
CollectedRevenuePerCropCycle
=
RecognizedRevenuePerCropCycle
```

---

## 11. CropCycleProfitabilityReport

### 11.1 Properti

```text
OrganizationId
CropCycleId
CropCycleCode
CropCycleName
Commodity snapshots
RecognizedRevenue
CollectedRevenue
OutstandingReceivable
ActivityResourceCost
ManualExpenseCost
TotalCultivationCost
NetProfit
ProfitMarginPercentage
Outcome
ConfirmedInvestorCapital
ConfirmedPartnerCapital
TotalConfirmedCapital
CapitalFundingGap
CapitalFundingExcess
AvailableHarvestQuantity
GeneratedAt
```

### 11.2 Rumus

```text
TotalCultivationCost
=
ActivityResourceCost + ManualExpenseCost
```

```text
NetProfit
=
RecognizedRevenue - TotalCultivationCost
```

```text
OutstandingReceivable
=
RecognizedRevenue - CollectedRevenue
```

```text
ProfitMarginPercentage
=
NetProfit / RecognizedRevenue x 100
```

Margin null ketika revenue nol.

### 11.3 Outcome

```text
NetProfit < 0  -> Loss
NetProfit = 0  -> BreakEven
NetProfit > 0  -> Profit
```

### 11.4 Funding status

```text
CapitalFundingGap
=
Max(TotalCultivationCost - TotalConfirmedCapital, 0)
```

```text
CapitalFundingExcess
=
Max(TotalConfirmedCapital - TotalCultivationCost, 0)
```

Preview boleh ditampilkan walaupun ada gap atau excess.

---

## 12. Model pembagian hasil

### 12.1 Dua profit pool

```text
ManagementProfitPool
=
Round(NetProfit / 3, 2)

CapitalProfitPool
=
NetProfit - ManagementProfitPool
```

Remainder diberikan ke CapitalProfitPool agar total tepat sama dengan
NetProfit.

### 12.2 Management profit

ManagementProfitPool diberikan kepada Mitra pengelola.

Bagian ini tidak bergantung pada modal Mitra.

### 12.3 Capital profit

```text
ContributorCapitalRatio
=
ContributorConfirmedCapital
/
TotalCultivationCost
```

```text
ContributorCapitalProfit
=
CapitalProfitPool
x
ContributorCapitalRatio
```

### 12.4 Rumus Mitra

```text
PartnerProfitShare
=
ManagementProfitPool
+
PartnerCapitalProfit
```

Setara dengan:

```text
PartnerProfitShare
=
(1/3 x NetProfit)
+
(
    2/3
    x
    PartnerCapital / TotalCultivationCost
    x
    NetProfit
)
```

### 12.5 Rumus Investor

```text
InvestorProfitShare
=
CapitalProfitPool
-
PartnerCapitalProfit
```

Karena total confirmed capital harus sama dengan total cost:

```text
InvestorProfitShare
+
PartnerProfitShare
=
NetProfit
```

### 12.6 Semua modal dari Investor

```text
PartnerCapital = 0
InvestorCapital = TotalCultivationCost
```

Hasil:

```text
PartnerProfitShare = 1/3 x NetProfit
InvestorProfitShare = 2/3 x NetProfit
```

Total payout Investor tetap mencakup pengembalian modal.

---

## 13. Capital recovery dan loss

### 13.1 Profit

Ketika NetProfit positif:

```text
CapitalRecovery = full confirmed capital
CapitalLoss = 0
ProfitShare mengikuti bagian 12
```

### 13.2 Break even

```text
NetProfit = 0
ProfitShare = 0
CapitalRecovery = full confirmed capital
CapitalLoss = 0
```

### 13.3 Loss

Ketika revenue lebih kecil dari cost:

```text
ProfitShare = 0
RecoverableCapitalPool = Max(RecognizedRevenue, 0)
```

```text
ContributorCapitalRecovery
=
RecoverableCapitalPool
x
ContributorCapital
/
TotalCultivationCost
```

```text
ContributorCapitalLoss
=
ContributorCapital
-
ContributorCapitalRecovery
```

Loss tidak otomatis dianggap sebagai kelalaian Mitra.

Penetapan tanggung jawab khusus berada di luar rumus otomatis MVP.

---

## 14. ProfitSharingSettlement aggregate

### 14.1 Properti

```text
Id
OrganizationId
CropCycleId
Code
SettlementDate
ManagingPartnerCode
ManagingPartnerName
RecognizedRevenue
CollectedRevenue
ActivityResourceCost
ManualExpenseCost
TotalCultivationCost
NetProfit
Outcome
ManagementProfitPool
CapitalProfitPool
TotalInvestorCapital
TotalPartnerCapital
TotalCapital
TotalCapitalRecovery
TotalCapitalLoss
TotalInvestorProfitShare
TotalPartnerProfitShare
TotalPayout
CalculationVersion
Notes
Status
FinalizedAt
VoidReason
audit fields
Allocations
```

### 14.2 Calculation version

MVP memakai:

```text
SIPACUL-PS-1
```

### 14.3 Managing Partner

Managing Partner wajib mempunyai code dan name snapshot.

Managing Partner boleh mempunyai modal nol.

### 14.4 Lifecycle

```text
Draft -> Finalized
Draft -> Voided
Finalized -> Voided
```

Voided terminal.

Koreksi settlement:

1. void settlement lama;
2. perbaiki sumber;
3. buat settlement baru;
4. finalize settlement baru.

### 14.5 Finalized

Finalized settlement:

- immutable;
- menjadi settlement aktif;
- mengunci sumber perhitungan;
- hanya satu aktif per Crop Cycle.

---

## 15. ProfitSharingAllocation child

### 15.1 Properti

```text
Id
OrganizationId
ProfitSharingSettlementId
ContributorCodeSnapshot
ContributorNameSnapshot
ContributorRole
ConfirmedCapital
CapitalRatio
CapitalRecovery
CapitalLoss
ManagementProfitShare
CapitalProfitShare
TotalProfitShare
TotalPayout
Sequence
CreatedAt
```

### 15.2 Managing Partner allocation

Managing Partner selalu mempunyai allocation.

Bila modal nol:

```text
ConfirmedCapital = 0
CapitalRatio = 0
CapitalRecovery = 0
CapitalLoss = 0
CapitalProfitShare = 0
ManagementProfitShare = ManagementProfitPool
```

### 15.3 Invariant

```text
Sum(ConfirmedCapital)
=
TotalCultivationCost
```

```text
Sum(CapitalRecovery)
=
Settlement.TotalCapitalRecovery
```

```text
Sum(CapitalLoss)
=
Settlement.TotalCapitalLoss
```

Ketika profit:

```text
Sum(TotalProfitShare)
=
NetProfit
```

Selalu:

```text
Sum(TotalPayout)
=
Settlement.TotalPayout
```

---

## 16. Finalization prerequisites

Settlement hanya dapat difinalisasi bila:

1. Crop Cycle berstatus Completed atau Cancelled.
2. Tidak ada activity Planned atau InProgress.
3. Tidak ada HarvestBatch Draft.
4. Harvest Confirmed mempunyai AvailableQuantity nol.
5. Tidak ada Sale Draft yang memakai harvest Crop Cycle.
6. Seluruh Sale Confirmed terkait sudah lunas.
7. Tidak ada CultivationExpense Draft.
8. Tidak ada CapitalContribution Draft.
9. Tidak ada SalePayment Draft terkait.
10. Total confirmed capital sama dengan total cost.
11. Total cost lebih besar dari nol.
12. Tidak ada settlement Finalized aktif.
13. Seluruh sumber dihitung ulang dalam transaksi.
14. Hasil hitung ulang sama dengan draft snapshot.

Closing inventory valuation belum didukung.

Karena itu, hasil panen Confirmed harus sudah terjual atau dibatalkan
sebelum settlement final.

---

## 17. Source locking

Settlement Finalized aktif memblokir:

- pembatalan CultivationExpense;
- pembatalan CapitalContribution;
- pembatalan SalePayment;
- pembatalan Sale yang mempunyai line untuk Crop Cycle tersebut;
- perubahan lain yang mengubah sumber settlement.

Satu Sale dapat memuat beberapa Crop Cycle.

Settlement Finalized pada salah satu Crop Cycle cukup untuk memblokir
pembatalan seluruh Sale.

Void settlement membuka koreksi sumber bila tidak ada settlement aktif
lain.

---

## 18. Concurrency dan transaksi

Finalization menggunakan:

```text
PostgreSQL Serializable transaction
```

dan wajib dijalankan melalui:

```csharp
DbContext.Database.CreateExecutionStrategy()
```

Urutan finalization:

1. reload Crop Cycle;
2. reload activities dan resources;
3. reload expenses;
4. reload contributions;
5. reload harvest availability;
6. reload sales dan lines;
7. reload payments;
8. reload active settlement;
9. hitung profitability;
10. hitung allocation;
11. finalize;
12. save;
13. commit.

Serialization failure dipetakan ke:

```text
ProfitSharingSettlements.ConcurrencyConflict
```

HTTP status:

```text
409 Conflict
```

---

## 19. Rounding

Money:

```text
numeric(18,2)
MidpointRounding.AwayFromZero
```

Quantity tetap:

```text
numeric(18,4)
```

Capital ratio:

```text
numeric(18,8)
```

Remainder allocation menggunakan urutan stabil:

```text
ContributorRole
ContributorCode
```

Revenue allocation menggunakan SaleLine.Id.

Tidak menggunakan floating-point double.

---

## 20. Database baseline

Tabel baru:

```text
CultivationExpenses
CapitalContributions
SalePayments
ProfitSharingSettlements
ProfitSharingAllocations
```

Unique indexes:

```text
UX_CultivationExpenses_OrganizationId_CropCycleId_Code

UX_CapitalContributions_OrganizationId_CropCycleId_Code

UX_SalePayments_OrganizationId_Code

UX_ProfitSharingSettlements_OrganizationId_Code
```

Satu settlement Finalized aktif menggunakan partial unique index
PostgreSQL.

Semua foreign key menggunakan organization-scoped keys dan:

```text
DeleteBehavior.Restrict
```

Tidak ada cascade delete untuk histori keuangan.

---

## 21. Application services

### 21.1 ICultivationExpenseService

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdateDraftAsync
ConfirmAsync
CancelAsync
```

### 21.2 ICapitalContributionService

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdateDraftAsync
ConfirmAsync
CancelAsync
```

### 21.3 ISalePaymentService

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdateDraftAsync
ConfirmAsync
CancelAsync
GetPaymentSummaryAsync
```

### 21.4 IProfitabilityService

```text
GetCropCycleReportAsync
GetOrganizationSummaryAsync
```

### 21.5 IProfitSharingSettlementService

```text
CreateDraftAsync
GetAllAsync
GetByIdAsync
RecalculateDraftAsync
FinalizeAsync
VoidAsync
```

---

## 22. HTTP API baseline

### 22.1 Expenses

```text
POST   /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses/{expenseId}
PUT    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses/{expenseId}
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses/{expenseId}/confirm
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/expenses/{expenseId}/cancel
```

### 22.2 Capital contributions

```text
POST   /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions/{contributionId}
PUT    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions/{contributionId}
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions/{contributionId}/confirm
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/capital-contributions/{contributionId}/cancel
```

### 22.3 Sale payments

```text
POST   /api/v1/organizations/{organizationId}/sales/{saleId}/payments
GET    /api/v1/organizations/{organizationId}/sales/{saleId}/payments
GET    /api/v1/organizations/{organizationId}/sales/{saleId}/payments/{paymentId}
PUT    /api/v1/organizations/{organizationId}/sales/{saleId}/payments/{paymentId}
PATCH  /api/v1/organizations/{organizationId}/sales/{saleId}/payments/{paymentId}/confirm
PATCH  /api/v1/organizations/{organizationId}/sales/{saleId}/payments/{paymentId}/cancel
GET    /api/v1/organizations/{organizationId}/sales/{saleId}/payment-summary
```

### 22.4 Profitability

```text
GET
/api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profitability
```

### 22.5 Settlements

```text
POST   /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements
GET    /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements/{settlementId}
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements/{settlementId}/recalculate
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements/{settlementId}/finalize
PATCH  /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-settlements/{settlementId}/void
```

---

## 23. Error baseline

```text
CultivationExpenses.Validation
CultivationExpenses.OrganizationNotFound
CultivationExpenses.CropCycleNotFound
CultivationExpenses.NotFound
CultivationExpenses.CodeAlreadyExists
CultivationExpenses.InvalidStatusTransition
CultivationExpenses.FinalizedSettlementExists

CapitalContributions.Validation
CapitalContributions.OrganizationNotFound
CapitalContributions.CropCycleNotFound
CapitalContributions.NotFound
CapitalContributions.CodeAlreadyExists
CapitalContributions.ContributorIdentityConflict
CapitalContributions.InvalidStatusTransition
CapitalContributions.FinalizedSettlementExists

SalePayments.Validation
SalePayments.OrganizationNotFound
SalePayments.SaleNotFound
SalePayments.NotFound
SalePayments.CodeAlreadyExists
SalePayments.SaleNotConfirmed
SalePayments.PaymentDateBeforeSale
SalePayments.AmountExceedsOutstanding
SalePayments.InvalidStatusTransition
SalePayments.FinalizedSettlementExists

Profitability.OrganizationNotFound
Profitability.CropCycleNotFound
Profitability.InvalidSourceState

ProfitSharingSettlements.Validation
ProfitSharingSettlements.OrganizationNotFound
ProfitSharingSettlements.CropCycleNotFound
ProfitSharingSettlements.NotFound
ProfitSharingSettlements.CodeAlreadyExists
ProfitSharingSettlements.ActiveSettlementExists
ProfitSharingSettlements.CropCycleNotTerminal
ProfitSharingSettlements.ActiveActivityExists
ProfitSharingSettlements.DraftHarvestExists
ProfitSharingSettlements.UnsoldHarvestExists
ProfitSharingSettlements.DraftSaleExists
ProfitSharingSettlements.OutstandingReceivableExists
ProfitSharingSettlements.DraftExpenseExists
ProfitSharingSettlements.DraftContributionExists
ProfitSharingSettlements.DraftPaymentExists
ProfitSharingSettlements.CapitalDoesNotMatchCost
ProfitSharingSettlements.ZeroCostUnsupported
ProfitSharingSettlements.SourceDataChanged
ProfitSharingSettlements.InvalidStatusTransition
ProfitSharingSettlements.ConcurrencyConflict
```

---

## 24. Tenant boundary

Setiap command dan query membawa OrganizationId.

Repository selalu memfilter organisasi.

Cross-organization reference menghasilkan NotFound.

Organization-scoped foreign key menjadi pertahanan database tambahan.

---

## 25. Explicit MVP decisions

1. Semua nilai uang memakai IDR.
2. Capital contribution bukan revenue.
3. Sale Confirmed adalah recognized revenue.
4. SalePayment Confirmed adalah collected revenue.
5. Payment state dihitung.
6. Expense Draft belum menjadi actual cost.
7. Expense Confirmed menjadi actual cost.
8. Activity Planned resource belum menjadi actual cost.
9. Tidak ada general ledger.
10. Tidak ada hard delete.
11. Header Sale discount dialokasikan proporsional.
12. Contribution total harus sama dengan cost saat finalization.
13. Management profit pool adalah 1/3 Net Profit.
14. Capital profit pool adalah remainder Net Profit.
15. Managing Partner menerima management share meskipun modal nol.
16. Capital profit dibagi berdasarkan rasio modal.
17. Profit share tidak mencakup capital recovery.
18. Total payout mencakup capital recovery.
19. Pada loss tidak ada profit share.
20. Pada loss capital recovery dibagi proporsional.
21. Kelalaian Mitra tidak diputuskan otomatis.
22. Finalized settlement mengunci sumber.
23. Koreksi memakai void dan settlement baru.
24. Hanya satu Finalized settlement aktif per Crop Cycle.
25. Closing inventory valuation ditunda.
26. Finalization memerlukan Sale lunas.
27. Finalization memakai Serializable transaction.
28. Sprint 19 memakai hasil finance untuk evaluasi.
29. Sprint 20 menambahkan auth dan tenant membership.

---

## 26. Implementation sequence

### Sprint 18A-1

- approve design;
- commit baseline Finance dan Profit Sharing.

### Sprint 18A-2

- implement CultivationExpense domain;
- add domain tests.

### Sprint 18A-3

- add expense persistence, migration, application, API, dan E2E.

### Sprint 18B-1

- implement CapitalContribution end-to-end.

### Sprint 18C-1

- implement SalePayment;
- add receivable calculation;
- protect Sale cancellation.

### Sprint 18D-1

- implement revenue allocation;
- implement cost aggregation;
- implement CropCycleProfitabilityReport.

### Sprint 18E-1

- implement ProfitSharingSettlement dan Allocation;
- add profit, break-even, dan loss tests.

### Sprint 18E-2

- add settlement persistence dan migration.

### Sprint 18F-1

- add settlement application;
- add source locking;
- add retry-safe serializable finalization.

### Sprint 18G-1

- add profitability dan settlement HTTP API.

### Sprint 18H-1

- run full E2E with API and PostgreSQL;
- verify profit, break-even, loss, rounding, locking, void;
- verify organization isolation;
- clean test data;
- confirm clean Git working tree.

---

## 27. Acceptance criteria

Sprint 18 selesai ketika:

- activity resource cost dihitung sesuai lifecycle;
- manual expense bekerja end-to-end;
- capital contribution bekerja end-to-end;
- contribution tidak menjadi revenue;
- partial dan full Sale payment bekerja;
- overpayment ditolak;
- outstanding receivable benar;
- cross-cycle Sale allocation benar;
- header Sale discount allocation tepat;
- recognized dan collected revenue dapat dibedakan;
- total cost benar;
- funding gap dan excess benar;
- Net Profit dan margin benar;
- Profit, BreakEven, dan Loss benar;
- seluruh modal Investor menghasilkan pembagian 2/3 dan 1/3;
- modal Mitra menambah bagian capital profit Mitra;
- capital recovery terpisah dari profit share;
- loss mengurangi capital recovery secara proporsional;
- allocation total sama dengan settlement total;
- rounding remainder tidak mengubah total;
- finalization prerequisites ditegakkan;
- Finalized settlement immutable;
- source locking bekerja;
- void mempertahankan histori;
- settlement baru dapat dibuat setelah void;
- organization isolation terverifikasi;
- PostgreSQL constraints terverifikasi;
- automated tests lulus;
- E2E lulus;
- cleanup bersih;
- repository kembali bersih.
