# Profit Sharing V2 — Stage 3A Frontend Foundation

## Tujuan

Stage 3A menghubungkan frontend dengan seluruh workflow backend
`SIPACUL-PS-2` tanpa mengubah halaman pengguna terlebih dahulu. Fondasi ini
menyediakan kontrak TypeScript dan API client untuk:

- katalog skema berversi;
- assignment snapshot skema ke siklus budidaya;
- preview waterfall;
- finalisasi langsung, riwayat snapshot, dan void.

Kontrak `SIPACUL-PS-1` tetap tersedia agar halaman lama dapat berjalan selama
transisi frontend dilakukan bertahap.

## Fondasi editor skema

Helper editor tidak bergantung pada library drag-and-drop. Urutan peserta dan
aturan diubah melalui fungsi murni yang selalu membangun ulang `sequence`
kontigu. Komponen UI berikutnya dapat memakai tombol pindah, pointer drag, atau
library aksesibilitas tanpa mengubah bentuk request backend.

Tiga titik awal tersedia:

1. internal perusahaan;
2. perusahaan dengan mitra pengelola dan biaya pengelolaan `1 / 3`;
3. perusahaan dengan investor pasif dan pembagian residual pro-rata modal.

Preset hanya menjadi titik awal. Pengguna tetap dapat menambah investor pasif,
mitra, aturan biaya pengelolaan, imbal hasil modal, atau pembagian residual
custom.

## Validasi lokal

Validasi frontend mengikuti invariant katalog backend:

- kode unik dan format kode konsisten;
- peserta serta penerima aturan harus tersedia;
- tarif lebih besar dari nol dan tidak melebihi satu;
- residual tunggal, pro-rata modal, dan persentase tetap saling eksklusif;
- persentase residual tetap harus berjumlah tepat 100%;
- urutan request selalu dibangun ulang dari posisi visual.

Backend tetap menjadi sumber validasi akhir dan otorisasi.

## Batas Stage 3A

Tahap ini tidak mengubah backend, migration, dependency, route halaman, atau
tampilan yang sedang digunakan. Stage 3B akan memakai fondasi ini untuk katalog
dan editor skema visual. Stage 3C akan mengintegrasikan assignment, preview,
finalisasi, dan riwayat waterfall ke halaman Pembagian Hasil.
