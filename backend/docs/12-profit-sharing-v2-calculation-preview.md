# Profit Sharing V2 — Stage 2B2 Calculation Preview

## Tujuan

Stage 2B2 menghubungkan tiga sumber yang telah tersedia:

- snapshot skema pada assignment siklus;
- setoran modal berstatus `Confirmed` per kode pemberi modal;
- laporan profitabilitas aktual siklus budidaya.

Ketiga sumber tersebut dibentuk menjadi input kalkulator
`SIPACUL-PS-2`. Endpoint bersifat baca-saja dan tidak mengubah settlement,
setoran modal, assignment, maupun database.

## Pemetaan peserta dan modal

`ParticipantCode` pada snapshot assignment harus sama dengan
`ContributorCode` pada setoran modal. Beberapa setoran dengan kode dan peran
yang sama dijumlahkan sebagai modal terkonfirmasi peserta.

Pemetaan peran awal:

```text
Company          -> Investor
PassiveInvestor  -> Investor
ManagingPartner  -> Partner
Other            -> Investor atau Partner
```

Preview ditolak bila:

- ada modal terkonfirmasi yang kodenya tidak terdapat dalam assignment;
- satu kode modal menggunakan lebih dari satu peran atau identitas nama;
- peran setoran tidak sesuai dengan peran peserta;
- total rincian modal berubah di antara pembacaan sumber;
- total modal tidak sama dengan biaya budidaya yang dihitung;
- snapshot rule atau residual tidak valid untuk kalkulator.

Peserta tanpa modal tetap dapat berada di dalam skema untuk menerima bagian
pengelolaan. Investor pasif dapat menerima `ReturnOnCapital`, dan hanya ikut
laba residual bila flag hybrid diaktifkan pada skema.

## API

Endpoint:

`GET /api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-preview`

Hak akses menggunakan `ProfitSharingRead`.

Respons memuat:

- snapshot assignment dan versi skema;
- laporan profitabilitas yang menjadi sumber angka;
- versi kalkulator;
- seluruh priority allocation beserta requested, allocated, dan unallocated;
- pengembalian modal, kerugian modal, bagian laba, dan total payout per peserta;
- total keseluruhan untuk rekonsiliasi.

Field `isPersisted` selalu `false` pada tahap ini. Preview dapat berubah bila
transaksi sumber berubah.

## Batas Stage 2B2

Stage ini tidak membuat migration dan tidak mengganti
`SIPACUL-PS-1`. Tahap berikutnya akan membuat hasil final immutable yang
menyimpan snapshot perhitungan, mengunci sumber setelah finalisasi, dan menjadi
dasar pembayaran tanpa mengubah histori settlement lama.
