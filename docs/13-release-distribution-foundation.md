# Sprint 20D2G1 - Release Distribution Foundation

Sprint 20D2G1 menambahkan jalur distribusi image produksi SiPacul yang
provider-neutral. Tahap ini tidak melakukan deployment publik.

## Tujuan

Empat image yang dipakai `compose.production.yml` dapat diterbitkan ke GitHub
Container Registry (GHCR):

- `sipacul-migrator`;
- `sipacul-api`;
- `sipacul-frontend`;
- `sipacul-edge`.

Setiap publikasi memakai tag immutable berbasis full Git commit SHA:

```text
sha-<40-character-git-sha>
```

Contoh bentuk referensi:

```text
ghcr.io/<owner>/sipacul-api:sha-<full-git-sha>
```

Tidak ada tag `latest` pada foundation ini. Deployment harus memilih versi
secara eksplisit.

## Workflow

Workflow:

```text
.github/workflows/publish-release-images.yml
```

hanya dapat dimulai melalui `workflow_dispatch`.

Workflow tidak berjalan otomatis pada setiap push `main`. Sebelum melakukan
publish, workflow harus:

1. berjalan dari `refs/heads/main`;
2. menemukan `Release gate` event `push` yang sudah `success` untuk SHA yang
   sama;
3. login ke GHCR dengan `GITHUB_TOKEN`;
4. memastikan keempat tag SHA belum ada di registry;
5. membangun dan mendorong keempat image;
6. mencatat image tag dan digest pada GitHub Actions step summary.

Jika salah satu tag SHA sudah ada, workflow gagal sebelum build/push. Kebijakan
ini mencegah workflow memindahkan tag yang sudah diperlakukan sebagai immutable.

## Mapping build

| Image | Dockerfile | Target / build input |
| --- | --- | --- |
| Migrator | `backend/Dockerfile` | target `migration` |
| API | `backend/Dockerfile` | target `runtime` |
| Frontend | `frontend/Dockerfile` | `SIPACUL_API_ORIGIN=http://api:8080` |
| Edge | `edge/Dockerfile` | default final stage |

Mapping tersebut sama dengan build surface pada `compose.production.yml` dan
container release gate.

## Permissions

Workflow memakai permission minimum berikut:

```yaml
contents: read
actions: read
packages: write
```

`actions: read` hanya digunakan untuk memverifikasi Release gate pada SHA yang
akan dipublikasikan. `packages: write` digunakan untuk GHCR. Workflow tidak
membutuhkan PAT atau deployment secret baru.

## Menggunakan image pada deployment

`production.env.example` tetap memakai image lokal secara default agar
development dan container gate tidak berubah.

Pada deployment yang sudah memilih release SHA, empat variabel dapat diarahkan
ke GHCR:

```dotenv
SIPACUL_MIGRATOR_IMAGE=ghcr.io/<owner>/sipacul-migrator:sha-<full-git-sha>
SIPACUL_API_IMAGE=ghcr.io/<owner>/sipacul-api:sha-<full-git-sha>
SIPACUL_FRONTEND_IMAGE=ghcr.io/<owner>/sipacul-frontend:sha-<full-git-sha>
SIPACUL_EDGE_IMAGE=ghcr.io/<owner>/sipacul-edge:sha-<full-git-sha>
```

Keempat image harus berasal dari SHA yang sama.

## Batas scope

Sprint 20D2G1 tidak:

- membuat GitHub Release;
- membuat semantic/version release tag;
- mengubah DNS atau domain;
- membuka public bind;
- menyediakan atau merotasi sertifikat produksi;
- mengubah firewall;
- mengaktifkan HSTS;
- membuat deployment host;
- menjalankan database migration produksi;
- menyediakan rollback host.

Hal-hal tersebut tetap menjadi tahap deployment berikutnya.

## Acceptance

Sprint 20D2G1 dianggap siap di-commit ketika:

1. workflow publish hanya `workflow_dispatch`;
2. permission workflow tidak melebihi `contents: read`, `actions: read`, dan
   `packages: write`;
3. empat image memakai tag `sha-${GITHUB_SHA}`;
4. tidak ada tag `latest`;
5. workflow memverifikasi Release gate sukses untuk SHA yang sama;
6. workflow menolak overwrite tag SHA yang sudah ada;
7. Dockerfile, target, dan frontend build argument sesuai production Compose;
8. `production.env.example` tetap aman dengan default image lokal;
9. `git diff --check` bersih.

Publish GHCR pertama baru dilakukan setelah perubahan ini di-commit, di-push,
dan Release gate untuk commit tersebut sukses.
