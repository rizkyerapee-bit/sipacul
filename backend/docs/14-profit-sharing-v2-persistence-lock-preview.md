# Profit Sharing V2 — Stage 2C2A Persistence and Cross-Version Lock

## Tujuan

Stage 2C2A menyimpan aggregate final waterfall dari Stage 2C1 tanpa mengubah
hasil kalkulator `SIPACUL-PS-2` atau histori settlement `SIPACUL-PS-1`.

Empat tabel baru menyimpan:

- root final settlement dan seluruh snapshot profitabilitas serta skema;
- priority allocation, termasuk tarif dan nilai yang tidak teralokasi;
- participant allocation, termasuk investor pasif, recovery, loss, dan payout;
- fixed residual share yang menjadi bagian kontrak pembagian.

## Penguncian lintas versi

Satu siklus hanya boleh memiliki satu settlement final aktif, baik dari
`SIPACUL-PS-1` maupun `SIPACUL-PS-2`.

Indeks parsial menjaga keunikan settlement aktif di masing-masing tabel.
Karena PostgreSQL tidak menyediakan indeks unik lintas dua tabel, finalisasi
memakai row lock pada record `CropCycles` dalam transaksi serializable. Alur
finalisasi lama juga memakai row lock yang sama dan menolak settlement V2 aktif.

Perubahan terhadap sumber berikut ditolak ketika salah satu versi memiliki
settlement final aktif:

- biaya budidaya;
- setoran modal;
- pembayaran penjualan.

Settlement V2 yang di-void tidak lagi dianggap sebagai lock aktif, sedangkan
snapshot historinya tetap tersimpan.

## Batas Stage 2C2A

Tahap ini menambahkan konfigurasi EF Core, repository, DbSet, migration, dan
penguncian lintas versi. Belum ada endpoint finalisasi/void atau perubahan
frontend. Stage 2C2B akan menggunakan row lock dan repository ini untuk API
finalisasi atomik.
