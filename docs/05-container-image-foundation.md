# Sprint 20B1 — Container Image Foundation

Checkpoint ini menyediakan image produksi multi-stage untuk API ASP.NET Core
dan frontend Next.js. Kedua image dibangun dari root repository agar dependency
project dan lockfile tetap menjadi sumber yang deterministik.

## API

- Build menggunakan .NET SDK 10 dan runtime menggunakan ASP.NET Core 10.
- Hasil publish tidak membawa SDK atau source ke image runtime.
- Container berjalan sebagai user non-root bawaan image .NET.
- Port internal adalah `8080`.
- Docker health check memakai `GET /health/live` sehingga liveness tidak
  bergantung pada PostgreSQL.

Connection string, bootstrap token, dan konfigurasi produksi tidak ditulis ke
image. Semuanya harus diberikan saat container dijalankan.

## Frontend

- Dependency dipasang memakai `npm ci` dan `package-lock.json`.
- Runtime hanya membawa output Next.js `standalone` dan aset statis.
- Container berjalan sebagai user `node`, bukan root.
- Port internal adalah `3000`.
- Docker health check memeriksa halaman `/login`.
- `SIPACUL_API_ORIGIN` merupakan build argument karena rewrite Next.js disusun
  saat production build.

## Batas checkpoint

Image foundation belum mengatur PostgreSQL, migration runner, secret,
persistensi volume, reverse proxy, TLS, backup, ataupun domain publik. Semua
hal tersebut menjadi scope Sprint 20B2 setelah kedua image lulus build dan
smoke test secara terisolasi.
