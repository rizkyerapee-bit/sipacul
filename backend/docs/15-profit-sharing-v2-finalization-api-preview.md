# Profit Sharing V2 — Stage 2C2B Finalization and Void API

## Tujuan

Stage 2C2B menutup workflow backend waterfall `SIPACUL-PS-2` dari preview
menjadi snapshot final yang tersimpan. Pengguna tidak membuat draft settlement
V2. Alurnya adalah:

1. pilih skema aktif dan ikat snapshot-nya ke siklus;
2. periksa preview berdasarkan sumber transaksi terkini;
3. finalisasi dengan kode, tanggal, dan catatan opsional;
4. simpan satu snapshot final immutable beserta seluruh alokasi;
5. void dengan alasan wajib jika hasil final harus dibatalkan.

## Satu sumber kalkulasi

Preview dan finalisasi menggunakan
`ProfitSharingWaterfallSourceCalculator` yang sama. Komponen ini memetakan:

- peserta skema dan setoran modal terkonfirmasi;
- identitas serta peran perusahaan, mitra pengelola, dan investor pasif;
- aturan prioritas seperti biaya pengelolaan atau imbal hasil investor;
- kebijakan residual pro-rata modal, penerima sisa, atau persentase tetap.

Pemisahan ini menjadi titik ekstensi untuk strategi waterfall khusus tenant
SaaS tanpa menduplikasi formula di endpoint atau processor transaksi.

## Jaminan finalisasi

Finalisasi menggunakan transaksi `Serializable` dan row lock pada record
`CropCycles`. Lock yang sama dipakai finalizer `SIPACUL-PS-1`, sehingga dua
versi tidak dapat membuat settlement aktif pada siklus yang sama.

Setelah lock diperoleh, processor membaca ulang dan memvalidasi:

- status siklus sudah selesai atau dibatalkan;
- tidak ada aktivitas aktif, panen draft, penjualan draft, biaya draft,
  setoran draft, atau pembayaran draft;
- seluruh panen telah terjual dan piutang telah diterima;
- biaya budidaya positif dan sama dengan modal terkonfirmasi;
- identitas, peran, dan jumlah modal cocok dengan snapshot skema;
- belum ada settlement final aktif dari V1 maupun V2;
- kode settlement belum pernah digunakan pada siklus tersebut.

Snapshot baru hanya ditulis jika seluruh validasi dan kalkulasi selesai dalam
transaksi yang sama.

## Void

Void juga menggunakan transaksi `Serializable` dan row lock siklus. Hanya
settlement berstatus `Finalized` yang dapat di-void dan alasannya wajib diisi.
Snapshot, peserta, formula, dan alokasi tetap tersimpan untuk audit, sedangkan
source lock aktif dilepas setelah status berubah menjadi `Voided`.

## API

Base route:

`/api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-waterfall-settlements`

- `POST /` — finalisasi langsung dari sumber terkini;
- `GET /` — daftar snapshot dengan filter status dan tanggal;
- `GET /{settlementId}` — detail snapshot dan seluruh alokasi;
- `PATCH /{settlementId}/void` — void dengan alasan wajib.

Endpoint menggunakan permission baca, finalisasi, dan void yang sudah tersedia.

## Batas Stage 2C2B

Tahap ini tidak menambah tabel atau migration, tidak menjalankan update database,
dan tidak mengubah frontend. Integrasi UI skema, preview, finalisasi, serta
riwayat settlement dikerjakan setelah backend V2 lengkap dan checkpoint ini
stabil.
