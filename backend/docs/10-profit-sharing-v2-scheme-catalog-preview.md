# Profit Sharing V2 — Stage 2A Scheme Catalog

## Tujuan

Stage 2A menyimpan skema waterfall `SIPACUL-PS-2` sebagai konfigurasi
organisasi yang berversi. Kalkulator Stage 1 tetap murni dan tidak diubah.
Skema aktif tidak dapat diedit; perubahan dibuat sebagai versi draft baru agar
hasil lama tetap dapat ditelusuri saat SiPacul digunakan sebagai SaaS.

## Model versi

Setiap keluarga skema memiliki `SchemeFamilyId`, `Code`, dan nomor `Version`.
Statusnya mengikuti alur berikut:

```text
Draft v1 -> Active v1 -> Superseded v1
                  \-> Draft v2 -> Active v2
```

- Satu keluarga hanya boleh memiliki satu draft dan satu versi aktif.
- Aktivasi draft berikutnya menandai versi aktif sebelumnya sebagai
  `Superseded` dalam transaksi serializable.
- Versi baru menyalin snapshot peserta dan aturan dari versi aktif, lalu dapat
  disunting sebelum diaktifkan.
- Semua data dibatasi oleh `OrganizationId`.

## Isi skema

Satu versi menyimpan:

- peserta: perusahaan, investor pasif, mitra pengelola, atau peran lain;
- urutan peserta untuk tampilan dan editor visual di tahap frontend;
- aturan prioritas berurutan, seperti biaya pengelolaan dan imbal hasil atas
  modal;
- metode pembagian laba tersisa: penerima sisa tunggal, proporsional terhadap
  modal, atau persentase tetap;
- urutan aturan dan pembagian sisa sebagai fondasi editor drag-and-drop tanpa
  mengikat domain ke library antarmuka tertentu.

Nilai tingkat disimpan sebagai pembilang dan penyebut. Contohnya `1 / 3`
dipertahankan sebagai pecahan, bukan dibulatkan lebih awal menjadi `33,33%`.

## Contoh konfigurasi

### Internal perusahaan

- peserta `PERUSAHAAN`;
- tanpa aturan prioritas;
- seluruh sisa dialokasikan ke `PERUSAHAAN`.

### Perusahaan dan mitra pengelola

- peserta `PERUSAHAAN` dan `MITRA`;
- aturan prioritas `ManagementShare` sebesar `1 / 3` kepada `MITRA`;
- laba setelah biaya pengelolaan dibagi proporsional terhadap modal.

### Perusahaan, mitra, dan investor pasif

- tambahkan peserta berperan `PassiveInvestor`;
- aturan `ReturnOnCapital` dapat memberi imbal hasil prioritas kepada investor;
- peserta yang memenuhi syarat dapat mengikuti pembagian laba tersisa secara
  proporsional terhadap modal;
- pada kondisi rugi, kalkulator Stage 1 tetap mengembalikan modal tersedia
  secara proporsional tanpa menjalankan aturan laba.

## API Stage 2A

Semua endpoint berada di bawah:

`/api/v1/organizations/{organizationId}/profit-sharing-schemes`

- `POST /` membuat draft versi pertama;
- `GET /` menampilkan katalog, dengan filter status dan kode;
- `GET /{schemeId}` mengambil satu snapshot;
- `PUT /{schemeId}` mengubah draft;
- `POST /{sourceSchemeId}/versions` membuat draft versi berikutnya;
- `PATCH /{schemeId}/activate` mengaktifkan draft dan menggantikan versi aktif.

Hak baca menggunakan `ProfitSharingRead`, perubahan draft menggunakan
`ProfitSharingWrite`, dan aktivasi menggunakan `ProfitSharingFinalize`.

## Batas Stage 2A

Stage ini belum mengikat skema ke siklus budidaya dan belum membuat preview
dari setoran modal aktual. Stage 2B akan menambahkan assignment skema ke siklus,
membangun input kalkulator dari setoran terkonfirmasi dan laporan
profitabilitas, lalu menyimpan snapshot versi skema pada hasil preview/final.
Frontend editor—termasuk drag-and-drop—baru menggunakan kontrak ini setelah
alur backend tersebut stabil.
