# Sprint 19A1 — Season Evaluation Domain Preview

## Purpose

This stage starts Sprint 19, Land History and Season Evaluation.

It introduces a deterministic domain report for evaluating one crop cycle. It
does not yet add persistence, an HTTP endpoint, a migration, or a frontend.

The purpose of this separation is to stabilize evaluation language and rules
before historical data is aggregated across land plots and seasons.

## Accepted MVP boundary

Stage 19A1 evaluates facts that SiPacul already records:

- crop-cycle lifecycle and date variance;
- activity completion and cancellation;
- field issues recorded on activities;
- SOP compliance, deviation, and missing evaluation;
- presence of confirmed harvest batches;
- recognized and collected revenue;
- cultivation cost, profit or loss, and funding gap.

This stage does not:

- assign a subjective overall score;
- generate recommendations using artificial intelligence;
- compare different commodities as if they used identical units;
- aggregate harvest quantities with incompatible units;
- persist evaluation notes;
- mutate historical crop-cycle, finance, harvest, or settlement records.

## Domain types

The domain foundation consists of:

- `SeasonEvaluationInput` — normalized source facts for one crop cycle;
- `SeasonEvaluationReport` — calculated metrics and review readiness;
- `SeasonEvaluationAttention` — one deterministic attention indicator;
- `SeasonEvaluationAttentionCode` — stable machine-readable reason;
- `SeasonEvaluationAttentionSeverity` — information, warning, or critical;
- `SeasonEvaluationCalculator` — validation, calculation, and ordering rules.

The report carries organization, crop-cycle, land, plot, and commodity
snapshots. This prepares a later read model without weakening organization
isolation.

## Readiness

A season is ready for formal review when the crop cycle is terminal:

- `Completed`; or
- `Cancelled`.

`Planned` and `InProgress` cycles may still be previewed, but receive the
`CycleNotTerminal` warning and are not ready for formal review.

## Calculated metrics

The calculator produces:

- start variance in days;
- harvest variance in days;
- activity completion percentage;
- SOP compliance percentage;
- outstanding receivable;
- net profit;
- profit margin percentage;
- profitability outcome;
- counts by attention severity.

Percentages are rounded to four decimal places. Money is rounded to two
decimal places using `MidpointRounding.AwayFromZero`, consistent with the
existing profitability module.

An activity percentage is null when no activities exist. An SOP percentage is
null when no activities are linked to an SOP. This distinguishes “no data”
from a real zero-percent result.

## Attention indicators

The first version uses stable codes rather than free-form conclusions.

| Code | Severity | Trigger |
| --- | --- | --- |
| `CycleNotTerminal` | Warning | Cycle is Planned or InProgress. |
| `CycleCancelled` | Critical | Cycle ended as Cancelled. |
| `LateStart` | Warning | Actual start is after planned start. |
| `LateHarvest` | Warning | Actual harvest is after expected harvest. |
| `ActivitiesIncomplete` | Warning | A terminal cycle still has pending activities. |
| `ActivitiesCancelled` | Warning | One or more activities were cancelled. |
| `ActivityIssuesRecorded` | Warning | One or more activities contain issue notes. |
| `SopDeviationRecorded` | Warning | One or more SOP-linked activities deviated. |
| `SopNotEvaluated` | Warning | A terminal cycle has SOP-linked activities not evaluated. |
| `NoConfirmedHarvest` | Critical | A completed cycle has no confirmed harvest. |
| `OutstandingReceivable` | Warning | Collected revenue is below recognized revenue. |
| `BreakEven` | Information | Net profit is zero. |
| `Loss` | Critical | Net profit is negative. |
| `CapitalFundingGap` | Warning | Confirmed capital is below cultivation cost. |

The optional numeric value has a meaning defined by its code:

- days for late start and late harvest;
- record count for activity and SOP indicators;
- IDR amount for receivable, loss, and funding gap.

## Validation invariants

The calculator rejects:

- empty organization, cycle, land, plot, or commodity identifiers;
- blank identity snapshots;
- unsupported crop-cycle status;
- invalid planned or actual date combinations;
- negative counts;
- activity status counts that do not reconcile to the total;
- SOP status counts that do not reconcile to linked activities;
- SOP-linked count above total activity count;
- issue count above total activity count;
- negative money;
- collected revenue above recognized revenue;
- default generation timestamp.

## Why there is no overall score yet

An overall score would require business-approved weights and thresholds. For
example, a five-day delay can be harmless for one commodity but severe for
another. Adding a generic score now would create false precision and make
future SaaS customization harder.

Stage 19 will first expose comparable facts and explicit attention codes. A
scorecard may be added later through versioned policies per commodity or
tenant, without changing historical reports.

## Planned continuation

The next stages are intentionally separated:

1. Stage 19A2: aggregate organization-scoped history for a land and plot from
   existing crop-cycle, activity, harvest, sale, payment, expense, capital,
   profitability, and settlement data.
2. Stage 19B: expose season history and comparison through HTTP endpoints.
3. Stage 19C: persist manual findings, lessons learned, and next-season
   recommendations without mutating source records.
4. Stage 19D: build the land-history and season-comparison frontend.
5. Stage 19E: run isolated end-to-end verification and browser UAT.

No AI recommendation engine is required for the MVP. The structured data and
manual recommendations created by Sprint 19 will be suitable input for such a
feature later.
