# Sprint 20C2B - Windows Task Scheduler Integration

## Tujuan

Sprint 20C2B menghubungkan siklus backup Sprint 20C2A dengan Windows Task Scheduler tanpa menyimpan kata sandi dan tanpa memberikan hak administrator kepada proses backup.

Fondasi ini menyediakan:

- kontrak task yang deterministik dan dapat diaudit;
- registrasi jadwal harian;
- current-user interactive token dengan run level terbatas;
- pencegahan dua instance melalui `IgnoreNew`, ditambah mutex dari Sprint 20C2A;
- restart maksimal dua kali dengan interval sepuluh menit;
- batas eksekusi empat jam;
- audit command line, principal, trigger, settings, state, dan riwayat runtime;
- unregister yang hanya menghapus task milik repository yang sama.

## Artefak

- `operations/SiPaculBackupTask.psm1`
- `operations/Register-SiPaculBackupTask-PS51.ps1`
- `operations/Test-SiPaculBackupTask-PS51.ps1`
- `operations/Unregister-SiPaculBackupTask-PS51.ps1`

## Model keamanan

Task didaftarkan pada root Task Scheduler dengan nama default `SiPacul-PostgreSQL-Backup`. Task berjalan sebagai akun Windows yang melakukan registrasi dengan `InteractiveToken` dan `LeastPrivilege`.

Konsekuensinya:

- kata sandi Windows tidak disimpan oleh skrip;
- task hanya dapat berjalan ketika sesi pengguna tersebut tersedia;
- Docker Desktop dan stack produksi harus dapat diakses oleh akun yang sama;
- task tidak meminta elevasi administrator;
- task lain dengan nama sama tidak boleh ditimpa;
- `-Force` hanya dapat memperbarui task yang memiliki ownership marker SiPacul untuk repository yang sama.

## Konfigurasi yang direkomendasikan

- waktu harian: `02:00`;
- output: `%USERPROFILE%\SiPaculBackups`;
- log: `%USERPROFILE%\SiPaculBackups\logs\backup-cycle.log`;
- retensi: 30 hari;
- minimum terlindungi: 7 backup;
- freshness: 26 jam;
- retensi apply: aktif;
- verifikasi seluruh hash: opsional karena biayanya bertambah seiring jumlah backup.

## Aktivasi bertahap

Pastikan stack produksi dan `.env.production` sudah siap. Daftarkan task dalam keadaan disabled terlebih dahulu:

```powershell
Set-Location D:\Development\Projects\SiPacul

powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Register-SiPaculBackupTask-PS51.ps1 `
  -StartTime "02:00" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention `
  -Disabled
```

Audit kontrak disabled:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculBackupTask-PS51.ps1 `
  -StartTime "02:00" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention `
  -ExpectedDisabled
```

Aktifkan dengan registrasi ulang terkontrol:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Register-SiPaculBackupTask-PS51.ps1 `
  -StartTime "02:00" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention `
  -Force
```

Audit kontrak aktif:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculBackupTask-PS51.ps1 `
  -StartTime "02:00" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention
```

## Audit setelah eksekusi pertama

Setelah jadwal pertama berlalu, wajibkan hasil terakhir sukses dan tidak lebih tua dari 26 jam:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculBackupTask-PS51.ps1 `
  -StartTime "02:00" `
  -RetentionDays 30 `
  -MinimumBackups 7 `
  -FreshnessHours 26 `
  -ApplyRetention `
  -RequireLastRunSuccess `
  -MaxLastRunAgeHours 26
```

Audit ini memeriksa konfigurasi Task Scheduler dan hasil proses. Freshness archive serta SHA256 tetap diperiksa oleh siklus backup C2A.

## Menghapus task

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Unregister-SiPaculBackupTask-PS51.ps1
```

Unregister bersifat idempotent. Skrip menolak penghapusan bila ownership marker tidak cocok atau task sedang berjalan. Archive backup dan log tidak ikut dihapus.

## Batas keselamatan

- Registrasi hanya menggunakan root Task Scheduler; nama task tidak menerima path atau karakter kontrol.
- Output dan log wajib berada di luar repository dan bukan root volume.
- Command line dibentuk dari nilai satu baris tanpa tanda kutip ganda.
- Task memakai satu action PowerShell dan satu trigger harian.
- Task tidak menjalankan `git`, migration, restore, atau perubahan database secara langsung.
- `StartWhenAvailable` aktif agar task yang terlewat dapat dimulai saat sesi kembali tersedia.
- Task dapat berjalan ketika perangkat menggunakan baterai dan tidak dihentikan saat beralih ke baterai.
- Registrasi yang gagal dipulihkan ke definisi sebelumnya; task baru yang gagal diverifikasi dihapus.
- Unregister tidak menghentikan siklus backup yang sedang berjalan.

## Definition of Done Sprint 20C2B

- task disabled sementara dapat didaftarkan tanpa elevasi atau kata sandi;
- command line memanggil tepat siklus backup C2A dengan konfigurasi yang diminta;
- principal, trigger harian, settings, dan ownership marker tervalidasi;
- registrasi berbeda dengan nama sama ditolak tanpa `-Force`;
- task sementara dapat dihapus melalui skrip terkelola;
- unregister kedua tetap sukses tanpa perubahan;
- task SiPacul yang telah ada tidak berubah;
- repository, database, container, backup produksi, dan log produksi tidak berubah selama drill.

## Tahap berikutnya

Sprint 20C2C akan melakukan aktivasi terkontrol pada konfigurasi produksi aktual, menjalankan satu siklus manual melalui command line yang sama, lalu menetapkan checklist pemantauan setelah jadwal pertama.
