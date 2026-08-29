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

`compose.production.yml` mengisi satu proxy eksak dari
`SIPACUL_TRUSTED_PROXY_IP`. Nilai default dan template adalah `127.0.0.1`.
Frontend container memakai alamat Docker dinamis sehingga sengaja belum
dipercaya pada checkpoint yang tidak memilih hosting ini.

Konsekuensinya, request API melalui frontend sementara dipartisi rate limiter
menurut peer frontend internal, bukan nilai `X-Forwarded-For`. Ini konservatif:
client tidak dapat memperoleh bucket login baru dengan mengganti header. Setelah
topologi hosting final memiliki alamat peer yang stabil, isi
`SIPACUL_TRUSTED_PROXY_IP` dengan representasi IP eksak yang benar-benar terlihat
oleh `HttpContext.Connection.RemoteIpAddress`. Jangan memasukkan IP client,
alamat bind, CIDR, atau nama host.

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

Container gate juga mengirim sepuluh percobaan login dari frontend dengan
spoofed IP A, lalu percobaan ke-11 dengan spoofed IP B. Request terakhir wajib
tetap menerima `429`. Jika header bebas dipercaya, IP B memperoleh partisi baru
dan gate gagal.

Setiap probe memakai malformed JSON agar sepuluh request pertama berhenti
sebagai `400` pada body binding setelah rate limiter memperoleh permit, tetapi
sebelum antiforgery memeriksa scheme HTTPS. Dengan demikian status probe tidak
bergantung pada token atau cookie antiforgery.

## Batas checkpoint

Sprint 20D2E tidak memilih reverse proxy atau hosting, tidak mengaktifkan HSTS,
tidak menetapkan domain/TLS, tidak mengubah limiter terdistribusi, dan tidak
mengubah database atau migration. Image, Git tag, GitHub Release, registry, dan
deployment publik juga tetap di luar checkpoint ini.
