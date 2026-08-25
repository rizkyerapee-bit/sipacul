# Sprint 20D1 — Deterministic Release Gate

Sprint 20D1 menambahkan satu gerbang verifikasi yang sama untuk perubahan pada
branch utama dan pull request. Gate ini memastikan source yang akan menjadi
Release Candidate tetap dapat dibangun dan diuji tanpa bergantung pada stack
produksi lokal atau pilihan penyedia hosting.

## Gerbang otomatis

Workflow `.github/workflows/release-gate.yml` berjalan pada:

- push ke `main`;
- pull request menuju `main`;
- pemicu manual melalui GitHub Actions.

Hak workflow dibatasi menjadi `contents: read`. Checkout tidak mempertahankan
credential Git pada langkah berikutnya. Job backend dan frontend berjalan
paralel pada runner Linux dengan batas waktu masing-masing 20 menit.

Backend menggunakan .NET SDK 10 dan menjalankan seluruh solution dalam
konfigurasi `Release`. Frontend menggunakan Node.js 22, memasang dependency
secara deterministik melalui `npm ci`, lalu menjalankan test satu kali, lint,
dan production build.

## Gerbang lokal

Jalankan dari root repository pada Windows PowerShell 5.1:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculReleaseGate-PS51.ps1
```

Skrip lokal menjalankan backend Release test, frontend test, lint, dan build.
Skrip juga memastikan HEAD dan status Git tetap identik sebelum dan sesudah
gate. Skrip tidak menjalankan Docker, migration, database, staging, commit,
atau push.

## Batas checkpoint

Checkpoint ini belum:

- menerapkan rate limiting atau global exception handling;
- menambahkan browser end-to-end test;
- membangun atau menerbitkan image container di CI;
- membuat deployment, domain, TLS, registry, atau tag rilis;
- mengganti keputusan hosting.

Tahap tersebut tetap dipisah agar kegagalan quality gate dapat diperbaiki tanpa
risiko terhadap stack dan data produksi lokal.

## Aturan Release Candidate

Commit hanya dapat menjadi kandidat RC setelah job backend dan frontend hijau.
Setelah workflow pertama lulus, branch protection GitHub sebaiknya mewajibkan
kedua job ini sebelum merge. Aktivasi branch protection merupakan pengaturan
repository dan tidak dilakukan otomatis oleh checkpoint ini.
