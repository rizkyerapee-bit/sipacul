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
5. Nginx edge baru dimulai setelah API dan frontend sehat.

PostgreSQL, API, dan frontend tidak memublikasikan port host. Satu-satunya port
host adalah HTTPS Nginx edge. Edge meneruskan `/api/v1/*` langsung ke API dan
route lain ke frontend melalui network internal Compose.

## Konfigurasi

Salin template dan ganti seluruh placeholder:

```powershell
Copy-Item production.env.example .env.production
notepad.exe .env.production
```

Gunakan password PostgreSQL dan bootstrap token acak yang panjang. Isi path
absolut sertifikat serta private key PEM, lalu pilih subnet application yang
tidak bertabrakan pada host. Jangan commit `.env.production`, sertifikat, atau
private key; pola `.env.*` repository sudah mengabaikan file environment.

Default hanya mengikat edge pada `127.0.0.1:8443`. Konfigurasi ini membuktikan
jalur HTTPS tetapi belum membuat deployment publik. Pertahankan loopback sampai
sertifikat sesuai domain, DNS, firewall, dan kebijakan pembaruan sertifikat
tersedia. Jangan memakai sertifikat sementara container gate pada deployment.

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
verifikasi jalur edge dengan sertifikat deployment:

```text
https://127.0.0.1:8443/login
https://127.0.0.1:8443/api/v1/bootstrap/status
```

## Persistensi dan operasi aman

- Volume `sipacul_postgres_data` menyimpan database.
- Volume `sipacul_data_protection_keys` mempertahankan kunci perlindungan data
  API saat container diganti.
- Logging Docker dibatasi tiga file berukuran maksimal 10 MiB per service.
- `docker compose down` menghentikan container tetapi mempertahankan data.
- `docker compose down --volumes` menghapus database dan kunci; gunakan hanya
  untuk reset yang memang disengaja dan sudah memiliki backup.

Backup terjadwal dan restore drill sudah memiliki checkpoint terpisah. Registry
image, domain publik, otomasi sertifikat, dan pipeline deployment tetap menjadi
checkpoint lanjutan. Sprint 20B2 tetap merupakan dasar lifecycle stack;
perubahan edge berikutnya memperketat jalur publik tanpa mengubah database.
