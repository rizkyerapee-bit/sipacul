# SiPacul Frontend

Frontend MVP SiPacul menggunakan Next.js App Router, TypeScript, dan proxy
same-origin ke ASP.NET Core API.

## Menjalankan secara lokal

1. Jalankan backend pada origin yang ditulis di `next.config.ts`.
2. Dari folder `frontend`, jalankan `npm.cmd run dev`.
3. Buka `http://localhost:3000`.

Jika origin backend berbeda, salin `.env.example` menjadi `.env.local`, lalu
ubah `SIPACUL_API_ORIGIN`. File `.env.local` tidak boleh di-commit.

## Pemeriksaan kualitas

```powershell
npm.cmd run lint
npm.cmd run test:run
npm.cmd run build
```

Token bootstrap hanya dimasukkan pada halaman setup pertama dan dikirim langsung
ke backend. Token tidak disimpan di local storage, session storage, cookie, atau
source frontend.