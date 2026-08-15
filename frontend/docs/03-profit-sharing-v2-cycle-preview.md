# Profit Sharing V2 — Stage 3C1 Cycle Assignment and Preview

## Tujuan

Stage 3C1 menghubungkan skema aktif dari katalog Stage 3B ke satu siklus
budidaya dan menampilkan hasil kalkulasi waterfall `SIPACUL-PS-2` dari data
aktual. Tahap ini bersifat preview dan belum membuat settlement final.

## Assignment skema

Tab **Preview V2** tetap menggunakan pemilih siklus yang sudah ada pada halaman
pembagian hasil. Untuk setiap siklus, pengguna dapat memilih satu skema aktif.
SiPacul menyimpan snapshot yang berisi identitas versi, peserta, aturan
prioritas, tarif pecahan, dan metode pembagian laba tersisa.

Aturan perubahan ditampilkan langsung pada antarmuka:

- assignment pertama dapat dibuat saat siklus `Direncanakan` atau `Berjalan`;
- assignment yang sudah ada hanya dapat diganti saat siklus masih
  `Direncanakan`;
- setelah siklus berjalan, snapshot skema terkunci;
- siklus `Selesai` atau `Dibatalkan` tidak dapat menerima assignment;
- hanya skema berstatus `Aktif` yang dapat dipilih.

Konfirmasi ditampilkan sebelum penyimpanan agar pengguna memahami bahwa aturan
akan menjadi snapshot khusus untuk musim tersebut.

## Preview waterfall

Setelah assignment tersedia, frontend meminta preview baca-saja dari backend.
Tampilan mencakup:

- pendapatan diakui, biaya, modal, dan laba atau rugi bersih;
- rekonsiliasi modal kembali ditambah bagian laba menjadi total pembayaran;
- setiap potongan prioritas, termasuk dasar hitung, nilai diminta, nilai yang
  dapat dialokasikan, dan kekurangan alokasi;
- modal, rasio modal, pemulihan modal, kerugian modal, biaya pengelolaan, imbal
  hasil modal, laba residual, dan total pembayaran setiap peserta;
- peran perusahaan, mitra pengelola, investor pasif, atau peserta lain dari
  snapshot assignment.

Jika preview ditolak, pesan frontend menjelaskan masalah yang perlu diperbaiki,
seperti kode modal yang belum ada pada skema, peran pemberi modal yang berbeda,
identitas modal yang tidak konsisten, atau total modal yang belum sama dengan
biaya budidaya.

## Rekonsiliasi dan kondisi rugi

Frontend memeriksa bahwa:

```text
Total pembayaran = Total modal kembali + Total bagian laba
```

Saat terjadi kerugian, komponen kerugian modal ditampilkan secara eksplisit.
Aturan laba tidak dijalankan dan modal yang tersedia tetap mengikuti hasil
kalkulator backend secara proporsional.

## Izin

- Membaca assignment dan preview menggunakan `profit-sharing.read`.
- Memilih atau mengganti skema menggunakan `profit-sharing.write`.
- Tahap ini tidak memakai izin finalisasi karena belum menyimpan settlement.

## Batas Stage 3C1

Preview selalu ditandai belum terkunci dan dapat berubah bila pendapatan, biaya,
atau modal berubah. Stage 3C2 akan menambahkan finalisasi immutable, daftar
settlement V2, detail snapshot final, dan pembatalan dengan alasan audit.
