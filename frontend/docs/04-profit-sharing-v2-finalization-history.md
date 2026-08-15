# Profit Sharing V2 — Stage 3C2 Finalization and History

## Tujuan

Stage 3C2 menutup workflow antarmuka `SIPACUL-PS-2`. Preview dinamis dari
Stage 3C1 sekarang dapat difinalkan menjadi snapshot immutable, dibaca kembali
sebagai histori, dan di-void tanpa menghapus jejak audit.

## Alur pengguna

1. Pengguna memilih siklus pada halaman Pembagian Hasil.
2. Tab **Preview V2** digunakan untuk memilih snapshot skema dan memeriksa
   hasil waterfall terbaru.
3. Setelah siklus selesai atau dibatalkan, tab **Finalisasi V2** menampilkan
   kesiapan finalisasi.
4. Pengguna dengan izin `profit-sharing.finalize` memasukkan kode, tanggal,
   serta catatan opsional.
5. Server menghitung ulang sumber transaksi dalam lock yang sama dengan
   finalizer V1 dan menyimpan snapshot final.
6. Histori menampilkan identitas skema, sumber profitabilitas, potongan
   prioritas, residual, modal kembali, kerugian modal, dan pembayaran setiap
   peserta.
7. Pengguna dengan izin `profit-sharing.void` dapat membatalkan snapshot aktif
   dengan alasan wajib. Snapshot tetap tersedia untuk audit dan finalisasi baru
   dapat dibuat setelah sumber transaksi dikoreksi.

## Prinsip keamanan

- UI tidak menganggap preview sebagai hasil resmi.
- Finalisasi hanya tersedia untuk siklus terminal tanpa settlement aktif.
- Server tetap menjadi sumber validasi akhir untuk status transaksi, panen,
  penjualan, piutang, biaya, modal, identitas peserta, dan concurrency.
- Snapshot final tidak menyediakan operasi edit atau hapus.
- Void tidak mengubah formula maupun nilai historis.
- Settlement V1 dan V2 tetap memakai lock aktif yang sama pada satu siklus.

## Dukungan skema

Rincian histori tidak mengasumsikan jumlah atau jenis peserta tertentu. Tabel
alokasi menampilkan perusahaan, mitra pengelola, investor pasif, dan peran
tambahan berdasarkan snapshot yang diberikan API. Aturan prioritas dan metode
residual juga dibaca dari snapshot sehingga strategi baru untuk tenant SaaS
dapat ditambahkan tanpa membangun ulang halaman histori.

## Tampilan responsif

Pada desktop, histori dan detail ditampilkan berdampingan dengan tabel alokasi
lima kolom. Pada tablet dan ponsel, daftar berpindah ke atas dan setiap baris
alokasi berubah menjadi kartu berlabel tanpa overflow horizontal.

## Scope

Stage ini mengubah tiga file frontend dan menambah tiga file frontend:

- integrasi tab pada halaman pembagian hasil;
- komponen dan style finalisasi serta histori V2;
- helper, validasi, ringkasan, filter, dan test;
- dokumen ini.

Tidak ada perubahan backend, migration, dependency, atau lockfile.
