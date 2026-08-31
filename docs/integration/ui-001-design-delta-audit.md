# UI-001 STEP 0 — Design System Delta Audit

Tanggal: 2026-08-31

## Baseline

- Core application source: `910390c602ac09cbe1a72b089536ce2888a9945e`
- Core repository HEAD saat audit: `2590bed3f87678b704766a3c093972535dc4c49e` (documentation-only bridge commit)
- Design baseline sebelumnya: `0e7e04723dc3fb7740a8cb6c46c4a4d8dfaf00a8`
- Design source yang diaudit: `090ee421c0caf2eaca93d127eac4df54c8647a31`
- Design repository HEAD observed: `a88ae908b1c7f297d14bf6585ca97ae05b6b75cd` (documentation-only handoff refinement above source head)
- Delta: 8 commit dari `0e7e047...` ke `090ee421...`

## Verdict

**PASS WITH ADAPTATION GUARDRAILS.**

`DESIGN_AUDITED_BY_CORE` boleh maju ke `090ee421c0caf2eaca93d127eac4df54c8647a31` sebagai baseline desain yang sudah dipahami Core. Ini **tidak** berarti seluruh delta sudah approved untuk copy verbatim dan **tidak** berarti sudah terintegrasi ke production.

## Audit per perubahan

### 1. `24178c5` — Inter + Georgia retrofit

**Status: APPROVED VISUAL BASELINE.**

Core `globals.css` memang memakai Inter sebagai body/UI font. Core `crop-cycle-management.module.css` dan `harvest-management.module.css` secara eksplisit memakai Georgia untuk hero/title dan angka besar. Karena itu retrofit dari Geist ke Inter+Georgia merupakan koreksi fidelity yang didukung source Core.

Guardrail: jangan copy `<link>` Google Fonts prototype ke production secara otomatis. Strategi font loading production harus tetap kompatibel dengan CSP/build/deployment Core.

### 2. `8816f71` — Sprint 9 Panen Langkah 1

**Status: AUDITED, NOT READY FOR INTEGRATION.**

Konten Panen diekstrak dari `HarvestManagement` Core dan cocok secara konsep: cycle context, permission `harvest.read/write`, metric summary, unit behavior, dan lifecycle batch. Namun Design Sprint 9 baru Langkah 1 dari sekitar 6 langkah. Jangan port Panen sampai desain Panen lengkap dan audit delta berikutnya selesai.

### 3. `37edfae` — tinggi custom-select di modal

**Status: APPROVED AS VISUAL REQUIREMENT, ADAPT IMPLEMENTATION.**

Requirement agar control form sejajar masuk akal. Tetapi implementasi prototype berbasis `.field .select-trigger` tidak boleh disalin mekanis ke Core. Jika Core membangun custom-select React, primitive tersebut harus punya sizing yang konsisten tanpa bergantung pada struktur sibling/page prototype.

### 4. `3529873` — dropdown popup auto-width

**Status: APPROVED UX INTENT, REQUIRES ACCESSIBLE REACT IMPLEMENTATION.**

Intent agar opsi panjang terbaca tanpa horizontal scrolling diterima. Core saat ini banyak memakai native `<select>`, yang sudah punya semantics/keyboard behavior browser. Jangan mengganti native select secara global hanya untuk meniru prototype. Custom-select hanya boleh masuk lewat primitive React yang diuji keyboard/focus/listbox semantics, overflow, mobile viewport, dan reduced motion.

### 5. `6c9467e` — confirm-dialog actions ditengahkan

**Status: APPROVED VISUAL DEVIATION.**

Core Lahan saat ini menggunakan `justify-content:flex-end`; Design memilih center secara eksplisit. Ini murni presentation, tidak mengubah confirmation semantics. Boleh diadopsi saat modal/dialog primitive dimigrasikan, selama urutan tombol, dangerous action semantics, dirty-form behavior, keyboard, dan focus management Core dipertahankan.

### 6. `ea674df` — documentation artifact batch

**Status: NO IMPLEMENTATION IMPACT.**

Dokumentasi dari perbaikan yang sudah tercakup commit lain.

### 7. `f4f6eca` — `select-value` nowrap + field width

**Status: APPROVED UX INTENT, ADAPT IMPLEMENTATION.**

Requirement satu baris/ellipsis dan alokasi lebar field diterima. Jangan membawa ID/markup prototype apa adanya ke React production; implementasikan sebagai behavior primitive/layout CSS Modules yang reusable.

### 8. `090ee421` — sub-view Aktivitas tetap instan

**Status: APPROVED BEHAVIOR CONFIRMATION.**

Core sudah menggunakan route `/cultivation/activities`; prototype meniru perpindahan instan. Saat porting, pertahankan routing production Core. Jangan mengganti route menjadi local-state sub-view hanya karena prototype menggunakan satu file HTML.

## Cross-check terhadap Core

- Core body/UI menggunakan Inter.
- Core Crop Cycle dan Harvest memakai Georgia untuk heading/angka besar.
- Core crop-cycle filter saat ini masih native `<select>`; custom-select Design adalah presentational/interaction proposal untuk primitive production, bukan source behavior.
- Core Lahan confirm actions saat ini rata kanan; center adalah approved visual deviation, bukan behavior requirement.
- Core `HarvestManagement` sudah memiliki actual API calls, permissions, validation, lifecycle, route navigation, dan native select; semua itu tetap source of truth.

## Risk Classification

- A — tokens/typography: **LOW**, approved.
- B — button/card/badge/dialog alignment: **LOW–MEDIUM**, visual migration with regression tests.
- C — custom select/dropdown/modal interaction: **MEDIUM**, accessible React primitive required before broad use.
- D — routing/API/auth/permissions/lifecycle/business logic: **KEEP CORE**, Design is reference only.

## UI-001 Compatibility Foundation Scope

UI-001 implementation berikutnya sebaiknya fokus pada foundation yang aman dan reusable:

1. production design tokens alias layer;
2. typography foundation Inter + Georgia using Core-safe loading/fallback;
3. spacing/radius/shadow tokens;
4. Button/Badge/Card primitives or shared styling layer;
5. modal/dialog visual foundation while preserving Core behavior;
6. form control sizing foundation;
7. optional custom-select primitive only if implemented accessibly and tested in isolation;
8. shell visual migration only after foundation primitives pass tests.

Tidak termasuk UI-001 foundation:

- Panen page migration;
- Master Komoditas implementation;
- API/backend changes;
- auth/session changes;
- database/migration;
- replacing production routing;
- copying mock/vanilla JS prototype logic.

## Decision

`DESIGN_AUDITED_BY_CORE` maju ke `090ee421c0caf2eaca93d127eac4df54c8647a31`.

`DESIGN_INTEGRATED_TO_CORE` tetap `null`.

Design repository documentation commits `d39603d...` dan `a88ae908...` dibaca sebagai bridge metadata, bukan design source baseline.

Langkah Core berikutnya: **UI-001 STEP 1 — Compatibility Foundation Context & Implementation Plan** sebelum mutation application source.