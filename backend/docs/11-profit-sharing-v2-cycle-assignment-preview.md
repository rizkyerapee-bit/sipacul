# Profit Sharing V2 — Stage 2B1 Cycle Assignment

## Tujuan

Stage 2B1 menghubungkan satu versi skema bagi hasil aktif ke satu siklus
budidaya. Assignment menyimpan snapshot lengkap agar perubahan katalog pada
musim berikutnya tidak mengubah aturan musim yang sedang berjalan atau histori
musim sebelumnya.

## Snapshot yang disimpan

Assignment menyimpan:

- identitas skema sumber, keluarga skema, kode, nama, dan nomor versi;
- peserta beserta peran perusahaan, mitra pengelola, investor pasif, atau
  peran tambahan;
- status keikutsertaan peserta dalam pembagian laba residual;
- aturan prioritas berurutan, termasuk biaya pengelolaan dan imbal hasil
  modal;
- pecahan tarif sebagai pembilang dan penyebut tanpa pembulatan dini;
- metode pembagian laba residual dan penerima sisa bila digunakan;
- urutan setiap peserta serta aturan sebagai fondasi editor visual di
  frontend.

Snapshot disimpan dalam tabel terstruktur, bukan satu kolom JSON. Pendekatan
ini mempertahankan validasi relasional dan tetap memungkinkan kontrak custom
ditambahkan bertahap ketika SiPacul berkembang menjadi SaaS.

## Aturan assignment

- Hanya skema berstatus `Active` yang dapat dipilih.
- Satu siklus hanya memiliki satu assignment aktif.
- Assignment pertama dapat dibuat saat siklus `Planned` atau `InProgress`
  untuk mengakomodasi siklus lama yang sudah berjalan.
- Skema dapat diganti hanya selama siklus masih `Planned`.
- Permintaan memilih skema yang sama bersifat idempotent dan tidak menulis
  ulang database.
- Siklus `Completed` atau `Cancelled` tidak dapat menerima assignment.
- Skema sumber tetap direferensikan untuk audit, sedangkan seluruh definisi
  yang dipakai disalin ke snapshot assignment.

## API Stage 2B1

Endpoint berada di bawah:

`/api/v1/organizations/{organizationId}/crop-cycles/{cropCycleId}/profit-sharing-scheme`

- `GET /` mengambil snapshot assignment siklus;
- `PUT /` memilih atau mengganti skema menggunakan `{ "schemeId": "..." }`.

Hak baca menggunakan `ProfitSharingRead`, sedangkan assignment menggunakan
`ProfitSharingWrite`.

## Batas Stage 2B1

Tahap ini belum menjalankan kalkulator waterfall menggunakan data modal dan
profitabilitas aktual. Stage 2B2 akan membangun preview `SIPACUL-PS-2` dari
snapshot assignment, setoran modal terkonfirmasi, serta laporan profitabilitas
siklus. Database belum diperbarui otomatis; migration hanya dibuat dan ditinjau
sebagai bagian dari checkpoint source.
