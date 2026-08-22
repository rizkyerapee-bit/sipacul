# Sprint 19A3 — Season History API Preview

## Purpose

Sprint 19A3 exposes the paged, read-only land season history from Stage 19A2
through the SiPacul HTTP API. It does not change evaluation calculations,
persist derived results, add a database migration, or add frontend behavior.

## Endpoint

```text
GET /api/v1/organizations/{organizationId}/lands/{landId}/season-history
```

Supported query parameters:

- `landPlotId`: optional plot filter;
- `includeNonTerminal`: defaults to `false`;
- `page`: defaults to `1`; and
- `pageSize`: defaults to `20`, with `50` enforced by the application service
  as the maximum.

The response uses the existing `LandSeasonHistoryResponse` contract. Formal
history therefore remains terminal-only by default and newest-first according
to the deterministic ordering defined in Stage 19A2.

## Authorization boundary

The endpoint requires `finance.read` within the organization from the route.
This is intentional because the response includes recognized revenue,
collections, receivables, cultivation cost, net profit, margin, confirmed
capital, and funding gap.

The current Owner, Admin, and Finance roles have this permission. Operator does
not. This prevents an operational cultivation permission from implicitly
exposing financial results.

Organization scope is resolved by the existing authorization policy. The
application service and repository continue to constrain every source query by
the same organization and land identifiers as defense in depth.

## HTTP behavior

- `200 OK`: a valid paged history response, including an empty season list;
- `400 Bad Request`: invalid pagination, empty filter identifiers, or malformed
  query values;
- `401 Unauthorized`: no authenticated user;
- `403 Forbidden`: the user lacks `finance.read` for the organization;
- `404 Not Found`: the route does not match or the land/plot is absent in the
  organization; and
- `500 Internal Server Error`: derived source facts violate an invariant.

The last case retains the stable `SeasonHistory.SourceDataInvalid` code. It is
an internal consistency failure rather than a client conflict, so the endpoint
does not expose it as a recoverable `409`.

## Test coverage

Endpoint tests cover:

- response serialization and default query values;
- complete filter binding;
- validation, missing land, missing plot, and invalid source mappings;
- route and malformed-query rejection before service execution;
- authentication; and
- organization-scoped `finance.read` authorization.

## Planned continuation

Stage 19B will introduce authored findings, lessons learned, and recommendations
as separate persistent business records. Derived season metrics remain
read-only and reproducible from operational sources.
