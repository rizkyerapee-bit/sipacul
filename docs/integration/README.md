# SiPacul Core ↔ Design System Integration Contract

Dokumen ini adalah kontrak koordinasi antara dua repository yang berkembang paralel:

- **Core / Production Source of Truth**: `rizkyerapee-bit/sipacul`
- **Design / Visual Source of Truth**: `rizkyerapee-bit/sipacul-design-system`

Tujuannya adalah menjaga pertukaran informasi berdasarkan Git commit SHA tanpa menjadikan user sebagai kurir detail teknis dan tanpa membuat Design System menjadi dependency runtime production.

## Source of Truth

### Core owns

Core adalah sumber kebenaran untuk:

- domain dan business rules;
- database dan migration;
- API contracts;
- authentication, authorization, permissions, membership;
- CSRF dan security behavior;
- routing production;
- validation;
- lifecycle/status transitions;
- financial calculations;
- audit/immutability rules;
- deployment dan production behavior.

### Design System owns

Design System adalah sumber kebenaran untuk visual/UX yang sudah disetujui, termasuk:

- design tokens;
- typography;
- spacing, radius, shadow;
- component visual patterns;
- layout;
- sidebar/topbar presentation;
- modal/dialog/form/table/card presentation;
- responsive behavior;
- light/dark visual specification;
- interaction design;
- product/UX capability proposals.

Jika ada konflik, Core menang untuk behavior/business/security. Design System menang untuk presentation yang sudah approved. Behavior baru yang hanya muncul di prototype adalah proposal sampai diaudit Core.

## Dokumen Bridge

### Core → Design

`docs/integration/design-system-handoff.md`

Dimiliki dan diperbarui oleh proses Core. Claude/Design System membacanya secara read-only untuk mengetahui perubahan aplikasi nyata yang berdampak ke desain.

### Design → Core

`sipacul-design-system/docs/integration/core-integration-handoff.md`

Dimiliki dan diperbarui oleh proses Design System. Integrator Core membacanya sebelum pekerjaan UI/frontend/feature integration.

### Machine-readable state

`docs/integration/sync-state.json`

Mencatat baseline koordinasi yang dapat diperiksa otomatis tanpa menggantikan handoff naratif.

## Aturan Integrasi

1. Design HEAD tidak pernah otomatis menjadi Production Design.
2. Setiap pekerjaan UI/frontend/feature integration di Core diawali **Design System Delta Audit**.
3. Setiap sprint desain baru diawali **Core Delta Review** sesuai working agreement Design System.
4. Setiap pekerjaan mengunci exact Core SHA dan exact approved Design SHA.
5. Perubahan Design System saat integrasi sedang berjalan tidak otomatis mengubah scope task aktif; perubahan tersebut masuk delta audit berikutnya kecuali critical fix disetujui eksplisit.
6. Porting dilakukan sebagai **port/adapt visual**, bukan merge dua aplikasi.
7. Jangan membawa mock data, vanilla-JS prototype behavior, fake API, fake permissions, fake security behavior, atau business rule buatan prototype ke Core.
8. Jangan menambahkan Tailwind, Radix, shadcn, Zustand, atau dependency referensi lain ke Core hanya karena prototype/referensi menggunakannya.
9. Core production behavior seperti API, permissions, auth/session, CSRF, validation, lifecycle, dan form-data-loss protection harus dipertahankan saat UI dipindahkan.
10. Fungsi baru dari proses desain dicatat sebagai capability proposal sampai capability audit Core menentukan statusnya.

## Model Baseline

Gunakan konsep berikut:

- `DESIGN_REPOSITORY_HEAD_OBSERVED`: HEAD repository Design System yang terakhir terlihat Core, termasuk commit dokumentasi.
- `DESIGN_SOURCE_HEAD_AT_HANDOFF`: commit source/prototype yang menjadi sumber desain pada handoff.
- `DESIGN_AUDITED_BY_CORE`: commit desain terakhir yang sudah direview Core.
- `DESIGN_INTEGRATED_TO_CORE`: commit/porsi desain yang benar-benar sudah masuk Core dan lulus validation/UAT.
- `CORE_SOURCE_HEAD_AT_HANDOFF`: commit Core sebelum commit handoff/dokumentasi itu sendiri, agar tidak terjadi circular SHA requirement.
- `PRODUCTION_RELEASE`: release SHA yang benar-benar sedang digunakan production.

## Capability Proposal

Jika Design System menambahkan fungsi baru, fungsi tersebut boleh terus dieksplorasi. Namun statusnya harus dicatat di handoff Design sebagai salah satu dari:

- `DESIGN_ONLY`
- `INTERACTION_PROTOTYPE`
- `CORE_BACKED_CANDIDATE`
- `NEW_PRODUCT_CAPABILITY`
- `APPROVED_FOR_INTEGRATION`
- `DEFERRED`
- `REJECTED`
- `INTEGRATED`

Integrator Core kemudian memeriksa apakah capability sudah tersedia penuh, sebagian, belum ada, atau perlu masuk Feature Expansion Register.

## Release Boundary

Perubahan bridge/dokumentasi tidak berarti UI sudah terintegrasi dan tidak berarti deployment sudah terjadi. `DESIGN_INTEGRATED_TO_CORE` hanya boleh maju setelah implementasi Core benar-benar selesai dan validation/UAT yang diperlukan lulus.
