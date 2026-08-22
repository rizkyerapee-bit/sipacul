# Sprint 19A2 — Land Season History Read Model Preview

## Purpose

Sprint 19A2 turns the deterministic season calculator from Stage 19A1 into an
organization-scoped, read-only history service for a land and its plots.

The stage reads facts already stored by SiPacul and returns one evaluation per
crop cycle. It does not persist a duplicate evaluation snapshot, mutate source
records, add an HTTP endpoint, or change the database schema.

## Accepted MVP boundary

The read model combines:

- land and plot identity;
- crop-cycle lifecycle and dates;
- commodity identity;
- activity status and issue counts;
- SOP linkage and compliance counts;
- confirmed harvest-batch count;
- recognized and collected sale revenue;
- actual activity-resource cost;
- confirmed manual cultivation expense;
- confirmed investor and partner capital;
- capital funding gap; and
- the Stage 19A1 evaluation metrics and attention indicators.

No harvest quantity is summed across incompatible units. Profit-sharing
settlements remain independent immutable records and are not recalculated by
this read model.

## Application contracts

The feature adds:

- `SeasonHistoryFilter`;
- `LandSeasonHistoryResponse`;
- `SeasonEvaluationResponse`;
- `SeasonEvaluationAttentionResponse`;
- `ISeasonHistoryService`;
- `SeasonHistoryService`;
- `ISeasonHistoryReadRepository`;
- `SeasonHistoryPageSource`; and
- `SeasonHistoryCycleSource`.

The response contains current land and plot identity plus calculated season
facts. The source record remains internal to the application boundary so the
database query can be optimized later without changing the future HTTP or
frontend contracts.

## Organization and land isolation

Every query is constrained by `OrganizationId` and `LandId`.

The service additionally rejects a source row when it:

- belongs to another organization;
- belongs to another land;
- belongs to a plot outside the requested plot filter; or
- references a plot that is no longer part of the land aggregate.

This defense-in-depth check prevents an infrastructure regression from
silently leaking history between organizations or lands.

## Default history semantics

Formal history defaults to terminal crop cycles only:

- `Completed`; and
- `Cancelled`.

`Planned` and `InProgress` cycles can be included explicitly with
`IncludeNonTerminal`. They are evaluated using Stage 19A1 rules and carry the
`CycleNotTerminal` warning.

Cycles are ordered by planned start date descending, then code descending, and
finally identifier. This provides deterministic newest-first pagination.

## Pagination

The default page is `1` with `20` seasons. The maximum page size is `50`.

The response exposes:

- current page and page size;
- total matching season count;
- total page count;
- previous-page indicator; and
- next-page indicator.

Pagination is part of the application contract before the endpoint exists so
the future SaaS API does not need to introduce a breaking response shape when
history becomes large.

## Batch aggregation

The infrastructure repository loads a bounded crop-cycle page and aggregates
the related sources in batches. It does not execute a complete profitability
query sequence once for every season.

The batch uses the existing `ProfitabilitySourceAggregator`, including its
sale discount and payment-allocation rules. This keeps the profitability page,
profit-sharing workflow, and season history consistent.

The read model includes only source states already recognized by profitability:

- actual activity resources;
- confirmed manual expenses;
- confirmed capital contributions;
- confirmed sales;
- confirmed payments; and
- confirmed harvest batches.

## Failure behavior

Invalid identifiers and pagination return validation errors. Missing land or
plot references return not-found errors.

Inconsistent source facts return `SeasonHistory.SourceDataInvalid`, including:

- a missing commodity;
- cross-organization or cross-land rows;
- incompatible harvest units within one cycle;
- sold quantity above confirmed harvest;
- collected revenue above recognized revenue; and
- invalid Stage 19A1 activity, SOP, date, or money invariants.

Cancellation tokens are propagated to land and history reads. Cancellation is
not converted into a business failure.

## No persistence or migration

Stage 19A2 adds no table because all calculated values can be reproduced from
existing operational records. Manual findings and recommendations planned for
Stage 19C will be persisted separately because they are authored business
records rather than derived metrics.

## Planned continuation

1. Stage 19B will expose the paged read model through an authorized HTTP
   endpoint.
2. Stage 19C will persist manual findings, lessons learned, and recommendations
   without mutating source records.
3. Stage 19D will add land history, season detail, and comparison views.
4. Stage 19E will run isolated end-to-end verification and browser UAT.
