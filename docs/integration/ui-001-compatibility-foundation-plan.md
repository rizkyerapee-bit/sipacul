# UI-001 Compatibility Foundation Plan

Tanggal: 2026-08-31

## Pinned Baseline

- Core repository baseline: `d46e1b09a9e4beb6aad239767906bf412dd1428e`
- Core application behavior baseline: `910390c602ac09cbe1a72b089536ce2888a9945e`
- Approved Design source: `090ee421c0caf2eaca93d127eac4df54c8647a31`
- Latest Design repository HEAD observed before implementation: `2640faf0d9c819ac875fbc66da3590066981a58d`
- Design App Shell blob at approved source and latest observed HEAD: `3b8bcc6cb49311f65325a3d9f32e5d8ceaeb0a22` (identik)

Perubahan Design setelah `090ee421c0caf2eaca93d127eac4df54c8647a31` hanya melanjutkan Sprint 9 Panen dan dokumentasi/bridge. App Shell tidak berubah, sehingga perubahan baru tersebut tidak diserap ke UI-001 foundation.

## Scope

UI-001 foundation adalah lapisan kompatibilitas additive:

- canonical design tokens ditempatkan di scope `DashboardShell`;
- existing `--app-*` tetap tersedia sebagai aliases agar halaman lama tidak harus dimigrasikan sekaligus;
- desktop shell memakai target 272px sidebar, 64px collapsed sidebar, 48px topbar, dan content max 1536px;
- desktop navigation dibuat lebih compact dan caption visual disembunyikan;
- caption tetap tersedia melalui `aria-label` dan `title`;
- real organization switcher, role, profile, theme, sidebar state, permissions, routing, session dan logout Core tetap dipertahankan;
- current mobile drawer contract tetap dipertahankan pada UI-001 untuk mengurangi regression surface.

## Explicitly Out of Scope

- Tailwind, Radix, shadcn, Zustand, atau dependency baru;
- search command palette prototype;
- fake notification;
- fake billing/account action;
- quick message / mock actions;
- custom-select HTML/vanilla JS prototype;
- perubahan API/auth/session/permission/CSRF;
- perubahan route/navigation semantics;
- perubahan validation/business/lifecycle;
- port halaman Dashboard/Lahan/Budidaya/Panen;
- Sprint 9 Panen Langkah 2;
- perubahan database/migration;
- perubahan breakpoint mobile dari kontrak Core 960px ke 860px;
- deployment.

## Validation Gate

Sebelum commit:

1. `npm run test:run` tepat sekali.
2. `npm run lint` tepat sekali.
3. `npm run build` tepat sekali.
4. `git diff --check`.
5. Scope diff harus hanya:
   - `frontend/src/components/dashboard-shell.module.css`
   - `frontend/src/components/dashboard-shell.tsx`
   - `frontend/src/lib/ui/dashboard-shell-design-contract.test.ts`
   - `docs/integration/ui-001-compatibility-foundation-plan.md`

Deployment/UAT dilakukan pada tahap terpisah setelah commit dan release gate.
