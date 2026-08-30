# Sprint 20D2A — Container Build and Stack Smoke Gate

Sprint 20D2A memperluas Release gate dari verifikasi source menjadi verifikasi
artefak runtime. Checkpoint ini memastikan Dockerfile dan
`compose.production.yml` tetap dapat membentuk stack produksi yang sehat pada
host bersih, tanpa menerbitkan image atau mengakses database nyata.

## Cakupan gate

Skrip `operations/Test-SiPaculContainerReleaseGate-PS51.ps1`:

1. membuat konfigurasi dan rahasia acak di folder sementara;
2. membangun target migration, API runtime, frontend, dan edge dengan tag unik;
3. memulai stack Compose dengan project, volume, network, dan port loopback
   yang unik;
4. menunggu PostgreSQL, migration, API, frontend, dan edge mencapai state yang
   diwajibkan;
5. memeriksa migration terakhir, HTTPS `/login`, routing API langsung, token
   antiforgery, login, sanitasi forwarded header, publikasi port, serta
   keanggotaan network setiap service;
6. menghapus container, volume, network, image bertag, dan rahasia sementara;
7. memastikan stack produksi yang sudah ada serta state Git tidak berubah.

Skrip kompatibel dengan Windows PowerShell 5.1 dan PowerShell pada runner Linux.
Workflow menjalankannya sebagai job ketiga setelah checkout source. Setiap job
Release gate tetap independen dan menggunakan izin repository `contents: read`.

## Menjalankan secara lokal

Pastikan Docker Desktop aktif, lalu jalankan dari root repository:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculContainerReleaseGate-PS51.ps1
```

Parameter `StartupTimeoutSeconds` hanya mengatur waktu tunggu health stack dan
dibatasi antara 120–900 detik:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculContainerReleaseGate-PS51.ps1 `
  -StartupTimeoutSeconds 420
```

## Isolasi dan keselamatan

- `.env.production` tidak dibaca atau diubah.
- Password database dan bootstrap token sementara tidak dicetak.
- PostgreSQL, migrator, API, dan frontend tidak memublikasikan port host.
- Hanya edge TLS yang diikat ke `127.0.0.1` pada port acak.
- Nama project dan tag image memakai suffix acak untuk mencegah tabrakan.
- Cleanup hanya menargetkan project dan tag unik yang dibuat oleh satu run.
- Container produksi dengan prefix `sipacul-production-` diaudit sebelum dan
  sesudah gate.
- Build cache dan base image Docker dapat dipakai ulang, tetapi empat tag image
  hasil gate selalu dihapus.

## Batas checkpoint

Sprint 20D2A belum:

- menerbitkan image ke registry;
- membuat tag atau GitHub Release;
- menghasilkan kredensial deployment;
- memilih penyedia hosting, domain, atau sertifikat produksi;
- mengubah stack maupun database produksi;
- menjalankan backup atau restore produksi.

Publikasi image dan manifest Release Candidate dilakukan setelah container gate
ini hijau pada GitHub Actions dan tujuan registry/hosting telah ditetapkan.
