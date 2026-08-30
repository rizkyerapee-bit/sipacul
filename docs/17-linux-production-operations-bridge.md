# Sprint 20D2G3C5B - Linux Production Operations Bridge

Sprint 20D2G3C5B menambahkan implementasi Linux-native untuk kontrak deployment,
pre-deploy backup, dan emergency application rollback SiPacul pada Ubuntu
production host.

Tahap ini tidak mengganti kontrak Sprint 20D2G2. Ia mempertahankan invariant yang
sama dan hanya mengganti host execution layer Windows PowerShell menjadi Bash,
Docker Engine, Git, dan Python 3 yang tersedia pada Ubuntu 24.04.

## Artefak

```text
operations/linux/sipacul-common.sh
operations/linux/backup-postgres.sh
operations/linux/deploy.sh
operations/linux/application-rollback.sh
operations/Test-SiPaculLinuxOperationsBridge-PS51.ps1
docs/17-linux-production-operations-bridge.md
```

Windows production operations yang sudah ada tidak dihapus atau dimodifikasi.
Mereka tetap menjadi reference contract dan tetap dapat dipakai pada Windows host.

## Production path contract

Default Linux path:

```text
Repository:
  /opt/sipacul/repository

Production secret environment:
  /etc/sipacul/.env.production

Deployment state:
  /var/lib/sipacul/deployment-state/
    current-deployment.json
    current-release.env
    pending-operation.json
    history/

PostgreSQL backup:
  /var/backups/sipacul
```

State, backup, TLS secret, dan `.env.production` tetap berada di luar repository.

Semua mutation script Linux harus dijalankan melalui `sudo`. Operator
`sipaculadmin` tidak perlu menjadi anggota group `docker`.

## Dependencies

Host production memerlukan:

```text
bash
docker + docker compose
git
python3
realpath
sha256sum
```

Tidak ada registry credential yang disimpan oleh script.

Jika package GHCR private, operator harus melakukan `docker login ghcr.io`
secara terpisah sebelum execute deployment.

## Dua environment file

Kontrak tetap sama dengan Sprint 20D2G2:

1. `/etc/sipacul/.env.production` menyimpan database secret, bootstrap token,
   public activation config, network config, dan TLS path;
2. `current-release.env` hanya menyimpan empat immutable image reference.

Image reference memakai pola:

```text
ghcr.io/<owner>/sipacul-<component>:sha-<40-character-git-sha>
```

Normal deployment memakai SHA sama untuk migrator, API, frontend, dan edge.

Application rollback mempertahankan migrator pada `databaseReleaseSha` dan hanya
mengubah API, frontend, dan edge ke `runtimeReleaseSha` target.

## Plan-only adalah default

Linux deployment tidak melakukan mutation tanpa `--execute`.

Contoh plan:

```bash
sudo ./operations/linux/deploy.sh \
  --release-sha <40-character-git-sha>
```

Plan melakukan:

1. validasi repository dan working tree;
2. validasi production environment;
3. validasi managed deployment state;
4. membuat release environment sementara di `/tmp`;
5. menjalankan `docker compose config --quiet`;
6. menghapus temporary release environment.

Plan tidak pull image, tidak mengubah container, database, volume, state, secret,
DNS, firewall, certificate, public bind, atau HSTS.

## Initial managed deployment

Initial execute wajib acknowledgement eksplisit:

```bash
sudo ./operations/linux/deploy.sh \
  --release-sha <40-character-git-sha> \
  --execute \
  --allow-initial-deployment-without-backup
```

Flag tersebut hanya benar bila:

- `current-deployment.json` belum ada;
- tidak ada container dengan Compose project `sipacul-production`;
- database production memang belum pernah dibuat.

Unmanaged stack tidak diadopsi otomatis.

## Normal deployment

Normal execute:

1. pull empat immutable GHCR image;
2. verifikasi OCI label `org.opencontainers.image.revision`;
3. pastikan PostgreSQL existing healthy;
4. buat satu pre-deploy PostgreSQL backup;
5. tulis `pending-operation.json`;
6. tulis target `current-release.env`;
7. pertahankan PostgreSQL dan volume;
8. stop edge, frontend, dan API;
9. jalankan migrator target sebagai migration gate;
10. start API, frontend, dan edge satu per satu;
11. tunggu health check setiap runtime service;
12. tulis `current-deployment.json` dan history;
13. hapus pending operation.

Migration failure atau runtime health failure tidak memicu restore database atau
application rollback otomatis.

## PostgreSQL backup

`backup-postgres.sh` mempertahankan format backup yang sudah diterima:

```text
sipacul-postgres-<UTC timestamp>.dump
sipacul-postgres-<UTC timestamp>.dump.sha256
sipacul-postgres-<UTC timestamp>.dump.json
```

Backup:

- menuntut tepat satu container PostgreSQL managed;
- menuntut PostgreSQL healthy;
- membaca latest EF migration;
- menggunakan `pg_dump --format=custom --compress=9`;
- memvalidasi archive melalui `pg_restore --list`;
- menyalin archive ke host melalui temporary filename;
- membuat SHA256 dan manifest schemaVersion 1;
- finalisasi melalui rename;
- membersihkan temporary file pada kegagalan;
- memastikan Git HEAD dan working tree tidak berubah.

Script tidak pernah menjalankan `pg_restore` terhadap database production.
`pg_restore --list` hanya membaca struktur archive untuk validasi.

## Emergency application rollback

Plan rollback:

```bash
sudo ./operations/linux/application-rollback.sh
```

Execute membutuhkan acknowledgement schema compatibility:

```bash
sudo ./operations/linux/application-rollback.sh \
  --execute \
  --acknowledge-database-compatibility
```

Rollback hanya tersedia jika `previousRuntimeReleaseSha` ada.

Urutan execute:

1. verifikasi API/frontend/edge target;
2. pastikan PostgreSQL healthy;
3. buat pre-rollback backup;
4. tulis pending operation;
5. stop edge/frontend/API;
6. pertahankan migrator pada `databaseReleaseSha`;
7. start API/frontend/edge target tanpa migrator;
8. tunggu health check;
9. simpan state dan history.

Rollback tidak:

- menjalankan migrator;
- menjalankan database restore;
- menurunkan schema;
- menghapus PostgreSQL volume;
- menghapus Data Protection key volume.

Setelah rollback, `databaseReleaseSha` dan `runtimeReleaseSha` boleh berbeda.
Operator wajib membuktikan runtime lama kompatibel dengan schema existing sebelum
execute.

## Failure safety

Linux bridge tidak pernah:

- menjalankan `docker compose down --volumes`;
- menghapus PostgreSQL volume;
- menghapus Data Protection key volume;
- melakukan production `pg_restore`;
- melakukan schema downgrade;
- melakukan rollback otomatis setelah migration failure;
- mengubah `.env.production`;
- menyimpan GHCR credential;
- membuka public bind;
- mengubah DNS;
- mengubah firewall;
- meminta atau mengganti certificate;
- mengaktifkan HSTS.

Jika execute gagal setelah pending operation dibuat, pending state dipertahankan.
Deployment/rollback berikutnya harus ditolak sampai investigasi selesai.

## Public activation tetap terpisah

Linux bridge tidak mengubah public activation contract.

Sampai cutover eksplisit:

```dotenv
SIPACUL_PUBLIC_ACTIVATION=disabled
SIPACUL_PUBLIC_HOSTNAME=_
SIPACUL_HSTS_ENABLED=false
SIPACUL_BIND_ADDRESS=127.0.0.1
SIPACUL_HTTPS_PORT=8443
```

Port 443, DNS, certificate automation, dan external endpoint probe tetap berada
pada fase public cutover sesudah release frontend siap.

## Backup scheduler

Sprint ini hanya menyediakan backup primitive yang dipakai oleh deployment dan
rollback. Linux scheduled backup, retention, freshness audit, dan systemd timer
akan dibuat sebagai scope terpisah agar lifecycle scheduler dapat divalidasi
tanpa memperbesar blast radius deployment bridge.

## Acceptance

Sprint 20D2G3C5B dianggap siap ketika:

1. Windows production tooling existing tidak berubah;
2. Linux deployment default plan-only;
3. initial execute membutuhkan acknowledgement eksplisit;
4. normal deployment memakai empat immutable image dari SHA sama;
5. OCI revision label diverifikasi sebelum mutation runtime;
6. pre-deploy backup menghasilkan custom archive, SHA256, dan manifest;
7. migration tetap gate terpisah;
8. API/frontend/edge harus lulus health check berurutan;
9. state tetap membedakan database dan runtime release SHA;
10. application rollback tidak menjalankan migrator atau restore database;
11. failed execute mempertahankan pending operation;
12. state/backup/secret tetap di luar repository;
13. tidak ada volume deletion, DNS, firewall, certificate, atau public mutation.
