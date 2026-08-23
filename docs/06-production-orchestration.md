# Sprint 20B2 — Production Orchestration

Checkpoint ini mengorkestrasi image Sprint 20B1 menjadi satu stack produksi
yang terisolasi. File `compose.yml` untuk PostgreSQL pengembangan tetap tidak
berubah; stack produksi memakai `compose.production.yml` dan project Compose
yang berbeda.

## Urutan startup

1. PostgreSQL dimulai dan harus lulus `pg_isready`.
2. Service `migrator` menjalankan seluruh EF Core migration tepat satu kali.
3. API baru dimulai setelah migrator keluar dengan kode `0`.
4. Frontend baru dimulai setelah `/health/ready` API sehat.

PostgreSQL dan API tidak memublikasikan port host. Satu-satunya port host adalah
frontend, yang meneruskan `/api/v1/*` ke API melalui network internal Compose.

## Konfigurasi

Salin template dan ganti seluruh placeholder:

```powershell
Copy-Item production.env.example .env.production
notepad.exe .env.production
```

Gunakan password PostgreSQL dan bootstrap token acak yang panjang. Jangan
commit `.env.production`; pola `.env.*` repository sudah mengabaikannya.

Default hanya mengikat frontend pada `127.0.0.1:8080`, cocok untuk reverse
proxy yang berjalan pada host yang sama. TLS, nama domain, sertifikat, dan
hardening reverse proxy tetap berada di luar checkpoint ini. Jangan membuka
port langsung ke internet sebelum lapisan tersebut tersedia.

## Menjalankan stack

Validasi konfigurasi efektif terlebih dahulu:

```powershell
docker.exe compose `
  --env-file .env.production `
  --file compose.production.yml `
  config --quiet
```

Bangun dan mulai:

```powershell
docker.exe compose `
  --env-file .env.production `
  --file compose.production.yml `
  build

docker.exe compose `
  --env-file .env.production `
  --file compose.production.yml `
  up --detach
```

Periksa status dan log migrator:

```powershell
docker.exe compose --env-file .env.production --file compose.production.yml ps
docker.exe compose --env-file .env.production --file compose.production.yml logs migrator
```

Liveness dan readiness API tetap tersedia di dalam network Compose. Dari host,
verifikasi jalur publik melalui frontend:

```text
http://127.0.0.1:8080/login
http://127.0.0.1:8080/api/v1/bootstrap/status
```

## Persistensi dan operasi aman

- Volume `sipacul_postgres_data` menyimpan database.
- Volume `sipacul_data_protection_keys` mempertahankan kunci perlindungan data
  API saat container diganti.
- Logging Docker dibatasi tiga file berukuran maksimal 10 MiB per service.
- `docker compose down` menghentikan container tetapi mempertahankan data.
- `docker compose down --volumes` menghapus database dan kunci; gunakan hanya
  untuk reset yang memang disengaja dan sudah memiliki backup.

Backup terjadwal, restore drill, registry image, TLS/reverse proxy, dan pipeline
deployment menjadi checkpoint lanjutan. Sprint 20B2 hanya membuktikan lifecycle
stack produksi, migration gate, isolasi network, serta persistensi dasar.
