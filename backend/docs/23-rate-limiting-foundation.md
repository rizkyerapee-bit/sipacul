# Sprint 20D2B — API Rate Limiting Foundation

Checkpoint ini menambahkan pembatasan permintaan bawaan ASP.NET Core pada API
SiPacul sebelum Release Candidate dipublikasikan. Pembatasan berjalan di dalam
setiap instance API dan tidak menambah dependency, penyimpanan, atau migration.

## Kebijakan

| Cakupan | Batas | Partisi |
| --- | ---: | --- |
| Seluruh request API non-health | 240 per menit | user atau alamat IP |
| `POST /api/v1/auth/login` | 10 per menit | alamat IP |
| `POST /api/v1/bootstrap/owner` | 5 per menit | alamat IP |

Queue selalu `0`. Request di atas batas ditolak segera dengan status `429`,
payload `application/problem+json`, kode `RateLimit.Exceeded`, header
`Retry-After` bila tersedia, dan cache control `no-store`.

Endpoint `/health/live` dan `/health/ready` tidak dibatasi agar Docker,
orchestrator, dan monitoring tetap dapat mengevaluasi keadaan instance ketika
traffic API sedang dibatasi.

## Keamanan partisi

Request anonim dipartisi menurut `RemoteIpAddress`. Request terautentikasi pada
batas global dipartisi menurut identifier user stabil, sedangkan login dan
bootstrap selalu dipartisi menurut alamat IP untuk menahan percobaan berulang
sebelum identitas dapat dipercaya.

Stack produksi telah mengaktifkan forwarded headers. Reverse proxy hosting
wajib meneruskan alamat client hanya dari proxy yang dipercaya dan tidak boleh
membiarkan client langsung mencapai API. PostgreSQL dan API tetap tidak
dipublikasikan ke host oleh `compose.production.yml`.

## Verifikasi

Test API memeriksa:

- batas global dan partisi request;
- pengecualian health probe;
- metadata kebijakan login dan bootstrap;
- request ke-11 login ditolak sebagai problem `429`;
- health probe tetap sehat setelah partisi login mencapai batas.

## Batas checkpoint

Limiter ini bersifat in-memory per instance. Ketika deployment memakai lebih
dari satu replica API, reverse proxy atau gateway harus menambahkan batas
terdistribusi agar kapasitas gabungan tidak bertambah tanpa kendali. Angka
produksi perlu ditinjau dari telemetry nyata; perubahan batas tetap harus
melalui source, test, dan Release gate.

Sprint 20D2B tidak menerbitkan image, membuat Git tag/Release, memilih hosting,
mengubah database, atau memproses data bisnis. Registry, manifest RC, domain,
TLS, serta deployment publik tetap menunggu keputusan hosting.
