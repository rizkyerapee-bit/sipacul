# Sprint 20D2D — Public Response Security Headers

Checkpoint ini menambahkan baseline header keamanan pada seluruh permukaan
publik. Next.js menerapkannya pada halaman, sedangkan middleware API
menerapkannya sebelum respons melewati rewrite same-origin. Tujuannya adalah
menutup perlindungan HTTP yang tidak bergantung pada penyedia hosting, domain,
sertifikat, atau reverse proxy tertentu.

## Header yang diwajibkan

- `Content-Security-Policy` membatasi `base-uri` ke origin sendiri serta menolak
  `frame-ancestors` dan `object-src`.
- `X-Frame-Options: DENY` mempertahankan perlindungan clickjacking untuk user
  agent lama yang belum menerapkan `frame-ancestors`.
- `X-Content-Type-Options: nosniff` mencegah browser menebak tipe konten.
- `Referrer-Policy: strict-origin-when-cross-origin` membatasi detail URL yang
  dikirim ketika pengguna berpindah origin.

Baseline CSP sengaja tidak menetapkan `default-src`, `script-src`, atau
`style-src`. Next.js masih memerlukan script hydration dan aset build; CSP
ketat berbasis nonce harus dirancang sebagai checkpoint terpisah dan diuji pada
hosting final.

## Verifikasi

Test frontend mengunci satu rule `/(.*)` dan nilai keempat header. Test backend
mengunci middleware pada endpoint liveness tanpa memerlukan koneksi database.
Container release gate juga memeriksa header aktual pada `/login` dan endpoint
API `/api/v1/bootstrap/status`, sehingga konfigurasi build saja tidak cukup
untuk meluluskan checkpoint.

Header `Strict-Transport-Security` belum ditambahkan karena hanya aman setelah
TLS, domain, dan cakupan subdomain diputuskan. Kebijakan cache dan CORS juga
tetap tidak diubah agar checkpoint ini tidak mengubah kontrak data maupun
topologi same-origin yang sudah berjalan.

## Batas checkpoint

Sprint 20D2D tidak:

- memilih hosting, domain, sertifikat, reverse proxy, atau registry;
- mengaktifkan HSTS atau CSP nonce;
- mengubah autentikasi, cookie, CSRF, rate limiting, atau failure handling;
- mengubah API, database, migration, dependency, atau lockfile;
- membuat tag, GitHub Release, atau publikasi image.
