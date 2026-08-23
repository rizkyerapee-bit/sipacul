# Sprint 20A — Operational Health Foundation

Sprint 20A menambahkan kontrak health check yang dapat digunakan oleh Docker,
reverse proxy, dan platform orkestrasi tanpa membuka data bisnis atau memerlukan
autentikasi pengguna.

## Endpoint

- `GET /health/live` memastikan proses API masih hidup. Probe ini tidak
  mengakses PostgreSQL.
- `GET /health/ready` memastikan API siap menerima traffic dan dapat terhubung
  ke PostgreSQL.

Kedua endpoint mengembalikan JSON ringkas, memakai header `Cache-Control:
no-store`, tidak masuk OpenAPI, dan bersifat anonymous agar probe infrastruktur
tidak bergantung pada session pengguna.

## Status HTTP

- `200 OK` ketika probe sehat.
- `503 Service Unavailable` ketika readiness gagal.

Respons readiness hanya menampilkan nama check dan statusnya. Exception,
connection string, credential, serta detail database tidak dikirim kepada
client.

## Batas checkpoint

Checkpoint ini belum menambahkan Dockerfile aplikasi, reverse proxy, backup,
CI/CD, atau deployment produksi. Artefak tersebut akan memakai endpoint ini
pada checkpoint berikutnya.
