# SiPacul Core → Design System Handoff

Dokumen ini dimiliki oleh proses **Core** (`rizkyerapee-bit/sipacul`). Fungsinya memberi tahu Claude/Design System perubahan aplikasi nyata yang relevan terhadap pekerjaan desain tanpa mengharuskan Design System menebak dari seluruh histori Core.

## Current State

| Field | Nilai |
|---|---|
| `CORE_SOURCE_HEAD_AT_HANDOFF` | `910390c602ac09cbe1a72b089536ce2888a9945e` |
| `PRODUCTION_RELEASE` | `910390c602ac09cbe1a72b089536ce2888a9945e` |
| `DESIGN_REPOSITORY_HEAD_OBSERVED` | `a88ae908b1c7f297d14bf6585ca97ae05b6b75cd` |
| `DESIGN_SOURCE_HEAD_AT_HANDOFF` | `090ee421c0caf2eaca93d127eac4df54c8647a31` |
| `DESIGN_AUDITED_BY_CORE` | `0e7e04723dc3fb7740a8cb6c46c4a4d8dfaf00a8` |
| `DESIGN_INTEGRATED_TO_CORE` | *none* |
| `CURRENT_TRACK` | `BRIDGE-001` |
| `NEXT_CORE_TRACK` | `UI-001 STEP 0 — Design System Delta Audit` |

`CORE_SOURCE_HEAD_AT_HANDOFF` adalah commit Core sebelum dokumen bridge ini ditambahkan. Nama field sengaja tidak menggunakan `CORE_HEAD` agar dokumen tidak langsung basi karena commit dokumentasi dirinya sendiri.

## Core Changes Since Last Design Review

Pada pembentukan BRIDGE-001 ini tidak ada delta source Core baru terhadap baseline production `910390c602ac09cbe1a72b089536ce2888a9945e`.

Dua perubahan frontend terakhir yang penting untuk desain dan tetap wajib dipertahankan saat porting adalah:

- `c65eb27c6c43b6d1965e851985fa8353b468c12e` — `fix(lands): protect editor form data`;
- `910390c602ac09cbe1a72b089536ce2888a9945e` — `fix(cultivation): protect form data`.

Keduanya menggunakan kontrak `frontend/src/lib/ui/form-data-loss.ts`. Design System sudah mencatat bahwa dirty-check/discard-confirmation prototype harus mengikuti behavior Core ini, bukan sebaliknya.

## Production Behavior That Design Must Preserve

Design/porting tidak boleh mengganti sumber kebenaran berikut:

- API client dan API contracts Core;
- authentication/session Core;
- organization membership dan organization switching Core;
- permission filtering dan authorization Core;
- CSRF/security behavior Core;
- validation Core;
- lifecycle/status rules Core;
- form-data-loss protection Core;
- routing production Core;
- database dan business rules Core.

## Current Frontend/Product Checkpoint

- UI-000 Design System Integration Assessment: **SELESAI — PASS WITH GUARDRAILS**.
- BRIDGE-001: sedang dibentuk sebagai koordinasi dua arah.
- UI-001 Compatibility Foundation: belum dimulai.
- C8G12 Rev1 Master Komoditas Frontend: **JANGAN DIJALANKAN**.
- Master Komoditas/Kategori backend/API Core sudah tersedia; frontend resmi akan dibuat ulang sebagai C8G12 Rev2 setelah foundation integrasi disepakati.
- Tidak ada migration/database change yang diperlukan hanya untuk basic Master Komoditas frontend berdasarkan audit sebelumnya.

## Design Delta Known by Core

Baseline desain yang sudah diaudit Core:

`0e7e04723dc3fb7740a8cb6c46c4a4d8dfaf00a8`

Design source pada handoff terbaru:

`090ee421c0caf2eaca93d127eac4df54c8647a31`

Design System mendokumentasikan delta 8 commit di antara kedua SHA tersebut. Delta itu **belum otomatis approved untuk integration**. UI-001 STEP 0 harus membaca handoff Design dan melakukan Design System Delta Audit sebelum implementation.

Repository Design System kemudian memiliki commit dokumentasi bridge sampai:

`a88ae908b1c7f297d14bf6585ca97ae05b6b75cd`

Commit dokumentasi bridge tidak dengan sendirinya mengubah `DESIGN_AUDITED_BY_CORE` atau `DESIGN_INTEGRATED_TO_CORE`.

## New Core Capabilities Requiring Design Attention

**Saat ini: tidak ada capability Core baru sejak baseline 910390c yang belum tercermin dalam review terakhir.**

Jika Core menambahkan route/module/API/permission/function baru pada pekerjaan berikutnya, section ini harus diperbarui dan Design System harus memperlakukannya sebagai input Core Delta Review.

## Capability Proposals From Design

Menurut handoff Design pada `a88ae908...`, saat ini belum ada entri pada `New Functions / Product Capability Proposals`.

Jika kemudian Design System menambahkan fungsi baru, jangan hapus ide tersebut hanya karena belum ada di Core. Catat sebagai proposal. Proses Core akan mengklasifikasikan dukungan sebagai Full/Partial/None/Unknown dan menentukan apakah hanya perlu frontend integration atau Feature Expansion.

## Integration Guardrails for Claude

Saat membaca file ini pada awal sprint desain:

1. Anggap repository Core read-only.
2. Bandingkan `CORE_SOURCE_HEAD_AT_HANDOFF` dengan HEAD lokal Core sesuai `docs/01-working-agreement.md` Design System.
3. Jika berbeda, lakukan Core Delta Review sebelum melanjutkan sprint.
4. Jangan menebak business/API/security behavior dari prototype.
5. Jangan menganggap fungsi prototype sebagai production capability tanpa bukti Core.
6. Setelah sprint selesai, perbarui `sipacul-design-system/docs/integration/core-integration-handoff.md`.

## Do Not Infer

Dokumen ini tidak menyatakan bahwa Design System sudah diport ke Core. Sampai `DESIGN_INTEGRATED_TO_CORE` diubah setelah implementasi dan validation/UAT, production tetap menggunakan frontend Core yang ada.
