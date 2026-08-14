# Profit Sharing V2 — Stage 2C1 Final Snapshot Preview

## Tujuan

Stage 2C1 membangun aggregate domain untuk hasil akhir waterfall yang dapat
diaudit. Aggregate ini berbeda dari settlement lama `SIPACUL-PS-1` dan tidak
mengubah histori lama.

Snapshot final menyimpan:

- identitas assignment, skema sumber, keluarga skema, dan versinya;
- angka profitabilitas yang menjadi dasar perhitungan;
- versi kalkulator `SIPACUL-PS-2`;
- seluruh priority allocation beserta rate, requested, allocated, dan
  unallocated;
- seluruh participant allocation, termasuk modal, pengembalian modal,
  kerugian modal, bagian laba, dan payout;
- definisi fixed residual share bila digunakan;
- waktu perhitungan dan finalisasi;
- status final atau void beserta alasan void.

## Immutability

Setelah dibuat, seluruh angka dan snapshot kontrak tidak memiliki operasi
perubahan. Satu-satunya transisi yang diizinkan adalah:

```text
Finalized -> Voided
```

Void tidak menghapus atau menghitung ulang hasil. Snapshot tetap tersedia
untuk audit.

## Invariant final

Aggregate menolak finalisasi bila:

- organisasi atau siklus pada assignment, profitabilitas, dan hasil kalkulasi
  tidak sama;
- pendapatan masih memiliki piutang;
- masih ada hasil panen tersedia;
- biaya budidaya nol;
- modal terkonfirmasi tidak sama dengan biaya budidaya;
- peserta atau priority rule pada hasil kalkulasi tidak sama dengan snapshot
  assignment;
- jumlah recovery dan loss tidak sama dengan modal;
- jumlah bagian laba atau payout anak tidak sama dengan total root;
- total payout tidak sama dengan recognized revenue;
- waktu finalisasi bukan UTC.

## Batas Stage 2C1

Tahap ini hanya menambahkan domain aggregate dan automated test. Belum ada
database, migration, repository, endpoint, finalization transaction, source
locking lintas versi, staging, atau commit. Stage 2C2 akan menerapkan persistence
dan finalisasi atomik setelah kontrak immutable ini stabil.
