# Baseline Arsitektur SiPacul

Dokumen ini mencatat kondisi arsitektur SiPacul setelah modul Organisasi, Kategori Komoditas, Komoditas, dan SOP Budidaya selesai.

## Informasi Baseline

- Tanggal checkpoint: 30 Juli 2026
- Branch: `main`
- Commit baseline: `32549da`
- Repository: `rizkyerapee-bit/sipacul`
- Target framework: `.NET 10`
- Database: PostgreSQL 17
- ORM: Entity Framework Core 10
- API: ASP.NET Core Minimal API
- Pengujian: xUnit

## Tujuan Aplikasi

SiPacul adalah Sistem Pencatatan Akuntansi Usaha Lestari.

Aplikasi mencatat seluruh kegiatan usaha budidaya pertanian, mulai dari pengelolaan lahan, siklus budidaya, aktivitas lapangan, biaya, panen, penjualan, laba, investasi, sampai pembagian hasil.

Aplikasi dikembangkan untuk operasional internal terlebih dahulu, dengan struktur data yang disiapkan untuk model SaaS.

## Struktur Solution

Solution terdiri dari lima project utama:

1. `SiPacul.Domain`
2. `SiPacul.Application`
3. `SiPacul.Infrastructure`
4. `SiPacul.Api`
5. `SiPacul.Shared`

Terdapat empat project pengujian:

- `SiPacul.Domain.Tests`
- `SiPacul.Application.Tests`
- `SiPacul.Infrastructure.Tests`
- `SiPacul.Api.Tests`

## Modul yang Sudah Selesai

- Organisasi
- Kategori Komoditas
- Komoditas
- SOP Budidaya dan langkah budidaya

## Kondisi API

Jumlah endpoint HTTP pada baseline adalah 28:

- Organisasi: 6 endpoint
- Kategori Komoditas: 6 endpoint
- Komoditas: 6 endpoint
- SOP Budidaya: 10 endpoint

Seluruh endpoint menggunakan prefix `/api/v1`.

## Kondisi Persistence

`DbSet` yang tersedia:

- `Organizations`
- `CommodityCategories`
- `Commodities`
- `CultivationSops`
- `CultivationSopSteps`

Migration yang telah diterapkan:

1. `InitialCreate`
2. `AddCultivationSops`
3. `MakeCultivationSopStepOrderDeferrable`

Urutan langkah SOP dijaga oleh PostgreSQL menggunakan constraint:

```sql
UNIQUE (
    "OrganizationId",
    "CultivationSopId",
    "Sequence"
)
DEFERRABLE INITIALLY DEFERRED
```

## Isolasi Organisasi

Entity bisnis yang dimiliki organisasi menyimpan `OrganizationId`.

Query repository dibatasi berdasarkan organisasi. Foreign key gabungan digunakan pada relasi penting untuk mencegah record dari organisasi berbeda saling terhubung.

## Kualitas Baseline

- Build berhasil
- 195 pengujian otomatis berhasil
- Pengujian end-to-end SOP berhasil
- PostgreSQL berstatus healthy
- Seluruh migration telah diterapkan
- Tidak ditemukan paket NuGet rentan
- Tidak ditemukan `TODO`, `FIXME`, atau `HACK`

## Fitur yang Belum Tersedia

- Master lahan
- Master petak
- Siklus budidaya
- Aktivitas lapangan
- Biaya
- Panen
- Penjualan
- Investasi
- Perhitungan laba
- Pembagian hasil
- Histori tanaman per lahan
- Evaluasi musim
- Autentikasi dan otorisasi
- Frontend
- CI/CD
- Deployment produksi

## Modul Berikutnya

Modul berikutnya adalah Lahan dan Petak. Modul ini menjadi fondasi untuk siklus budidaya, histori tanaman per lahan, evaluasi musim, aktivitas lapangan, biaya per lahan, dan produktivitas per petak.
