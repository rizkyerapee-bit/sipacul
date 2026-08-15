# Profit Sharing V2 — Stage 3B Scheme Catalog and Editor

## Tujuan

Stage 3B menghubungkan katalog skema `SIPACUL-PS-2` ke halaman pembagian hasil.
Pengguna dapat membuat, mengubah, mengaktifkan, dan membuat versi baru skema
tanpa mengubah kalkulator, transaksi modal, atau settlement V1 yang sudah ada.

## Alur pengguna

Tab **Skema V2** menampilkan katalog tingkat organisasi dengan ringkasan jumlah
keluarga skema, versi aktif, draf, dan versi yang sudah digantikan. Katalog dapat
dicari berdasarkan kode, nama, atau deskripsi serta disaring berdasarkan status.

Pembuatan skema dimulai dari salah satu preset berikut:

- **Internal perusahaan**: modal dan pengelolaan internal, seluruh laba tersisa
  diterima perusahaan.
- **Dikelola mitra**: biaya pengelolaan `1/3` dipotong lebih dahulu, lalu laba
  tersisa dibagi proporsional terhadap modal aktual.
- **Perusahaan dan investor pasif**: laba tersisa dibagi proporsional terhadap
  modal tanpa menetapkan imbal hasil tetap secara sepihak.

Semua preset hanya merupakan titik awal. Pengguna tetap dapat mengubah peserta,
peran, penerima, tarif pecahan, aturan prioritas, dan metode pembagian laba
tersisa sebelum skema diaktifkan.

## Editor waterfall

Editor terdiri dari empat bagian berurutan:

1. identitas skema;
2. peserta, termasuk perusahaan, mitra pengelola, investor pasif, atau peran
   lain;
3. potongan prioritas berupa biaya pengelolaan atau imbal hasil modal;
4. pembagian laba tersisa kepada satu peserta, proporsional terhadap modal, atau
   menggunakan persentase tetap.

Peserta, aturan prioritas, dan persentase tetap dapat diurutkan dengan
drag-and-drop. Tombol naik dan turun selalu tersedia sebagai alternatif yang
lebih mudah diakses dan sebagai fallback pada perangkat sentuh. Implementasi
memakai API drag-and-drop browser dan helper urutan murni dari Stage 3A sehingga
tidak menambah dependency frontend.

Tarif tetap disimpan sebagai pembilang dan penyebut. Tampilan persentase hanya
merupakan bantuan visual; pecahan seperti `1/3` tidak dibulatkan saat disimpan.

## Versi dan izin

- Skema **Draf** dapat diubah dengan izin `profit-sharing.write`.
- Aktivasi membutuhkan izin `profit-sharing.finalize`.
- Skema **Aktif** tidak dapat diedit langsung. Pengguna membuat versi draf baru
  yang menyalin seluruh konfigurasi versi aktif.
- Aktivasi versi baru membuat versi aktif sebelumnya berstatus **Digantikan**.
- Semua versi lama tetap tampil sebagai jejak audit.

## Batas Stage 3B

Stage ini hanya membangun katalog dan editor skema tingkat organisasi. Pemilihan
skema aktif untuk suatu siklus, preview angka dari modal aktual, finalisasi
waterfall, dan histori settlement V2 tetap berada pada Stage 3C.
