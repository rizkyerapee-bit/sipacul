# Profit Sharing V2 — Technical Acceptance

## Status dokumen

- Status: diterima sebagai fondasi operasional SiPacul dan fondasi pengembangan SaaS.
- Tanggal penerimaan: 15 Agustus 2026.
- Baseline source: `559ec4cd7321d9009bd294fbed45291a9e7f4ae2`.
- Calculation version: `SIPACUL-PS-2`.
- Ruang lingkup: skema waterfall, assignment siklus, preview, settlement final, histori, void, API, persistence, dan antarmuka pengguna.

Dokumen ini menutup rangkaian implementasi Profit Sharing V2. Dokumen ini bukan kontrak hukum, bukti transfer dana, atau aturan pajak. Kontrak bisnis tetap menjadi sumber kewajiban para pihak; SiPacul mencatat skema dan menghasilkan perhitungan berdasarkan data yang dikonfirmasi.

## Keputusan bisnis yang diterima

Profit Sharing V2 mendukung empat pola utama tanpa membangun ulang modul:

1. Modal dan pengelolaan sepenuhnya internal perusahaan; seluruh modal kembali dan seluruh keuntungan menjadi hak perusahaan.
2. Modal sepenuhnya dari perusahaan, dengan mitra tani sebagai pengelola; bagian pengelolaan dipotong terlebih dahulu dari keuntungan sesuai tarif yang disepakati.
3. Modal berasal dari perusahaan, mitra tani, atau beberapa peserta; setelah bagian pengelolaan, keuntungan tersisa dibagi secara proporsional terhadap modal atau memakai bobot custom.
4. Investor pasif menerima pengembalian modal dan bagian keuntungan sesuai modal atau bobot custom, tanpa otomatis memperoleh bagian pengelolaan.

Skema custom dapat mengubah peserta, peran, urutan aturan prioritas, pecahan tarif, serta metode pembagian laba tersisa. Antarmuka drag-and-drop hanya mengubah urutan aturan; kalkulasi resmi tetap dilakukan oleh backend berdasarkan snapshot yang tersimpan.

## Model waterfall yang diterima

Urutan perhitungan kanonis adalah:

1. SiPacul mengambil sumber keuangan siklus yang telah dikonfirmasi.
2. Sistem menentukan hasil laba, impas, atau rugi.
3. Pada kondisi laba, aturan prioritas dijalankan berurutan. Contoh utamanya adalah biaya atau bagian pengelolaan.
4. Keuntungan yang masih tersisa dibagikan dengan metode residual yang tersimpan pada skema.
5. Modal dikembalikan berdasarkan modal terkonfirmasi dan dana yang tersedia.
6. Setiap peserta menerima alokasi terpisah untuk pengembalian modal, kerugian modal, bagian pengelolaan, bagian laba residual, dan total hak.
7. Total seluruh alokasi harus sama dengan total payout settlement.

Pada kondisi rugi, dana yang tersedia untuk pengembalian modal dialokasikan secara proporsional dan kekurangan dicatat sebagai kerugian modal. Tidak ada pembagian keuntungan apabila tidak tersedia keuntungan positif.

Tarif disimpan sebagai pecahan pembilang dan penyebut. Pendekatan ini menghindari pembulatan konfigurasi seperti `1/3` menjadi desimal terbatas dan memungkinkan skema khusus ditambahkan kemudian.

## Skenario penerimaan utama

Skenario end-to-end menggunakan:

- modal perusahaan: Rp60.000.000;
- modal investor pasif: Rp20.000.000;
- modal mitra pengelola: Rp20.000.000;
- total modal dan biaya budidaya: Rp100.000.000;
- pendapatan dan kas terkonfirmasi: Rp160.000.000;
- keuntungan bersih: Rp60.000.000;
- bagian pengelolaan: `1/3 × Rp60.000.000 = Rp20.000.000`;
- laba residual: Rp40.000.000.

Laba residual dibagi menurut proporsi modal 60% : 20% : 20%, sehingga hasil akhirnya:

| Peserta | Pengembalian modal | Pengelolaan | Laba residual | Total payout |
| --- | ---: | ---: | ---: | ---: |
| Perusahaan | Rp60.000.000 | Rp0 | Rp24.000.000 | Rp84.000.000 |
| Investor pasif | Rp20.000.000 | Rp0 | Rp8.000.000 | Rp28.000.000 |
| Mitra pengelola | Rp20.000.000 | Rp20.000.000 | Rp8.000.000 | Rp48.000.000 |
| **Total** | **Rp100.000.000** | **Rp20.000.000** | **Rp40.000.000** | **Rp160.000.000** |

Hasil tersebut telah cocok pada kalkulator, API end-to-end, preview frontend, finalisasi, serta pembacaan kembali histori.

## Lifecycle dan perlindungan data

- Skema memiliki keluarga dan versi. Perubahan terhadap skema yang sudah digunakan dilakukan melalui versi baru.
- Hanya skema aktif yang dapat dipilih untuk siklus.
- Assignment menyalin peserta, peran, aturan prioritas, pecahan tarif, urutan, dan residual sebagai snapshot siklus.
- Perubahan assignment hanya diperbolehkan sebelum siklus dimulai; pemilihan skema yang sama bersifat idempotent.
- Preview selalu dihitung dari snapshot assignment dan sumber keuangan siklus terkini.
- Finalisasi mengunci hasil menjadi settlement immutable.
- Settlement final tidak diedit atau dihapus. Koreksi dilakukan melalui void beralasan dan settlement pengganti.
- Hanya satu settlement final aktif yang boleh ada untuk satu siklus.
- Settlement menyimpan jejak sumber skema, assignment, calculation version, peserta, prioritas, residual, dan rekonsiliasi.
- Lock silang mencegah finalisasi ganda antara workflow lama `SIPACUL-PS-1` dan waterfall `SIPACUL-PS-2`.

## Cakupan teknis yang diterima

Backend mencakup:

- kalkulator domain waterfall;
- katalog skema berversi;
- aktivasi skema;
- assignment dan snapshot siklus;
- calculation preview;
- settlement immutable;
- konfigurasi dan migration PostgreSQL;
- API finalisasi, detail, filter histori, void, dan replacement;
- penguncian mutasi sumber keuangan setelah settlement final.

Frontend mencakup:

- kontrak dan client API Profit Sharing V2;
- preset internal, mitra pengelola, dan investor pasif;
- katalog serta editor skema;
- pengurutan peserta dan aturan dengan drag-and-drop tanpa dependency baru;
- assignment skema per siklus;
- preview modal, laba/rugi, prioritas, residual, dan rekonsiliasi;
- finalisasi, detail snapshot, histori, void, dan settlement pengganti.

## Bukti penerimaan

| Checkpoint | Bukti |
| --- | --- |
| Stage 1 | Kalkulator waterfall dan test domain; 1.534 test backend lulus. |
| Stage 2A | Katalog skema berversi dan migration; 1.564 test backend lulus. |
| Stage 2B1 | Assignment snapshot siklus dan migration; 1.590 test backend lulus. |
| Stage 2B2 | Preview waterfall API; 1.605 test backend lulus. |
| Stage 2C1 | Settlement snapshot immutable; 1.621 test backend lulus. |
| Stage 2C2A | Persistence settlement dan migration; 1.643 test backend lulus. |
| Stage 2C2B | API finalisasi, histori, void, replacement, dan lock; 1.688 test backend lulus. |
| Stage 3A | Fondasi frontend V2; lint, 118 test, dan build lulus. |
| Stage 3B | Editor skema; lint, 121 test, dan build lulus. |
| Stage 3C1 | Assignment dan preview; lint, 124 test, dan build lulus. |
| Stage 3C2 | Finalisasi dan histori; lint, 129 test, dan build lulus. |
| Stage 4A Fix 3 | Database kosong, tiga migration, HTTPS API, workflow penuh, formula, lock silang, void, dan replacement lulus. |
| Stage 4B Fix 1 | Checklist browser lulus dan tindakan pengguna diverifikasi kembali melalui API; residu otomatis Next.js dipulihkan. |

Stage penutupan tidak mengulang pengujian berat tersebut. Audit penutupan hanya memverifikasi rangkaian commit, keberadaan artefak, baseline, dan kebersihan Git.

## Fondasi perluasan SaaS

Arsitektur saat ini sengaja menyimpan konfigurasi sebagai data berversi dan hasil final sebagai snapshot. Karena itu, permintaan SaaS berikut dapat ditambahkan secara evolusioner:

- template skema per tenant atau per jenis komoditas;
- lebih banyak peserta dan peran;
- tarif bertingkat, batas minimum, batas maksimum, atau hurdle;
- imbal hasil investor berdasarkan waktu;
- biaya platform, pajak, atau reserve fund sebagai jenis aturan baru;
- approval berjenjang sebelum aktivasi dan finalisasi;
- permission yang lebih rinci;
- laporan pembayaran aktual dan integrasi bank;
- multi-currency dan kebijakan pembulatan per tenant.

Perluasan tersebut harus menggunakan jenis aturan atau calculation version baru apabila mengubah makna perhitungan. Snapshot lama tidak boleh dihitung ulang atau dimutasi.

## Batas MVP saat ini

- Settlement mencatat hak payout, bukan bukti bahwa transfer bank telah dilakukan.
- Aturan pajak, akad, dan kepatuhan hukum belum diotomatisasi.
- Imbal hasil berbasis durasi, hurdle, tier, reserve fund, dan multi-currency belum menjadi bagian MVP.
- Drag-and-drop mengatur urutan, bukan membuat formula arbitrer tanpa validasi domain.
- Workflow `SIPACUL-PS-1` tetap dipertahankan untuk kompatibilitas data lama, tetapi tidak boleh menghasilkan settlement aktif bersamaan dengan V2 pada siklus yang sama.

## Aturan perubahan setelah penerimaan

1. Jangan mengubah settlement final atau snapshot assignment yang sudah digunakan.
2. Gunakan versi skema baru untuk perubahan konfigurasi bisnis.
3. Gunakan calculation version baru untuk perubahan semantik perhitungan.
4. Tambahkan migration secara additive; jangan menulis ulang migration yang sudah diterapkan.
5. Pertahankan isolasi organisasi pada seluruh query dan constraint.
6. Tambahkan test untuk formula, lifecycle, authorization, persistence, API, dan frontend yang terdampak.
7. Jalankan end-to-end terisolasi untuk perubahan yang memengaruhi uang, finalisasi, atau migration.
8. Catat keputusan custom tenant agar tidak berubah menjadi percabangan kode tanpa model yang jelas.

## Keputusan akhir

Profit Sharing V2 diterima untuk kebutuhan operasional internal SiPacul dengan skema perusahaan, mitra tani, dan investor pasif. Fondasi versioning, snapshot, aturan berurutan, residual custom, immutability, serta isolasi organisasi memadai untuk pengembangan SaaS berikutnya tanpa membangun ulang seluruh modul.
