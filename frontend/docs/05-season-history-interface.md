# Sprint 19A4 — Season History Interface Preview

## Purpose

Sprint 19A4 exposes the read-only land season history from Stages 19A1–19A3
inside the authenticated SiPacul workspace. It does not mutate operational
records, persist derived evaluations, or create subjective scores.

## Navigation and authorization

The **Evaluasi** navigation item opens:

```text
/evaluations/season-history
```

The item requires `finance.read`, matching the API boundary because each season
contains revenue, receivable, cost, profit, margin, and capital data. The view
also requires `lands.read` to load the land and plot selector. Current Owner,
Admin, and Finance roles have both permissions; Operator does not receive the
navigation item.

## Filters

The interface supports:

- land selection;
- optional plot selection;
- terminal-only history by default;
- explicit inclusion of Planned and InProgress cycles;
- 10, 20, or 50 seasons per page; and
- server-backed previous/next pagination.

Changing the land, plot, active-season option, or page size resets pagination
to page one. Every identifier is encoded by the API client before the request
is sent.

## Page-level facts

The summary cards intentionally distinguish page-level values from the entire
filtered history:

- total matching season count comes from the API pagination metadata;
- visible seasons, readiness, and attention counts use only the current page;
- outstanding receivable is explicitly labeled as the sum on the current page.

This avoids implying that a partial page is a complete portfolio aggregate.

## Season detail

For the selected season, the view shows:

- planned and actual start/harvest dates with variance;
- crop-cycle status and confirmed harvest batch count;
- activity completion and SOP compliance;
- field issue count;
- recognized and collected revenue;
- outstanding receivable;
- cultivation cost;
- profit or loss and margin;
- capital funding gap; and
- all deterministic attention indicators with severity and code-defined units.

The 14 attention codes retain their domain meaning. Delay values are shown in
days, activity/SOP values as record counts, and financial values in IDR.

## No score or recommendation engine

The interface does not calculate a single overall score, rank commodities, or
generate recommendations. Such conclusions require approved commodity-aware
policies and authored agronomic context. This stage presents reproducible facts
so later manual findings and cross-season comparisons can remain auditable.

## Verification scope

Frontend verification covers:

- filter serialization and encoded identifiers;
- percentage, date, variance, attention-unit, and page-summary helpers;
- TypeScript compilation through the production build;
- ESLint; and
- the full existing Vitest suite.

No backend test is repeated because Stage 19A3 already passed the complete
backend suite and this stage changes only frontend files.
