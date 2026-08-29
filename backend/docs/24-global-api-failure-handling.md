# Sprint 20D2C - Global API Failure Handling

Sprint 20D2C menambahkan batas kegagalan global untuk exception yang tidak
ditangani oleh endpoint atau middleware API. Tujuannya adalah menjaga kontrak
HTTP produksi tanpa membocorkan pesan exception, stack trace, connection
string, atau detail internal lainnya kepada client.

## Kontrak respons

Exception tak terduga menghasilkan:

- status `500 Internal Server Error`;
- content type `application/problem+json`;
- title dan detail generik;
- kode stabil `Server.UnexpectedError`;
- `traceId` yang dapat dicocokkan dengan log server;
- header `Cache-Control: no-store` dan `Pragma: no-cache`.

`BadHttpRequestException` dari model binding selalu diperlakukan sebagai client
error. Status `4xx` yang valid dipertahankan; nilai status di luar rentang
client dinormalisasi menjadi `400 Bad Request`. Respons memakai pesan generik
dan kode stabil `Request.Invalid`. Nilai parameter yang gagal di-bind tidak
dikirim kembali kepada client dan tidak ditambahkan ke template log aplikasi.

Handler hanya mencatat method, path tanpa query string, trace identifier, dan
exception pada log server. Request body, cookie, token, query string, serta
nilai konfigurasi tidak ditambahkan secara eksplisit ke template log.
Client error framework dicatat sebagai warning tanpa object exception agar
nilai input mentah tidak ikut masuk ke log handler.

## Posisi pipeline

`UseExceptionHandler` ditempatkan sebelum HTTPS redirection, routing,
authentication, rate limiting, authorization, antiforgery, endpoint, dan
middleware bisnis. Dengan demikian exception dari seluruh pipeline setelahnya
masuk ke kontrak kegagalan yang sama.

Respons kegagalan domain yang memang dikembalikan sebagai `IResult` tidak
diubah. Kode seperti validation, conflict, not found, dan persistence failure
tetap mengikuti kontrak endpoint masing-masing.

## Verifikasi

Test integrasi mengganti service bootstrap publik dengan stub yang melempar
exception berisi pesan rahasia. Test memastikan respons tetap `500` berbentuk
Problem Details, memiliki kode dan trace identifier, memakai `no-store`, serta
tidak memuat pesan atau nama tipe exception.

Test regresi client-error melempar `BadHttpRequestException` berisi nilai
rahasia untuk status `400`, `422`, dan fallback dari `500` ke `400`. Test
memastikan kode `Request.Invalid` serta respons tetap tersanitasi. Test endpoint
model binding yang sudah ada juga menjaga empat query histori musim dan satu
enum pembagian hasil tetap mengembalikan `400`.

## Batas checkpoint

Checkpoint ini tidak menambahkan telemetry eksternal, distributed tracing,
alerting, error tracker, dashboard, reverse proxy, TLS, registry, atau hosting.
Log tetap memakai provider bawaan aplikasi dan kebijakan rotasi container yang
sudah tersedia. Integrasi observability eksternal dipilih bersama penyedia
hosting agar tidak menambah layanan yang belum diperlukan MVP.
