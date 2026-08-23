# Sprint 20C2A - Backup Automation Lifecycle

## Tujuan

Sprint 20C2A menambahkan primitive yang aman untuk menjalankan backup PostgreSQL SiPacul secara berulang:

- lock eksklusif agar dua siklus untuk repository dan folder yang sama tidak berjalan bersamaan;
- log operasional persisten di luar repository;
- pemeriksaan pasangan archive, SHA256, dan manifest;
- pemeriksaan usia backup terbaru;
- retensi dengan dry-run sebagai default;
- perlindungan jumlah minimum backup terbaru;
- penghapusan melalui direktori transaksi terisolasi.

Registrasi Windows Task Scheduler sengaja dipisahkan ke Sprint 20C2B. Dengan demikian task otomatis hanya dibuat setelah primitive lifecycle ini lulus pada lingkungan aktual.

## Artefak

- `operations/SiPaculBackupSet.psm1`
- `operations/Invoke-SiPaculBackupCycle-PS51.ps1`
- `operations/Invoke-SiPaculBackupRetention-PS51.ps1`
- `operations/Test-SiPaculBackupFreshness-PS51.ps1`

Skrip memakai archive yang dibuat oleh `operations/Backup-SiPaculPostgres-PS51.ps1` dari Sprint 20C1.

## Menjalankan satu siklus manual

Pastikan stack produksi aktif dan `.env.production` tersedia:

```powershell
Set-Location D:\Development\Projects\SiPacul

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculBackupCycle-PS51.ps1
```

Default yang digunakan:

- output: `%USERPROFILE%\SiPaculBackups`;
- log: `%USERPROFILE%\SiPaculBackups\logs\backup-cycle.log`;
- masa retensi: 30 hari;
- minimum yang selalu dilindungi: 7 backup;
- batas freshness: 26 jam;
- retensi hanya dry-run.

Untuk menerapkan penghapusan backup yang memenuhi kebijakan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculBackupCycle-PS51.ps1 `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention
```

Setiap siklus:

1. memperoleh mutex lokal berdasarkan repository dan folder output;
2. menjalankan backup C1 dalam proses PowerShell terpisah;
3. memastikan tepat satu archive baru terbentuk;
4. mengevaluasi atau menerapkan retensi;
5. memeriksa freshness dan SHA256 archive terbaru;
6. memastikan HEAD dan working tree tidak berubah;
7. menulis status setiap tahap ke log.

## Retensi mandiri

Dry-run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculBackupRetention-PS51.ps1 `
  -BackupDirectory "$env:USERPROFILE\SiPaculBackups" `
  -RetentionDays 30 `
  -MinimumBackups 7
```

Penerapan setelah daftar dry-run diperiksa:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculBackupRetention-PS51.ps1 `
  -BackupDirectory "$env:USERPROFILE\SiPaculBackups" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -Apply
```

Retensi gagal tertutup bila menemukan:

- archive tanpa sidecar;
- sidecar tanpa archive;
- nama atau timestamp tidak valid;
- manifest tidak lengkap;
- ukuran atau metadata SHA256 tidak konsisten;
- SHA256 kandidat penghapusan tidak cocok.

Sebelum menghapus, seluruh kandidat dipindahkan ke direktori transaksi dengan nama unik. Jika pemindahan gagal, file yang telah dipindahkan dikembalikan. Direktori transaksi dihapus hanya setelah semua triplet berhasil dipindahkan.

## Pemeriksaan freshness

Pemeriksaan cepat memverifikasi SHA256 backup terbaru:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculBackupFreshness-PS51.ps1 `
  -BackupDirectory "$env:USERPROFILE\SiPaculBackups" `
  -MaxAgeHours 26 `
  -MinimumValidBackups 1
```

Audit seluruh archive dapat dijalankan berkala:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculBackupFreshness-PS51.ps1 `
  -BackupDirectory "$env:USERPROFILE\SiPaculBackups" `
  -MaxAgeHours 26 `
  -MinimumValidBackups 7 `
  -VerifyAllHashes
```

## Batas keselamatan

- Semua pencarian file bersifat non-rekursif dan hanya mengenali prefix `sipacul-postgres-`.
- Root volume ditolak sebagai folder backup.
- Archive, sidecar SHA256, dan manifest diperlakukan sebagai satu triplet.
- Dry-run tidak mengubah file.
- `-Apply` harus diberikan secara eksplisit untuk retensi.
- Jumlah minimum backup terbaru tidak pernah menjadi kandidat penghapusan.
- File tidak berpasangan menghentikan seluruh retensi sebelum penghapusan.
- Log dan backup harus berada di luar repository.
- Skrip tidak menerima target restore dan tidak mengubah database.

## Definition of Done Sprint 20C2A

- tiga siklus backup berurutan dapat dijalankan tanpa benturan lock;
- retensi menghapus hanya triplet tertua yang memenuhi kebijakan;
- minimum backup tetap dipertahankan;
- dry-run terbukti tidak menghapus file;
- file orphan menyebabkan retensi gagal tanpa menghapus backup valid;
- freshness dan seluruh SHA256 dapat diverifikasi;
- log menyimpan hasil setiap tahap;
- database, container pengembangan, Git, dan source aplikasi tidak berubah.

## Tahap berikutnya

Sprint 20C2B akan menambahkan registrasi dan pemeriksaan Windows Task Scheduler, termasuk command line yang dikunci ke repository, folder backup, waktu eksekusi, dan kebijakan retensi yang telah lulus di Sprint 20C2A.
