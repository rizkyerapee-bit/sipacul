# Sprint 19B3A — Season Review Application Preview

This checkpoint adds application contracts and lifecycle orchestration for an authored season review.

- A review can only be created for a completed or cancelled crop cycle in the same organization.
- Only one active review can exist per crop cycle.
- Draft reviews can be read, updated, and finalized.
- Finalized reviews remain readable and immutable.
- Persistence uses the existing organization-scoped repository and unit of work.

HTTP routes, authorization mapping, frontend changes, migrations, and database updates remain outside this checkpoint.
