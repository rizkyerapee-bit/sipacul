# Profit Sharing V2 - Waterfall Preview

## 1. Status

Dokumen ini mendefinisikan calculator domain percobaan
`SIPACUL-PS-2`.

Tahap ini hanya menambahkan domain calculator dan automated test.
Belum ada perubahan pada database, migration, API, frontend, settlement,
atau source locking.

Calculator `SIPACUL-PS-1` tetap aktif dan tidak diubah.

---

## 2. Tujuan

Profit Sharing V2 mendukung skema pembagian hasil berurutan atau
waterfall untuk:

- pengelolaan internal perusahaan;
- pengelolaan oleh Mitra Tani;
- modal bersama perusahaan dan Mitra Tani;
- satu atau lebih Investor Pasif;
- imbal hasil Investor Pasif berdasarkan modal;
- pembagian sisa laba berdasarkan modal;
- pembagian sisa laba dengan persentase tetap;
- skema hybrid yang diaktifkan secara eksplisit;
- custom priority melalui urutan rule;
- kerugian dan pengembalian modal secara proporsional.

---

## 3. Istilah

`ManagementShare` adalah Bagian Pengelolaan yang berasal dari laba.
Nilai ini bukan biaya operasional budidaya.

Jika pembayaran pengelola wajib dibayar walaupun rugi, pembayaran
tersebut harus dicatat sebagai Biaya Budidaya sebelum Net Profit
dihitung.

`ReturnOnCapital` adalah imbal hasil Investor Pasif:

```text
Requested Return
=
Confirmed Investor Capital x Contract Rate
```

Imbal hasil hanya dialokasikan ketika outcome adalah Profit dan tidak
boleh melebihi laba yang masih tersedia.

`ResidualProfit` adalah laba yang tersisa setelah seluruh priority rule.

`CapitalRecovery` adalah pengembalian modal dan selalu dipisahkan dari
profit share.

---

## 4. Urutan waterfall

```text
1. Net Profit
2. Priority Rules sesuai Sequence
   - ManagementShare
   - ReturnOnCapital
3. Residual Profit
4. Capital Recovery
5. Total Payout
```

Urutan priority rule bersifat eksplisit. Ketika laba tidak mencukupi,
rule yang lebih awal menerima alokasi terlebih dahulu. Kekurangan
disimpan sebagai `UnallocatedAmount` dan tidak menjadi utang atau
carry-forward pada V2 awal.

Residual profit mempunyai tiga metode:

```text
RemainderToParticipant
ProRataCapital
FixedPercentage
```

Capital recovery bukan rule yang dapat dipindahkan. Ketentuan ini
menjaga modal dan laba tetap terpisah.

---

## 5. Peserta

Role awal:

```text
Company
PassiveInvestor
ManagingPartner
Other
```

Setiap peserta mempunyai:

- code dan name snapshot;
- role;
- confirmed capital;
- penanda apakah ikut membagi residual profit;
- sequence stabil untuk pembulatan dan tampilan.

Investor Pasif default:

- mempunyai modal;
- tidak menerima ManagementShare;
- menerima ReturnOnCapital;
- tidak ikut residual profit.

Investor dapat memakai skema hybrid hanya bila
`ParticipatesInResidualProfit` diaktifkan secara eksplisit.

---

## 6. Rumus profit

```text
Available Profit = Net Profit
```

Untuk setiap priority rule:

```text
Management Requested
=
Net Profit x Rate
```

```text
Return On Capital Requested
=
Recipient Confirmed Capital x Rate
```

```text
Allocated Amount
=
Min(Requested Amount, Available Profit)
```

```text
Available Profit
=
Available Profit - Allocated Amount
```

Setelah priority rule:

```text
Residual Profit = Available Profit
```

Pada metode `ProRataCapital`:

```text
Participant Residual Share
=
Residual Profit
x Participant Eligible Capital
/ Total Eligible Capital
```

Hasil akhir peserta:

```text
Total Profit Share
=
Management Share
+ Return On Capital Share
+ Residual Profit Share
```

```text
Total Payout
=
Capital Recovery
+ Total Profit Share
```

---

## 7. Loss dan break-even

Pada Loss:

- seluruh priority profit share bernilai nol;
- residual profit bernilai nol;
- tidak ada carry-forward;
- recognized revenue menjadi recoverable capital pool;
- capital recovery dibagi proporsional kepada seluruh pemodal;
- capital loss adalah modal dikurangi capital recovery.

Pada BreakEven:

- seluruh profit share bernilai nol;
- modal dikembalikan penuh.

---

## 8. Invariant

Calculator menolak skema bila:

- participant code tidak unik;
- sequence tidak berurutan mulai dari satu;
- total modal peserta tidak sama dengan biaya budidaya;
- recipient rule tidak ditemukan;
- ReturnOnCapital diberikan kepada peserta tanpa modal;
- fixed residual percentage tidak berjumlah 100%;
- metode pro-rata tidak mempunyai peserta bermodal yang eligible;
- rate bukan lebih dari nol sampai dengan 100%.

Calculator menjamin:

```text
Total Capital Recovery + Total Capital Loss
=
Total Capital
```

```text
Total Priority Profit + Total Residual Profit
=
Net Profit, ketika Profit
```

```text
Sum Participant Total Payout
=
Recognized Revenue
```

Selisih pembulatan diberikan kepada penerima terakhir yang stabil.

---

## 9. Preset yang diuji

1. Perusahaan internal 100%.
2. Modal perusahaan, pengelolaan Mitra Tani.
3. Modal perusahaan dan Mitra Tani, dikelola Mitra Tani.
4. Perusahaan dan Investor Pasif.
5. Perusahaan, Investor Pasif, dan Mitra Tani.
6. Beberapa Investor Pasif dengan rate berbeda.
7. Investor hybrid.
8. Laba tidak mencukupi dan priority cap.
9. Drag-and-drop priority melalui perubahan sequence.
10. Loss dengan pengembalian modal proporsional.
11. Fixed residual dan rounding remainder.
12. Preset lama yang menghasilkan angka identik dengan
    `SIPACUL-PS-1`.

---

## 10. Ekstensibilitas SaaS

Frontend drag-and-drop nantinya hanya mengubah `Sequence` rule.
Frontend tidak mengirim kode atau formula bebas.

Jenis rule baru dapat ditambahkan sebagai handler domain berversi,
misalnya:

- fixed bonus;
- revenue percentage;
- tiered return;
- maximum payout cap;
- performance bonus;
- carry-forward pada kontrak yang mendukungnya.

Settlement masa depan harus menyimpan snapshot peserta, rule, urutan,
rate, requested amount, allocated amount, residual method, dan version.

Settlement `SIPACUL-PS-1` tidak dikonversi. Histori lama tetap dihitung
dan diaudit memakai calculation version asalnya.
