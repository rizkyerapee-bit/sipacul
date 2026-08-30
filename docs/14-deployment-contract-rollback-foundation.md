# Sprint 20D2G2 - Deployment Contract and Rollback Foundation

Sprint 20D2G2 menambahkan kontrak deployment provider-neutral untuk release
container SiPacul yang sudah diterbitkan ke GHCR pada Sprint 20D2G1.

Tahap ini menyediakan tooling deployment host, tetapi tidak memilih provider,
domain, DNS, public bind, certificate automation, firewall, CDN, atau HSTS.

## Artefak

- `operations/SiPaculDeployment.psm1`
- `operations/Invoke-SiPaculDeployment-PS51.ps1`
- `operations/Invoke-SiPaculApplicationRollback-PS51.ps1`

Semua script entrypoint kompatibel dengan Windows PowerShell 5.1.

## Dua environment file

Secret produksi tetap berada pada `.env.production`. Deployment tooling tidak
mengubah file itu.

Image release disimpan terpisah di luar repository:

```text
%USERPROFILE%\SiPaculDeploymentState\current-release.env
```

Setiap command Compose milik deployment menggunakan dua `--env-file`:

```powershell
docker.exe compose `
  --env-file .env.production `
  --env-file $env:USERPROFILE\SiPaculDeploymentState\current-release.env `
  --file compose.production.yml `
  --project-name sipacul-production `
  ...
```

File kedua hanya memuat empat `SIPACUL_*_IMAGE` dan mengoverride default image
lokal dari file produksi. Secret database, bootstrap token, dan path TLS tetap
berasal dari `.env.production`.

## Normal deployment

Normal deployment menerima full Git SHA 40 karakter.

Empat image target harus:

1. berada di GHCR owner yang dipilih;
2. memakai tag `sha-<full-sha>`;
3. dapat dipull oleh Docker host;
4. memiliki OCI label `org.opencontainers.image.revision` yang sama dengan SHA
   target.

Host harus sudah login ke GHCR bila package tidak public. Tooling ini tidak
menyimpan atau membuat token registry.

### Plan-only

Tanpa `-Execute`, deployment hanya melakukan preflight dan validasi Compose:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculDeployment-PS51.ps1 `
  -ReleaseSha <full-40-character-sha>
```

Plan-only tidak melakukan pull image, backup, migration, start/stop container,
atau menulis deployment state.

### Execute

Deployment existing:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculDeployment-PS51.ps1 `
  -ReleaseSha <full-40-character-sha> `
  -Execute
```

Initial installation tidak memiliki database existing untuk dibackup dan harus
diakui secara eksplisit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculDeployment-PS51.ps1 `
  -ReleaseSha <full-40-character-sha> `
  -Execute `
  -AllowInitialDeploymentWithoutBackup
```

Flag tersebut hanya sah untuk project Compose yang belum mempunyai managed
deployment state maupun container.

## Urutan deployment

1. repository harus clean;
2. `.env.production`, secret, dan TLS path divalidasi;
3. target Compose config divalidasi;
4. empat immutable GHCR image dipull dan revision label diverifikasi;
5. untuk deployment existing, PostgreSQL dibuat sehat lalu
   `Backup-SiPaculPostgres-PS51.ps1` dijalankan;
6. pending operation ditulis ke state directory di luar repository;
7. release environment target ditulis;
8. PostgreSQL dipertahankan;
9. edge, frontend, dan API dihentikan untuk maintenance;
10. migrator target dijalankan sebagai gate;
11. API, frontend, dan edge dimulai berurutan dengan `--no-deps`;
12. health setiap service harus lulus;
13. current state dan history difinalisasi.

Migration failure atau post-migration health failure tidak memicu database
restore maupun application rollback otomatis. Pending state dipertahankan untuk
investigasi.

## Backup

Deployment tidak membuat implementasi backup kedua. Ia memanggil:

```text
operations/Backup-SiPaculPostgres-PS51.ps1
```

dengan environment file, Compose project, dan output directory yang sama.

Backup existing tetap menghasilkan archive custom PostgreSQL, SHA256, dan
manifest. Deployment hanya melanjutkan bila script backup existing sukses dan
tepat satu backup set baru terdeteksi.

Default backup directory:

```text
%USERPROFILE%\SiPaculBackups
```

## State

Default:

```text
%USERPROFILE%\SiPaculDeploymentState
```

State directory wajib berada di luar repository.

File penting:

```text
current-deployment.json
current-release.env
pending-operation.json
history\
```

`current-deployment.json` membedakan dua release:

- `databaseReleaseSha`: SHA migrator terakhir yang berhasil dijalankan;
- `runtimeReleaseSha`: SHA API/frontend/edge yang sedang aktif.

Pada normal deployment, keduanya identik.

## Application rollback

Rollback hanya tersedia bila `previousRuntimeReleaseSha` terdapat pada state.

Plan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculApplicationRollback-PS51.ps1
```

Execute membutuhkan acknowledgement eksplisit:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Invoke-SiPaculApplicationRollback-PS51.ps1 `
  -Execute `
  -AcknowledgeDatabaseCompatibility
```

Rollback melakukan backup PostgreSQL terlebih dahulu, lalu hanya mengganti:

- API;
- frontend;
- edge.

Migrator tetap menunjuk `databaseReleaseSha` dan tidak dijalankan. Database tidak
direstore dan schema tidak didowngrade.

Setelah rollback, `databaseReleaseSha` dan `runtimeReleaseSha` dapat berbeda.
State tersebut disengaja dan menandai emergency application rollback.

Operator hanya boleh menjalankan rollback setelah memastikan runtime lama tetap
kompatibel dengan schema database yang sudah ada.

## Failure safety

Tooling tidak pernah:

- menjalankan `docker compose down --volumes`;
- menghapus PostgreSQL volume;
- menghapus Data Protection key volume;
- menjalankan `pg_restore` terhadap produksi;
- menjalankan EF migration pada application rollback;
- melakukan schema downgrade otomatis;
- melakukan rollback otomatis setelah migration failure;
- mengubah `.env.production`;
- menyimpan registry credential;
- membuka public bind;
- mengubah DNS, firewall, atau certificate.

Jika operasi execute gagal setelah dimulai, `pending-operation.json` tetap ada.
Deployment atau rollback berikutnya ditolak sampai kegagalan tersebut
diinvestigasi.

## GHCR authentication

Host deployment bertanggung jawab melakukan login registry sebelum execute jika
package GHCR bersifat private. Credential tidak masuk repository atau deployment
state.

Contoh login bergantung pada credential policy host dan sengaja tidak
diotomasi pada sprint ini.

## Batas scope

Sprint 20D2G2 belum menyediakan:

- provisioning server;
- remote SSH deployment;
- GitHub Actions deployment ke host;
- domain dan DNS;
- public bind;
- ACME/certificate renewal;
- firewall automation;
- HSTS;
- production database restore;
- automatic schema rollback.

Hal-hal yang mengaktifkan exposure publik tetap menjadi Sprint 20D2G3.

## Acceptance

Sprint 20D2G2 dianggap siap ketika:

1. deployment dan rollback default ke plan-only;
2. execute normal memakai immutable GHCR image;
3. empat image normal deployment berasal dari SHA yang sama;
4. pre-deploy backup memakai script backup existing;
5. migration menjadi gate terpisah sebelum runtime baru;
6. runtime baru harus lulus health checks;
7. current state membedakan database dan runtime SHA;
8. rollback tidak menjalankan migrator atau restore database;
9. state dan release env berada di luar repository;
10. tidak ada volume deletion atau secret mutation.
