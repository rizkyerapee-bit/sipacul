# Urutan Modul MVP SiPacul

Dokumen ini menetapkan urutan pengembangan MVP setelah fondasi Organisasi, Master Komoditas, dan SOP Budidaya selesai.

## Prinsip Pengembangan

Setiap modul baru harus:

- Dimiliki oleh organisasi
- Mencegah relasi lintas organisasi
- Mempunyai aturan bisnis pada Domain Layer
- Mempunyai Application Service
- Mempunyai repository PostgreSQL
- Mempunyai endpoint HTTP
- Mempunyai unit test
- Mempunyai integration test
- Diuji end-to-end sebelum dinyatakan selesai

## Tahap yang Sudah Selesai

- Fondasi Organisasi
- Master Kategori Komoditas
- Master Komoditas
- Master SOP Budidaya

## Sprint 14 — Lahan dan Petak

Tujuan: menciptakan identitas lokasi budidaya yang stabil.

Entity utama:

- Farm atau Lahan
- Plot atau Petak

Aturan utama:

- Kode lahan unik dalam organisasi
- Kode petak unik dalam satu lahan
- Petak harus berada pada organisasi yang sama dengan lahan
- Total luas petak tidak boleh melampaui luas lahan
- Lahan tidak boleh dinonaktifkan ketika masih mempunyai siklus aktif

## Sprint 15 — Siklus Budidaya

Mencatat satu periode budidaya dari persiapan sampai evaluasi serta menghubungkan organisasi, lahan, petak, komoditas, dan SOP.

## Sprint 16 — Aktivitas Budidaya

Mencatat pekerjaan lapangan, bahan, tenaga kerja, alat, durasi, biaya, bukti, penanggung jawab, dan status verifikasi.

## Sprint 17 — Panen dan Penjualan

Mencatat batch panen, kualitas, kuantitas, susut, harga jual, pembeli, pembayaran, dan piutang.

## Sprint 18 — Keuangan dan Bagi Hasil

Rumus dasar:

`Keuntungan Bersih = Penjualan - Total Biaya Budidaya`

Kondisi modal seluruhnya dari investor:

- Investor menerima 2/3 keuntungan bersih
- Mitra menerima 1/3 keuntungan bersih

Kondisi mitra ikut menanamkan modal:

`Bagi Hasil Mitra = (1/3 × Keuntungan Bersih) + (2/3 × Proporsi Modal Mitra × Keuntungan Bersih)`

## Sprint 19 — Histori Lahan dan Evaluasi Musim

Menggabungkan riwayat komoditas, SOP, aktivitas, biaya, insiden, panen, penjualan, keuntungan, kesalahan pelaksanaan, dan rekomendasi musim berikutnya.

## Sprint 20 — Identitas dan Penguatan SaaS

Mencakup pengguna, login, keanggotaan organisasi, peran, izin, tenant resolution, audit, rate limiting, health check, logging, backup, CI/CD, dan deployment self-hosted.

## Definisi MVP Selesai

MVP selesai ketika pengguna dapat:

1. Membuat organisasi
2. Membuat lahan dan petak
3. Membuat komoditas dan SOP
4. Membuka siklus budidaya
5. Mencatat aktivitas dan biaya
6. Mencatat panen
7. Mencatat penjualan
8. Menghitung laba
9. Menghitung pembagian hasil
10. Melihat histori dan evaluasi musim
