# Sprint 19B3B1 — Season Review API Preview

The API exposes create, read by identifier, read by crop cycle, update draft, and finalize operations under an organization-scoped route.

Read operations require `cultivation.read`. Mutating operations require `cultivation.write`. Application errors continue through the shared HTTP problem mapping. This checkpoint adds no permission, role, persistence, migration, database, or frontend changes.
