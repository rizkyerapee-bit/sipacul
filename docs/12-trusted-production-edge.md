# Sprint 20D2F — Trusted Production Edge

Checkpoint ini menutup jalur HTTPS internal stack produksi tanpa memilih
penyedia hosting. Nginx unprivileged menjadi satu-satunya service yang
memublikasikan port host, terminasi TLS, dan boundary terakhir sebelum request
masuk ke frontend atau API.

## Topologi

- `edge:8443` menerima HTTPS dan menjadi satu-satunya port host;
- `/api/v1` dan `/api/v1/*` diteruskan langsung ke `api:8080`;
- route lain diteruskan ke `frontend:3000`;
- PostgreSQL, migrator, API, dan frontend tidak memiliki published port;
- edge memiliki `SIPACUL_EDGE_IP` eksak pada network application;
- API memercayai hanya IP edge tersebut sebagai forwarded-header peer.

Frontend tetap memiliki rewrite API untuk workflow pengembangan, tetapi rewrite
itu tidak berada pada jalur publik produksi karena edge menangkap seluruh prefix
`/api/v1` lebih dahulu.

## Sanitasi request

Edge selalu mengganti:

- `X-Forwarded-For` dengan alamat peer TLS yang dilihat Nginx;
- `X-Forwarded-Proto` dengan scheme koneksi edge;
- `Host` dengan host yang telah dinormalisasi Nginx.

Edge menghapus `Forwarded`, `X-Forwarded-Host`, `X-Forwarded-Port`, dan
`X-Real-IP` dari request upstream. Dengan demikian header yang dikirim client
tidak dapat langsung memilih IP rate-limit atau membuat request HTTP terlihat
sebagai HTTPS bagi API.

## Sertifikat dan bind

`SIPACUL_TLS_CERTIFICATE_PATH` dan `SIPACUL_TLS_PRIVATE_KEY_PATH` wajib menunjuk
file PEM deployment. File dibaca read-only dan tidak boleh disimpan di Git.
Private key harus dapat dibaca UID `101` di dalam container edge.

Default bind tetap `127.0.0.1:8443`. Mengubahnya menjadi alamat publik baru
aman setelah DNS, sertifikat domain, firewall, rotasi sertifikat, dan backup
konfigurasi hosting tersedia. Container gate membuat sertifikat self-signed
sementara yang hanya berlaku selama satu run dan selalu dihapus.

## Verifikasi

Container gate membuktikan:

1. empat image produksi dapat dibangun;
2. hanya edge memublikasikan satu port HTTPS loopback;
3. `/login` dilayani frontend dan `/api/v1/*` dilayani API;
4. token dan cookie antiforgery dapat dibuat melalui scheme HTTPS;
5. login dengan token valid mencapai handler dan mengembalikan `401`, bukan
   kegagalan antiforgery atau exception `500`;
6. spoofed forwarded header tidak membuat partisi rate-limit baru;
7. IP edge identik dengan proxy eksak pada environment API;
8. sertifikat, container, volume, network, dan empat image sementara dibersihkan.

## Batas checkpoint

Sprint 20D2F tidak menyediakan domain, sertifikat produksi, DNS, public bind,
HSTS, WAF, CDN, registry, deployment pipeline, atau limiter terdistribusi.
Checkpoint juga tidak mengubah dependency aplikasi, frontend source, database,
migration, tag, maupun GitHub Release.
