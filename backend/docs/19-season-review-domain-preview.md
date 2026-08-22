# Sprint 19B1 — Authored Season Review Domain Preview

## Purpose

This stage introduces the domain foundation for a manual review of one crop
cycle. It complements the reproducible metrics from Sprint 19A without
changing operational, harvest, sale, finance, profitability, or settlement
records.

## Accepted MVP boundary

One authored review records:

- findings from the completed or cancelled season;
- lessons learned from execution and outcomes; and
- recommendations for the next season.

All three narratives are required, trimmed, and limited to 4,000 characters.
Users can explicitly state that no special finding exists instead of leaving
an ambiguous blank value.

The aggregate is organization-owned and linked to one crop cycle. Application
and persistence stages will enforce that the cycle belongs to the same
organization, is terminal, and has at most one active review.

## Lifecycle

A review begins as `Draft`. A draft can be edited and finalized. Finalization
stores its UTC timestamp and makes authored content immutable.

The first MVP does not reopen or overwrite a finalized review. A later stage
may add an explicit void-and-replace workflow if operations require a
correction, preserving the original audit trail.

## Audit and derived facts

`SeasonReview` inherits the existing auditable aggregate foundation. Created
and updated user identities remain the responsibility of the current audit
interceptor.

The review stores only authored business content. Derived metrics and the 14
attention indicators continue to be calculated from their source records and
are never copied into this aggregate.

## Excluded from this stage

This domain-only stage adds no:

- EF Core configuration or migration;
- repository or application service;
- HTTP endpoint or permission mapping;
- frontend form or comparison interface;
- automated recommendation or overall score; or
- modification of historical source records.

## Planned continuation

1. Sprint 19B2 will add persistence, organization/crop-cycle constraints, a
   migration, and PostgreSQL verification.
2. Sprint 19B3 will add application services, authorized HTTP endpoints, and
   backend integration tests.
3. Sprint 19B4 will add the review form and display inside season history.
4. Sprint 19C will add auditable cross-season comparison without comparing
   incompatible commodities or harvest units.
5. Sprint 19D will run isolated end-to-end verification and browser UAT.
