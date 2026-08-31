# SiPacul Core → Design System Handoff

Dokumen ini dimiliki oleh proses **Core** (`rizkyerapee-bit/sipacul`) dan dibaca Design System secara read-only.

## Current State

| Field | Nilai |
|---|---|
| `CORE_APPLICATION_BASELINE` | `910390c602ac09cbe1a72b089536ce2888a9945e` |
| `CORE_REPOSITORY_HEAD_OBSERVED_BEFORE_AUDIT_RECORD` | `2590bed3f87678b704766a3c093972535dc4c49e` |
| `PRODUCTION_RELEASE` | `910390c602ac09cbe1a72b089536ce2888a9945e` |
| `DESIGN_REPOSITORY_HEAD_OBSERVED` | `a88ae908b1c7f297d14bf6585ca97ae05b6b75cd` |
| `DESIGN_SOURCE_HEAD_AT_HANDOFF` | `090ee421c0caf2eaca93d127eac4df54c8647a31` |
| `DESIGN_AUDITED_BY_CORE` | `090ee421c0caf2eaca93d127eac4df54c8647a31` |
| `DESIGN_INTEGRATED_TO_CORE` | *none* |
| `CURRENT_TRACK` | `UI-001 STEP 0 — Design System Delta Audit COMPLETE` |
| `NEXT_CORE_TRACK` | `UI-001 STEP 1 — Compatibility Foundation Context & Implementation Plan` |

## UI-001 STEP 0 Result

Core sudah mengaudit delta desain `0e7e047... → 090ee421...` (8 commit).

Verdict: **PASS WITH ADAPTATION GUARDRAILS**.

Detail audit permanen: `docs/integration/ui-001-design-delta-audit.md`.

### Approved as visual baseline

- Inter untuk body/UI + Georgia untuk heading/angka besar sesuai source Core;
- form control sizing yang konsisten;
- dropdown option readability sebagai UX intent;
- confirm-dialog actions centered sebagai visual deviation yang disetujui;
- sub-view/navigation Aktivitas tetap instan, tetapi production tetap memakai route Core.

### Audited but not ready to integrate

- Panen Sprint 9 baru Langkah 1; jangan dianggap final atau diport sekarang.

### Adapt, do not copy verbatim

- custom-select HTML/vanilla JS prototype;
- page-scoped/sibling-dependent CSS selectors;
- Google Fonts `<link>` dari prototype;
- mock data/notice simulations;
- local-state sub-view implementation.

Core tetap source of truth untuk API, auth/session, permissions, CSRF/security, validation, routing, lifecycle/status, business rules, database, dan form-data-loss protection.

## Capability Proposals

Menurut Design handoff pada `a88ae908...`, belum ada capability proposal baru. Jika Design System menambah fungsi baru pada sprint berikutnya, catat di `New Functions / Product Capability Proposals`; jangan membuang idenya hanya karena belum tersedia di Core.

## Product Checkpoint

- UI-000: complete.
- BRIDGE-001: complete.
- UI-001 Step 0: complete.
- UI-001 Compatibility Foundation implementation: belum dimulai.
- C8G12 Rev1: **DO NOT RUN**.
- Master Komoditas frontend akan dibuat sebagai C8G12 Rev2 setelah foundation compatibility disepakati.

## Guardrails for Design

1. Core tetap read-only dari workflow Design System.
2. Jika Core application baseline berubah dari `910390c...`, lakukan Core Delta Review sebelum sprint desain berikutnya.
3. Jangan menganggap repository documentation commit Core sebagai perubahan behavior aplikasi.
4. Product capability baru boleh diprototipekan tetapi harus diberi status proposal sampai diaudit Core.
5. Update `docs/integration/core-integration-handoff.md` setelah sprint selesai.
