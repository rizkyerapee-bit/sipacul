# Sprint 20D2E — Forwarded Headers Trust Boundary

Checkpoint ini mengganti mode forwarded headers cloud yang menerima semua
forwarder dengan konfigurasi eksplisit ASP.NET Core. Tujuannya adalah memastikan
alamat IP dan scheme yang dipakai middleware API tidak dapat diganti hanya
dengan mengirim `X-Forwarded-*` dari sisi client.

## Kontrak kepercayaan

API hanya memproses:

- `X-Forwarded-For` untuk alamat client;
- `X-Forwarded-Proto` untuk scheme asal;
- satu hop paling dekat melalui `ForwardLimit = 1`;
- pasangan header dengan jumlah nilai simetris;
- request yang peer langsungnya cocok dengan satu alamat IP eksak pada
  `ForwardedHeaders:KnownProxies`.

`X-Forwarded-Host`, wildcard, subnet, dan proxy tak dikenal tidak dipercaya.
Nilai `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` dilarang dan membuat startup
gagal karena mode tersebut tidak membatasi alamat forwarder.

Middleware berjalan setelah global exception handler dan sebelum security
headers, HTTPS redirection, authentication, serta rate limiting. Dengan urutan
ini, consumer hanya melihat alamat dan scheme yang telah melewati boundary.

## Default stack produksi

Sprint 20D2F melengkapi boundary ini dengan Nginx edge ber-IP statis pada
network application. `compose.production.yml` mengisi alamat yang sama melalui
`SIPACUL_EDGE_IP` ke `ForwardedHeaders:KnownProxies` dan ke assignment network
edge. Frontend tetap memakai alamat Docker dinamis, tidak memublikasikan port,
dan tidak dipercaya API.

Edge terminasi TLS, merutekan `/api/v1/*` langsung ke API, dan mengganti
`X-Forwarded-For` serta `X-Forwarded-Proto` dari client. API menerima tepat satu
hop yang peer-nya adalah edge. Jangan memasukkan IP client, alamat bind, CIDR,
nama host, atau service lain ke daftar proxy tepercaya.

Sebelum mengubah nilai produksi, pastikan proxy tersebut:

1. menjadi satu-satunya peer yang dapat mencapai API;
2. menghapus atau mengganti header forwarded dari client;
3. meneruskan tepat satu nilai `X-Forwarded-For` dan `X-Forwarded-Proto`;
4. mempertahankan API tanpa port host yang dipublikasikan.

Topologi multi-hop memerlukan checkpoint terpisah; jangan menaikkan
`ForwardLimit` tanpa memodelkan dan menguji setiap hop tepercaya.

## Verifikasi

Test backend membuktikan default satu hop, penolakan mode cloud, validasi alamat
proxy, pengabaian header dari peer tak dikenal, dan penerimaan header simetris
dari proxy eksak.

Setelah satu login bertoken valid memakai satu permit, container gate mengirim
sembilan percobaan malformed login ke edge dengan spoofed IP A, lalu percobaan
ke-11 keseluruhan dengan spoofed IP B. Request terakhir wajib tetap menerima
`429`. Jika edge meneruskan header client mentah, IP B memperoleh partisi baru
dan gate gagal.

Kesembilan probe IP A memakai malformed JSON agar berhenti sebagai `400` pada
body binding setelah rate limiter memperoleh permit. Probe IP B mencapai batas
sebelum binding dan wajib `429`. Login pertama memakai token antiforgery valid
dan wajib mencapai handler sebagai `401`, sehingga scheme HTTPS juga dibuktikan
secara terpisah dari probe malformed.

## Batas checkpoint

Sprint 20D2E sendiri tidak memilih reverse proxy atau hosting. Sprint 20D2F
memilih Nginx sebagai edge stack tetapi tetap tidak menetapkan domain,
sertifikat produksi, public bind, HSTS, atau limiter terdistribusi. Database,
migration, GitHub Release, registry, dan deployment publik tetap di luar kedua
checkpoint.
