# Cross-season comparison foundation

Sprint 19C1 adds a frontend-only comparison model for two to four terminal
seasons already present on the active land-history page.

The oldest selected season is the baseline. Later columns expose numeric deltas
as increase, decrease, unchanged, or unavailable. These directions are neutral:
the foundation does not label a change as good or bad, calculate a composite
score, rank seasons, or generate recommendations.

The comparison uses facts already returned by the audited season-history read
model. It does not recalculate accounting or agronomic facts in the browser.
Missing values remain unavailable instead of becoming zero. Context flags state
whether all selected seasons use the same commodity and plot so the interface
can warn users before interpreting cross-context differences.

Selection is limited to terminal seasons on the current server-backed page.
This keeps the MVP bounded and avoids hidden cross-page state or a new backend
endpoint. Authored reviews and the visual comparison interface follow in the
next checkpoint.
