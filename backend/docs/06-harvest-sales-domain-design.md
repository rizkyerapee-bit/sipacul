# Sprint 17 - Harvest and Sales Domain Design

## 1. Tujuan

Sprint 17 menghubungkan hasil budidaya dengan pendapatan usaha.

Istilah antarmuka:

- **Panen** untuk `HarvestBatch`;
- **Penjualan** untuk `Sale`;
- **Rincian Penjualan** untuk `SaleLine`.

Kode tetap menggunakan bahasa Inggris agar konsisten dengan backend SiPacul.

Modul harus menjawab:

1. Berapa hasil panen dari setiap Siklus Budidaya?
2. Berapa hasil kotor, hasil ditolak, dan hasil bersih?
3. Berapa jumlah yang masih tersedia untuk dijual?
4. Batch panen mana yang sudah dijual?
5. Siapa pembelinya dan berapa harga jualnya?
6. Berapa pendapatan penjualan terkonfirmasi?
7. Siklus dan komoditas mana yang menghasilkan pendapatan tersebut?

Pendapatan terkonfirmasi akan menjadi input Sprint 18 untuk akuntansi,
profitabilitas, dan pembagian hasil investor-mitra.

---

## 2. Ruang lingkup

Sprint 17 mencakup:

- panen bertahap atau parsial;
- beberapa batch panen dalam satu Crop Cycle;
- jumlah kotor, ditolak, dan bersih;
- mutu dan lokasi penyimpanan sederhana;
- konfirmasi dan pembatalan panen;
- perhitungan jumlah tersedia;
- transaksi penjualan draft;
- beberapa baris batch panen dalam satu penjualan;
- snapshot pembeli;
- termin tunai atau kredit;
- konfirmasi dan pembatalan penjualan;
- pencegahan overselling;
- isolasi organisasi;
- proteksi lifecycle Crop Cycle;
- persistence PostgreSQL;
- HTTP API;
- unit, application, persistence, API, dan E2E tests.

Ditunda:

- master pelanggan;
- pembayaran piutang;
- cicilan;
- pajak;
- surat jalan;
- retur penjualan;
- konversi satuan;
- multi-gudang;
- jurnal akuntansi;
- pembagian hasil.

---

## 3. Keputusan utama

### 3.1 Panen menggunakan batch

Satu Crop Cycle dapat dipanen lebih dari satu kali.

Setiap kejadian panen disimpan sebagai `HarvestBatch`.

Contoh:

- petik pertama;
- petik kedua;
- panen akhir;
- kualitas berbeda;
- bagian lahan berbeda.

### 3.2 Penjualan berada pada tingkat organisasi

Satu penjualan dapat memuat batch dari beberapa Crop Cycle.

Base route penjualan:

```text
/api/v1/organizations/{organizationId}/sales
```

### 3.3 Tidak ada konversi satuan otomatis

Satuan baris penjualan harus sama dengan satuan batch panen.

MVP tidak mengubah kilogram ke ton, karung ke kilogram, atau peti ke unit.

### 3.4 Hanya penjualan Confirmed menjadi pendapatan

```text
Draft      -> bukan pendapatan
Confirmed  -> pendapatan
Cancelled  -> bukan pendapatan
```

### 3.5 Tidak ada hard delete melalui aplikasi

Data yang batal tetap disimpan dengan status dan alasan pembatalan agar histori
musim tetap utuh.

---

## 4. Struktur aggregate

```text
Organization
+-- CropCycle
|   +-- HarvestBatch
+-- Sale
    +-- SaleLine
        +-- references HarvestBatch
```

Aggregate root baru:

1. `HarvestBatch`
2. `Sale`

Child entity:

1. `SaleLine`

---

## 5. Enumeration

### 5.1 HarvestBatchStatus

```csharp
public enum HarvestBatchStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

### 5.2 SaleStatus

```csharp
public enum SaleStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3
}
```

### 5.3 HarvestQuantityUnit

```csharp
public enum HarvestQuantityUnit
{
    Kilogram = 1,
    Ton = 2,
    Quintal = 3,
    Piece = 4,
    Bunch = 5,
    Sack = 6,
    Crate = 7,
    Liter = 8
}
```

### 5.4 SalePaymentTerm

```csharp
public enum SalePaymentTerm
{
    Cash = 1,
    Credit = 2
}
```

Kredit wajib memiliki tanggal jatuh tempo.

---

## 6. HarvestBatch aggregate

### 6.1 Properti

```text
Id
OrganizationId
CropCycleId
Code
HarvestDate
GrossQuantity
RejectedQuantity
NetQuantity
QuantityUnit
QualityGrade
StorageLocation
Notes
Status
ConfirmedAt
CancellationReason
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
DeletedAt
DeletedBy
IsDeleted
```

### 6.2 Rumus

```text
NetQuantity = GrossQuantity - RejectedQuantity
```

`NetQuantity` dihitung oleh aggregate, bukan dikirim bebas oleh client.

### 6.3 Aturan kode

Kode:

- wajib;
- trim;
- uppercase;
- maksimal 40 karakter;
- hanya huruf, angka, titik, garis bawah, dan tanda hubung.

Pola:

```regex
^[A-Z0-9][A-Z0-9._-]{0,39}$
```

Unik dalam:

```text
OrganizationId + CropCycleId
```

Kode sama boleh digunakan di Crop Cycle berbeda.

### 6.4 Aturan kuantitas

- `GrossQuantity > 0`;
- `RejectedQuantity >= 0`;
- `RejectedQuantity <= GrossQuantity`;
- `NetQuantity > 0` sebelum konfirmasi;
- precision `numeric(18,4)`;
- rounding `MidpointRounding.AwayFromZero`.

### 6.5 Aturan tanggal

Batch baru hanya dapat dibuat ketika Crop Cycle `InProgress`.

Tanggal panen:

- wajib;
- tidak boleh sebelum `ActualStartDate` Crop Cycle;
- boleh lebih awal dari perkiraan panen;
- boleh lebih lambat dari perkiraan panen;
- tidak boleh menyalahi tanggal panen aktual final Crop Cycle.

### 6.6 Batas teks

```text
Code                 40
QualityGrade         100
StorageLocation      250
CancellationReason   500
Notes                1000
CreatedBy            150
UpdatedBy            150
DeletedBy            150
```

`QualityGrade` berupa teks karena standar mutu berbeda antar-komoditas.

### 6.7 Lifecycle

```text
Draft -> Confirmed
Draft -> Cancelled
Confirmed -> Cancelled
```

Tidak diperbolehkan:

```text
Confirmed -> Draft
Cancelled -> Draft
Cancelled -> Confirmed
```

### 6.8 Draft

Batch Draft:

- dapat diedit;
- belum dapat dijual;
- tidak masuk stok tersedia;
- menghalangi penyelesaian Crop Cycle;
- dapat dibatalkan.

### 6.9 Confirmed

Konfirmasi memvalidasi:

- organisasi dan Crop Cycle;
- Crop Cycle `InProgress`;
- kode unik;
- kuantitas valid;
- net quantity positif;
- tanggal valid.

Setelah Confirmed:

- kuantitas immutable;
- satuan immutable;
- kualitas immutable;
- tanggal immutable;
- batch tersedia untuk penjualan.

### 6.10 Cancelled

Confirmed batch hanya boleh dibatalkan jika tidak direferensikan penjualan
Confirmed aktif.

Batch Cancelled:

- tetap dapat dibaca;
- tersedia = 0;
- tidak dapat diedit;
- tidak dapat dikonfirmasi kembali;
- tidak dapat dipakai pada penjualan baru.

---

## 7. Ketersediaan hasil panen

Jumlah tersedia tidak disimpan sebagai kolom mandiri.

```text
AvailableQuantity
=
Confirmed Harvest Net Quantity
-
Confirmed Sold Quantity
```

Dikecualikan:

- sale Draft;
- sale Cancelled;
- harvest Draft;
- harvest Cancelled.

Hasil tidak boleh negatif.

---

## 8. Sale aggregate

### 8.1 Properti

```text
Id
OrganizationId
Code
SaleDate
BuyerName
BuyerPhone
BuyerAddress
PaymentTerm
DueDate
DiscountAmount
Subtotal
TotalAmount
Status
ConfirmedAt
CancellationReason
Notes
CreatedAt
CreatedBy
UpdatedAt
UpdatedBy
DeletedAt
DeletedBy
IsDeleted
Lines
```

### 8.2 Kode

Kode unik dalam organisasi.

Contoh:

```text
SALE-2027-0001
INV-2027-0001
PJ-2027-0001
```

### 8.3 Snapshot pembeli

```text
BuyerName
BuyerPhone
BuyerAddress
```

`BuyerName` wajib.

Batas:

```text
BuyerName      150
BuyerPhone      50
BuyerAddress   500
Notes         1000
```

### 8.4 Termin pembayaran

Tunai:

- due date opsional;
- bila ada, tidak boleh sebelum tanggal penjualan.

Kredit:

- due date wajib;
- due date minimal sama dengan tanggal penjualan.

Status pembayaran ditunda ke Sprint 18.

### 8.5 Precision uang

```text
UnitPrice       numeric(18,2)
LineDiscount    numeric(18,2)
LineTotal       numeric(18,2)
DiscountAmount  numeric(18,2)
Subtotal        numeric(18,2)
TotalAmount     numeric(18,2)
```

Rounding:

```csharp
MidpointRounding.AwayFromZero
```

### 8.6 Rumus

```text
GrossLineAmount = Quantity x UnitPrice
LineTotal = Round(GrossLineAmount, 2) - LineDiscount
Subtotal = Sum(LineTotal)
TotalAmount = Subtotal - DiscountAmount
```

Aturan:

- quantity > 0;
- unit price >= 0;
- line discount >= 0;
- line discount tidak melebihi gross line;
- sale discount >= 0;
- sale discount tidak melebihi subtotal;
- total amount >= 0.

### 8.7 Lifecycle

```text
Draft -> Confirmed
Draft -> Cancelled
Confirmed -> Cancelled
```

### 8.8 Draft

Sale Draft:

- dapat mengubah pembeli dan termin;
- dapat menambah, mengubah, menghapus line;
- belum menjadi pendapatan;
- belum mengurangi stok resmi;
- hanya boleh memakai harvest Confirmed;
- seluruh harvest harus satu organisasi.

### 8.9 Confirmed

Konfirmasi dilakukan dalam satu transaksi database.

Sale wajib memiliki minimal satu line.

Setiap line memvalidasi:

- batch ditemukan;
- organisasi sama;
- batch Confirmed;
- satuan sama;
- tidak duplikat;
- quantity tidak melebihi available quantity.

Setelah Confirmed:

- buyer immutable;
- line immutable;
- harga immutable;
- diskon immutable;
- termin immutable;
- menjadi pendapatan;
- mengurangi available quantity.

### 8.10 Cancelled

Sale Confirmed boleh dibatalkan dengan alasan wajib.

Pembatalan:

- menyimpan histori;
- mengeluarkan sale dari pendapatan;
- mengembalikan quantity tersedia;
- tidak menghapus line;
- sale tetap read-only.

---

## 9. SaleLine

### 9.1 Properti

```text
Id
OrganizationId
SaleId
HarvestBatchId
HarvestBatchCodeSnapshot
CropCycleIdSnapshot
CropCycleCodeSnapshot
CommodityIdSnapshot
CommodityCodeSnapshot
CommodityNameSnapshot
QualityGradeSnapshot
Quantity
QuantityUnit
UnitPrice
LineDiscount
LineTotal
Notes
CreatedAt
UpdatedAt
```

### 9.2 Snapshot

Snapshot menjaga histori apabila nama komoditas, kode Crop Cycle, atau grade
berubah setelah transaksi.

### 9.3 Duplikasi

Satu sale hanya boleh memiliki satu line untuk satu harvest batch.

Perbedaan harga untuk batch sama dicatat sebagai transaksi penjualan berbeda
pada MVP.

### 9.4 Lifecycle child

Line hanya dapat diubah ketika parent Sale masih `Draft`.

---

## 10. Integrasi Crop Cycle

### 10.1 Membuat panen

Harvest batch baru hanya untuk Crop Cycle `InProgress`.

### 10.2 Menyelesaikan Crop Cycle

Completion diblokir bila terdapat HarvestBatch `Draft`.

Completion diizinkan bila:

- tidak ada harvest Draft;
- harvest yang ada berstatus Confirmed atau Cancelled.

Crop Cycle boleh selesai tanpa harvest terkonfirmasi untuk mencatat gagal
panen atau hasil nol.

### 10.3 Membatalkan Crop Cycle

Cancellation diblokir selama ada HarvestBatch non-Cancelled.

Urutan yang benar:

1. batalkan harvest Draft;
2. batalkan harvest Confirmed yang tidak memiliki sale Confirmed;
3. batalkan Crop Cycle.

### 10.4 Penjualan setelah siklus selesai

Harvest Confirmed tetap dapat dijual setelah Crop Cycle Completed.

---

## 11. Isolasi organisasi

Setiap command dan query wajib membawa `OrganizationId`.

Repository selalu memfilter organisasi.

Cross-organization access mengembalikan `NotFound`.

SaleLine tidak boleh mereferensikan batch organisasi lain.

---

## 12. Persistence

### 12.1 HarvestBatches

Tabel:

```text
HarvestBatches
```

Key dan index:

```text
PK_HarvestBatches
AK_HarvestBatches_OrganizationId_Id
UX_HarvestBatches_OrganizationId_CropCycleId_Code
IX_HarvestBatches_OrganizationId_CropCycleId_Status
IX_HarvestBatches_OrganizationId_HarvestDate
IX_HarvestBatches_OrganizationId_Status
IX_HarvestBatches_IsDeleted
```

FK:

```text
OrganizationId -> Organizations.Id RESTRICT

OrganizationId + CropCycleId
-> CropCycles.OrganizationId + CropCycles.Id RESTRICT
```

### 12.2 Sales

Tabel:

```text
Sales
```

Key dan index:

```text
PK_Sales
AK_Sales_OrganizationId_Id
UX_Sales_OrganizationId_Code
IX_Sales_OrganizationId_Status
IX_Sales_OrganizationId_SaleDate
IX_Sales_OrganizationId_BuyerName
IX_Sales_IsDeleted
```

### 12.3 SaleLines

Tabel:

```text
SaleLines
```

Key dan index:

```text
PK_SaleLines
UX_SaleLines_OrganizationId_SaleId_HarvestBatchId
IX_SaleLines_OrganizationId_SaleId
IX_SaleLines_OrganizationId_HarvestBatchId
IX_SaleLines_OrganizationId_CommodityIdSnapshot
```

FK:

```text
OrganizationId + SaleId
-> Sales.OrganizationId + Sales.Id CASCADE

OrganizationId + HarvestBatchId
-> HarvestBatches.OrganizationId + HarvestBatches.Id RESTRICT
```

Cascade hanya menunjukkan ownership Sale ke SaleLine.

Aplikasi tetap tidak menghapus sale confirmed secara fisik.

---

## 13. Concurrency dan overselling

Overselling merupakan invariant lintas-baris.

Sale confirmation harus berada dalam transaksi:

1. load sale Draft;
2. load seluruh referenced harvest;
3. kunci atau lindungi batch terkait;
4. hitung confirmed sold quantity;
5. validasi available quantity;
6. ubah status menjadi Confirmed;
7. commit.

Implementasi pertama boleh menggunakan EF transaction dan query repository.

Optimasi row lock PostgreSQL dapat dilakukan kemudian.

---

## 14. Repository contracts

### 14.1 IHarvestBatchRepository

```text
GetByIdAsync
GetAllByCropCycleAsync
CodeExistsAsync
HasDraftBatchesAsync
HasNonCancelledBatchesAsync
AddAsync
```

### 14.2 ISaleRepository

```text
GetByIdAsync
GetAllAsync
CodeExistsAsync
GetConfirmedSoldQuantitiesAsync
HasActiveConfirmedSaleForHarvestAsync
AddAsync
```

---

## 15. Application services

### 15.1 IHarvestBatchService

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdateDraftAsync
ConfirmAsync
CancelAsync
```

### 15.2 ISaleService

```text
CreateAsync
GetAllAsync
GetByIdAsync
UpdateDraftAsync
AddLineAsync
UpdateLineAsync
RemoveLineAsync
ConfirmAsync
CancelAsync
```

### 15.3 HarvestBatchResponse

Tambahan hasil query:

```text
AvailableQuantity
ConfirmedSoldQuantity
```

### 15.4 SaleResponse

Mencakup header sale, totals, status, dan seluruh lines.

---

## 16. HTTP API

### 16.1 Harvest

Base route:

```text
/api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/harvest-batches
```

Endpoint:

```text
POST   /harvest-batches
GET    /harvest-batches
GET    /harvest-batches/{harvestBatchId}
PUT    /harvest-batches/{harvestBatchId}
PATCH  /harvest-batches/{harvestBatchId}/confirm
PATCH  /harvest-batches/{harvestBatchId}/cancel
```

Filter:

```text
status
harvestDateFrom
harvestDateTo
quantityUnit
qualityGrade
```

### 16.2 Sales

Base route:

```text
/api/v1/organizations/{organizationId}/sales
```

Endpoint:

```text
POST   /sales
GET    /sales
GET    /sales/{saleId}
PUT    /sales/{saleId}
POST   /sales/{saleId}/lines
PUT    /sales/{saleId}/lines/{lineId}
DELETE /sales/{saleId}/lines/{lineId}
PATCH  /sales/{saleId}/confirm
PATCH  /sales/{saleId}/cancel
```

Filter:

```text
status
saleDateFrom
saleDateTo
buyerName
paymentTerm
harvestBatchId
commodityId
```

---

## 17. Request contracts

```text
CreateHarvestBatchRequest
UpdateHarvestBatchRequest
CancelHarvestBatchRequest

CreateSaleRequest
UpdateSaleRequest
AddSaleLineRequest
UpdateSaleLineRequest
CancelSaleRequest
```

`AddSaleLineRequest`:

```text
HarvestBatchId
Quantity
UnitPrice
LineDiscount
Notes
```

Satuan diambil dari harvest batch.

---

## 18. Error catalogue

Harvest:

```text
HarvestBatches.Validation
HarvestBatches.OrganizationNotFound
HarvestBatches.CropCycleNotFound
HarvestBatches.CropCycleNotInProgress
HarvestBatches.NotFound
HarvestBatches.CodeAlreadyExists
HarvestBatches.InvalidStatusTransition
HarvestBatches.InvalidQuantity
HarvestBatches.InvalidHarvestDate
HarvestBatches.ConfirmedSaleReferenceExists
HarvestBatches.CropCycleHasDraftHarvests
HarvestBatches.CropCycleHasNonCancelledHarvests
```

Sales:

```text
Sales.Validation
Sales.OrganizationNotFound
Sales.NotFound
Sales.CodeAlreadyExists
Sales.InvalidStatusTransition
Sales.Empty
Sales.LineNotFound
Sales.DuplicateHarvestBatch
Sales.HarvestBatchNotFound
Sales.HarvestBatchNotConfirmed
Sales.UnitMismatch
Sales.InsufficientAvailableQuantity
Sales.InvalidPaymentTerm
Sales.InvalidDiscount
```

Mapping:

```text
Validation          -> 400
NotFound            -> 404
Uniqueness          -> 409
Lifecycle           -> 409
Overselling         -> 409
Cross-organization  -> 404
```

---

## 19. Reporting

Confirmed harvest:

```text
Sum NetQuantity
where HarvestBatch.Status = Confirmed
```

Rejected harvest:

```text
Sum RejectedQuantity
where HarvestBatch.Status = Confirmed
```

Confirmed revenue:

```text
Sum TotalAmount
where Sale.Status = Confirmed
```

Pendapatan per Crop Cycle berasal dari `SaleLine.CropCycleIdSnapshot`.

Alokasi discount level sale ke setiap Crop Cycle ditetapkan pada Sprint 18.

---

## 20. Cross-module protection

Crop Cycle completion memeriksa:

- tidak ada CultivationActivity InProgress;
- tidak ada HarvestBatch Draft.

Crop Cycle cancellation memeriksa:

- tidak ada CultivationActivity InProgress;
- tidak ada HarvestBatch non-Cancelled.

Harvest tidak mengubah snapshot SOP aktivitas.

---

## 21. Testing

### 21.1 Harvest domain

- code normalization;
- quantity validation;
- net calculation;
- rounding;
- update Draft;
- confirm;
- cancel;
- invalid transition;
- terminal immutability.

### 21.2 Sale domain

- buyer dan payment term;
- line add/update/remove;
- duplicate batch;
- amount calculation;
- discount;
- empty confirmation;
- confirm;
- cancel;
- terminal immutability.

### 21.3 Persistence

- table names;
- precision;
- unique indexes;
- organization-scoped FK;
- cascade ownership;
- restrictive external FK;
- confirmed sold query.

### 21.4 Application

- organization isolation;
- Crop Cycle status;
- availability;
- snapshot line;
- unit match;
- overselling;
- transaction;
- cancellation protection;
- Crop Cycle protection.

### 21.5 API

- route binding;
- filters;
- HTTP status;
- nested harvest routes;
- line routes;
- lifecycle endpoints.

### 21.6 E2E

E2E PostgreSQL nyata wajib membuktikan:

1. create reference data;
2. start Crop Cycle;
3. create dan confirm harvest;
4. draft harvest tidak dapat dijual;
5. create sale dan lines;
6. confirm sale;
7. available quantity berkurang;
8. overselling ditolak;
9. harvest cancellation diblokir;
10. cancel sale;
11. quantity kembali;
12. cancel harvest;
13. Crop Cycle protection;
14. database precision;
15. cleanup;
16. Git bersih.

---

## 22. Urutan implementasi

### Sprint 17A - Harvest

```text
17A-1  Harvest and sales design
17A-2  Harvest domain
17A-3  Harvest persistence
17A-4  Harvest application
17A-5  Harvest HTTP API
17A-6  Harvest E2E
```

### Sprint 17B - Sales

```text
17B-1  Sales domain
17B-2  Sales persistence
17B-3  Sales application
17B-4  Sales HTTP API
17B-5  Sales and inventory E2E
```

---

## 23. Acceptance criteria

Sprint 17 selesai ketika:

- multiple harvest batch bekerja;
- net harvest benar;
- hanya harvest Confirmed yang dapat dijual;
- sale dapat memiliki beberapa line;
- overselling ditolak;
- sale Confirmed mengurangi availability;
- sale Cancelled mengembalikan availability;
- confirmed revenue dapat dihitung;
- proteksi Crop Cycle diterapkan;
- isolasi organisasi terbukti;
- PostgreSQL constraints terverifikasi;
- semua tests lulus;
- E2E nyata lulus;
- cleanup lulus;
- working tree bersih.

---

## 24. Ringkasan formula

```text
Net Harvest
=
Gross Quantity - Rejected Quantity
```

```text
Available Quantity
=
Confirmed Net Harvest - Confirmed Sold Quantity
```

```text
Line Total
=
Round(Quantity x Unit Price, 2) - Line Discount
```

```text
Sale Total
=
Sum(Line Total) - Sale Discount
```

```text
Confirmed Revenue
=
Sum(Total Amount of Confirmed Sales)
```

Desain ini menjadi jembatan dari operasional budidaya menuju akuntansi,
profitabilitas, dan pembagian hasil pada Sprint 18.
