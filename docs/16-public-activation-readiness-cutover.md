# Sprint 20D2G3B - Public Endpoint Readiness and Cutover Guard

Sprint 20D2G3B menambahkan standar provider-neutral untuk membuktikan bahwa
endpoint publik SiPacul benar-benar siap sebelum public activation dianggap
selesai.

Tahap ini tetap tidak memilih hosting provider, DNS provider, atau ACME client.

## Artefak

```text
operations/Test-SiPaculPublicEndpoint-PS51.ps1
```

Probe kompatibel dengan Windows PowerShell 5.1 dan hanya membaca endpoint
publik.

## Acceptance endpoint publik

Cutover baru dianggap lulus bila dari mesin di luar host produksi:

1. hostname resolve melalui DNS;
2. expected public IP, bila diberikan, terdapat pada DNS result;
3. TCP HTTPS port dapat dijangkau;
4. TLS handshake lulus untuk hostname;
5. certificate belum expired dan tersisa minimal 14 hari;
6. `/login` memberi response HTTP non-error;
7. `/login` memiliki security headers SiPacul;
8. `/api/v1/bootstrap/status` dapat dijangkau;
9. API response memiliki security headers SiPacul;
10. HSTS hanya diwajibkan pada phase kedua setelah HTTPS stabil.

## Phase 1 - public HTTPS tanpa HSTS

Setelah DNS, certificate, firewall, dan public bind disiapkan, jalankan dari
jaringan eksternal:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPublicEndpoint-PS51.ps1 `
  -Hostname <production-fqdn> `
  -ExpectedIpAddress <public-ip>
```

Pada phase ini `SIPACUL_HSTS_ENABLED=false` tetap benar.

Probe menerima HSTS absent ketika `-RequireHsts` tidak diberikan.

## Phase 2 - HSTS

HSTS baru boleh diaktifkan setelah phase 1 stabil.

Setelah environment diubah menjadi:

```dotenv
SIPACUL_HSTS_ENABLED=true
```

dan edge direcreate melalui procedure deployment terkontrol, jalankan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPublicEndpoint-PS51.ps1 `
  -Hostname <production-fqdn> `
  -ExpectedIpAddress <public-ip> `
  -RequireHsts
```

`-RequireHsts` mewajibkan `Strict-Transport-Security` dengan `max-age`.

## Security headers

Login dan API harus tetap menyediakan:

- `Content-Security-Policy`;
- `Referrer-Policy`;
- `X-Content-Type-Options`;
- `X-Frame-Options`.

HSTS tetap edge concern dan tidak dipindahkan ke backend/frontend.

## Certificate safety

TLS handshake menggunakan hostname produksi. Certificate chain atau hostname
yang tidak dipercaya membuat probe gagal.

Certificate dengan sisa masa berlaku kurang dari 14 hari juga dianggap gagal.
Ini memberi ruang untuk memperbaiki renewal sebelum outage.

## DNS

`-ExpectedIpAddress` dapat diulang untuk beberapa address.

Contoh:

```powershell
-ExpectedIpAddress 203.0.113.10,2001:db8::10
```

Jika expected IP tidak ditemukan pada hasil DNS, probe gagal.

## Firewall

Probe tidak membuat firewall rule. TCP failure menjadi bukti bahwa routing,
security group, provider firewall, host firewall, NAT, atau service exposure
masih perlu diperbaiki.

## Provider-neutral boundary

Tooling ini tidak:

- membuat DNS record;
- meminta certificate;
- melakukan ACME challenge;
- membuka port firewall;
- login ke hosting provider;
- mengubah `.env.production`;
- menjalankan Docker Compose;
- mengaktifkan public bind;
- mengaktifkan HSTS;
- mengubah database.

## Cutover order

Urutan operasional tetap:

1. tetapkan FQDN;
2. siapkan host dan public IP;
3. siapkan certificate automation;
4. kunci firewall;
5. buat DNS;
6. verifikasi DNS propagation;
7. set activation enabled, HSTS false, port 443;
8. jalankan public activation config test;
9. deploy/recreate edge terkontrol;
10. jalankan public endpoint probe dari luar host;
11. observasi phase 1;
12. aktifkan HSTS;
13. jalankan probe dengan `-RequireHsts`.

## Acceptance Sprint 20D2G3B

Sprint dianggap siap ketika:

1. external endpoint probe memiliki syntax PowerShell valid;
2. probe gagal pada DNS/TCP/TLS/certificate error;
3. probe memeriksa login dan API;
4. security headers diwajibkan;
5. HSTS tetap phase kedua;
6. probe read-only;
7. tidak ada provider credential atau mutation saat apply.
