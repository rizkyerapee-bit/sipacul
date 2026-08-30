# Sprint 20D2G3A - Public Activation Gates

Sprint 20D2G3A menyiapkan edge SiPacul untuk aktivasi publik tanpa membuka
aplikasi ke internet hanya karena perubahan ini masuk repository.

Tahap ini adalah foundation. DNS provider, hosting provider, ACME challenge,
firewall host, dan domain produksi belum dipilih oleh repository.

## Safe defaults

`production.env.example` tetap aman:

```dotenv
SIPACUL_PUBLIC_ACTIVATION=disabled
SIPACUL_PUBLIC_HOSTNAME=_
SIPACUL_HSTS_ENABLED=false
SIPACUL_HSTS_MAX_AGE=86400
SIPACUL_BIND_ADDRESS=127.0.0.1
SIPACUL_HTTPS_PORT=8443
```

Dengan state tersebut edge hanya boleh memakai loopback, hostname wildcard
internal `_`, dan HSTS tidak boleh aktif.

## Domain-aware edge

Image `nginxinc/nginx-unprivileged` memiliki entrypoint envsubst. Dockerfile
SiPacul menempatkan `edge/default.conf` sebagai template
`/etc/nginx/templates/default.conf.template`.

Compose membatasi envsubst ke variable `SIPACUL_*`. Dengan demikian variable
Nginx seperti `$remote_addr` dan `$scheme` tetap menjadi variable Nginx.

`server_name` berasal dari:

```text
SIPACUL_PUBLIC_HOSTNAME
```

Domain tidak di-hardcode ke image.

## Runtime activation guard

Script berikut dijalankan oleh Nginx entrypoint sebelum Nginx start:

```text
edge/25-sipacul-public-activation.sh
```

Jika activation disabled, script mewajibkan:

- hostname `_`;
- bind `127.0.0.1`;
- HSTS `false`.

Jika activation enabled, script mewajibkan:

- hostname berbentuk DNS hostname;
- hostname bukan `_` atau `localhost`;
- bind bukan `127.0.0.1`;
- HTTPS port host `443`;
- HSTS hanya `true` atau `false`;
- HSTS max-age berada antara 300 dan 63072000 detik.

Konfigurasi yang tidak konsisten membuat container edge gagal sebelum Nginx
melayani request.

## HSTS gate

HSTS tidak berada di backend atau frontend. Edge membuat snippet:

```text
/etc/nginx/snippets/sipacul-hsts.conf
```

Saat HSTS false, snippet kosong.

Saat HSTS true:

```nginx
add_header Strict-Transport-Security "max-age=<seconds>" always;
```

`includeSubDomains` dan `preload` sengaja tidak diaktifkan pada foundation ini.
Keduanya mempunyai blast radius lebih besar dan harus menjadi keputusan
operasional terpisah.

HSTS hanya boleh diubah menjadi true setelah HTTPS publik untuk hostname
produksi sudah diverifikasi dari luar host.

## Static configuration test

Gunakan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPublicActivationConfig-PS51.ps1
```

Untuk memastikan environment sudah dipersiapkan untuk public cutover:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPublicActivationConfig-PS51.ps1 `
  -RequirePublicActivation
```

Setelah HTTPS eksternal terbukti sehat dan HSTS akan diaktifkan:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass `
  -File .\operations\Test-SiPaculPublicActivationConfig-PS51.ps1 `
  -RequirePublicActivation `
  -RequireHsts
```

Script test hanya membaca konfigurasi.

## Aktivasi nyata

Aktivasi host belum dijalankan pada sprint foundation ini.

Urutan cutover yang disyaratkan:

1. pilih FQDN produksi;
2. pilih host/provider dan tentukan public IPv4/IPv6;
3. pilih ACME/certificate automation sesuai provider dan challenge type;
4. simpan certificate dan private key di host, bukan Git;
5. buat DNS A/AAAA atau record yang sesuai;
6. verifikasi DNS dari resolver eksternal;
7. kunci firewall sehingga hanya port yang diperlukan yang terbuka;
8. ubah environment ke activation enabled, hostname produksi, bind publik,
   HTTPS port 443, tetapi HSTS tetap false;
9. jalankan config test dengan `-RequirePublicActivation`;
10. deploy/recreate edge melalui deployment procedure yang terkontrol;
11. verifikasi HTTPS, certificate chain, hostname, login, API, dan security
    headers dari jaringan eksternal;
12. baru aktifkan HSTS dan validasi dengan `-RequireHsts`.

## DNS

Repository tidak memilih DNS provider. Credential DNS API tidak boleh disimpan
di Git atau deployment state.

Automation DNS baru dapat dibuat setelah provider dipilih.

## Certificate automation

Repository belum memilih HTTP-01, TLS-ALPN-01, atau DNS-01. Pemilihan challenge
bergantung pada host, DNS provider, dan kebijakan port.

Certificate/private key tetap host secret dan menggunakan contract existing:

```dotenv
SIPACUL_TLS_CERTIFICATE_PATH=<absolute PEM path>
SIPACUL_TLS_PRIVATE_KEY_PATH=<absolute PEM path>
```

## Firewall

Tidak ada rule Windows Firewall, iptables, nftables, ufw, cloud security group,
atau provider firewall yang dibuat pada foundation ini.

Implementasi firewall harus mengikuti OS dan hosting target. Public activation
tidak dianggap selesai hanya karena Compose bind berubah.

## Scope boundary

Sprint 20D2G3A tidak:

- mengubah DNS;
- meminta certificate;
- menyimpan ACME account credential;
- menambah firewall rule;
- mengubah `.env.production`;
- membuka public bind;
- mengaktifkan HSTS;
- menjalankan Compose;
- melakukan deployment;
- mengubah database;
- memublikasikan image baru.

Semua mutation tersebut memerlukan activation step eksplisit setelah host dan
domain dipilih.

## Acceptance

Foundation dianggap siap ketika:

1. default production contract tetap loopback dan activation disabled;
2. hostname Nginx berasal dari environment template;
3. domain tidak di-hardcode ke image;
4. inconsistent activation state membuat edge gagal start;
5. HSTS default false dan dikendalikan pada edge;
6. HSTS tidak memakai includeSubDomains atau preload;
7. test PowerShell dapat memvalidasi disabled/public/HSTS state tanpa mutation;
8. Compose config tetap valid;
9. container release gate berikutnya tetap menjadi bukti canonical bahwa edge,
   trust boundary, dan stack berjalan;
10. tidak ada DNS/firewall/certificate/public mutation saat apply.
