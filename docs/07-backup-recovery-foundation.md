# Sprint 20C1 - Backup and Restore Foundation

## Tujuan

Sprint 20C1 menyediakan jalur backup PostgreSQL yang dapat diverifikasi dan latihan restore yang sepenuhnya terisolasi. Scope ini tidak mengubah domain, kontrak API, frontend, migration, dependency, atau database pengembangan.

Artefak operasional:

- `operations/Backup-SiPaculPostgres-PS51.ps1`
- `operations/Test-SiPaculPostgresRestore-PS51.ps1`

Keduanya kompatibel dengan Windows PowerShell 5.1 dan memakai `docker.exe` secara eksplisit.

## Batas keselamatan

- Backup hanya membaca service `postgres` milik stack Compose yang dipilih.
- Folder output wajib berada di luar repository.
- Archive biner dipindahkan dengan `docker cp`; PowerShell tidak mengalirkan byte dump melalui redirection teks.
- Latihan restore membuat container `sipacul-restore-drill-*` dengan network `none`, tanpa port host dan tanpa bind mount.
- Volume data anonim dari image PostgreSQL dihapus bersama container latihan.
- Container pengembangan bernama `sipacul-postgres-dev` hanya dibaca tanda tangannya sebelum dan sesudah latihan, tidak pernah dipilih sebagai target.
- Skrip latihan tidak menerima connection string atau target database produksi.
- Jalankan restore hanya untuk backup dari sumber yang dipercaya. Isi dump dapat menyebabkan kode database dijalankan pada target restore; isolasi container membatasi target tetapi tidak mengubah aturan kepercayaan tersebut.
- Tidak ada retensi otomatis atau penghapusan backup pada Sprint 20C1.

## Format backup

Backup dibuat dengan `pg_dump --format=custom` dan kompresi. Format custom dibaca kembali oleh `pg_restore --list` sebelum file difinalisasi. Setiap backup menghasilkan tiga file berdampingan:

```text
sipacul-postgres-YYYYMMDDTHHmmssfffZ.dump
sipacul-postgres-YYYYMMDDTHHmmssfffZ.dump.sha256
sipacul-postgres-YYYYMMDDTHHmmssfffZ.dump.json
```

Manifest JSON mencatat:

- waktu UTC;
- nama database;
- migration EF Core terakhir;
- image PostgreSQL;
- versi `pg_dump`;
- nama dan ukuran archive;
- SHA256.

SHA256 mendeteksi perubahan atau kerusakan tidak disengaja. SHA256 bukan tanda tangan kriptografis dan tidak membuktikan siapa yang membuat backup.

## Membuat backup

Pastikan stack produksi aktif dan file `.env.production` tersedia, lalu jalankan dari repository:

```powershell
Set-Location D:\Development\Projects\SiPacul

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Backup-SiPaculPostgres-PS51.ps1
```

Default output:

```text
%USERPROFILE%\SiPaculBackups
```

Folder lain di luar repository dapat dipilih secara eksplisit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Backup-SiPaculPostgres-PS51.ps1 `
  -OutputDirectory E:\SiPaculBackups
```

Untuk stack Compose dengan project name khusus:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Backup-SiPaculPostgres-PS51.ps1 `
  -EnvironmentFile C:\Secure\sipacul.env `
  -ComposeProject sipacul-production `
  -OutputDirectory E:\SiPaculBackups
```

Skrip gagal tertutup bila service PostgreSQL tidak tunggal/sehat, migration tidak dapat dibaca, archive kosong, `pg_restore --list` gagal, hash berubah, atau state Git berubah.

## Latihan restore

Gunakan file `.dump` yang dihasilkan oleh skrip backup. Sidecar `.sha256` dan `.json` harus tetap berada di folder yang sama.

```powershell
Set-Location D:\Development\Projects\SiPacul

$backup = Join-Path $env:USERPROFILE `
  "SiPaculBackups\sipacul-postgres-YYYYMMDDTHHmmssfffZ.dump"

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPostgresRestore-PS51.ps1 `
  -BackupFile $backup
```

Latihan memverifikasi:

1. nama, ukuran, SHA256, dan manifest;
2. daftar isi archive melalui `pg_restore --list`;
3. restore penuh dengan `--exit-on-error`;
4. migration terakhir dan jumlah riwayat migration;
5. jumlah tabel public dan keberadaan `SeasonReviews`;
6. tidak ada port host, bind mount, atau jaringan container;
7. cleanup container serta volume anonim;
8. container pengembangan tetap identik.

## Kebijakan operasional minimum

Rekomendasi awal untuk satu instalasi produksi:

- backup harian;
- simpan sekurangnya 30 titik pemulihan lokal sesuai kapasitas;
- salin backup ke lokasi off-host yang terenkripsi;
- lakukan latihan restore sekurangnya setiap kuartal dan setelah upgrade PostgreSQL besar;
- pantau exit code, ukuran archive, SHA256, migration, dan usia backup terakhir.

Retensi dan penyalinan off-host belum diotomasi pada Sprint 20C1. Hapus backup hanya melalui prosedur terpisah dengan target dan kebijakan yang telah disetujui.

## Yang tidak tercakup

`pg_dump` ini mencakup satu database aplikasi. Role, tablespace, konfigurasi server, secret `.env.production`, image aplikasi, dan key ring ASP.NET Data Protection harus diamankan melalui prosedur terpisah.

Restore ke produksi sengaja tidak disediakan. Pemulihan produksi memerlukan maintenance window, backup keadaan terkini, target yang diverifikasi, persetujuan eksplisit, rencana rollback, dan validasi aplikasi setelah restore.

## Referensi PostgreSQL

- <https://www.postgresql.org/docs/17/app-pgdump.html>
- <https://www.postgresql.org/docs/17/app-pgrestore.html>
- <https://www.postgresql.org/docs/17/backup-dump.html>

## Definition of Done Sprint 20C1

- backup archive custom dapat dibuat dari stack Compose;
- checksum dan manifest konsisten;
- archive dapat dipulihkan pada PostgreSQL sementara yang terisolasi;
- migration dan schema penting terverifikasi;
- seluruh resource latihan dibersihkan;
- container pengembangan tidak berubah;
- repository hanya mendapat dua skrip operasional dan dokumen ini.
