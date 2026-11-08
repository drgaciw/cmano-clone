---
id: S35-16
status: Complete
type: Config
priority: nice-to-have
graphite_branch: stack/sprint35/dependency-graph-plan
estimate_days: 0.5
dependencies: []
owner: team-data
sprint: 35
req_trace: Handoff item 8; polish-scope-boundary §Explicitly Out of Scope (S35 stretch)
governing_adrs: ADR-006; ADR-011 (read-only graph extension)
---

# Story 035-16 — Dependency Graph Platform→Link Edges (Plan-Only)

> **Epic:** sprint-35-polish-foundation

## Summary

Plan-only sketch for extending `CatalogDependencyGraphIndex` (S33-02 weapon/mount/sensor edges) to **platform→link FK graph** surfaces in Platform Editor UI. Produces interface sketch and optional ADR outline — **no runtime UI or sim changes** in Sprint 35.

## Acceptance Criteria

- [x] Evidence doc: `production/agentic/sprint-35-dependency-graph-platform-link-plan-2026-06-19.md`
- [x] Documents current `GetSortedDependencyEdges()` scope vs proposed link-edge model
- [x] Interface sketch for `ICatalogReader` or adjunct reader (link FK: `LinkId`, `PlatformId`, `CommsFittingId`, stable sort keys)
- [x] Invalidation contract aligned with `CatalogWriteGate.ApproveBatch` (same pattern as S33-02)
- [x] UI consumption sketch for Platform Editor Phase H / link catalog viewer (read-only; no implementation)
- [x] Explicit deferral of runtime work to Sprint 36+ with tracker reference
- [x] ZERO touch `DelegationBridge.cs`; extend-only `CatalogWriteGate` if write surfaces mentioned

## QA Test Cases

```
Manual check: Plan doc is implementation-ready for future epic
  Setup: Read S33-02 evidence + ADR-011 Platform Editor phases
  Verify: Plan lists edge types, sort keys, invalidation, and UI touchpoints
  Pass condition: No code merged; doc sufficient for /architecture-decision or S36 epic kickoff

Manual check: Scope boundary respected
  Setup: Read polish-scope-boundary handoff item 8
  Verify: Story delivers plan-only; no new tests or UI hosts
  Pass condition: Evidence marked PLAN-ONLY; no PR to Sim or UnityAdapter
```

## Test Evidence Path

- `production/agentic/sprint-35-dependency-graph-platform-link-plan-YYYY-MM-DD.md`
- Reference: `production/agentic/sprint-33-dependency-graph-2026-06-19.md`

## Out of Scope

- Implementing link edges in `CatalogDependencyGraphIndex`
- Platform Editor UI runtime changes
- SchemaVersion 011 or breaking migrations
- TL Phase 5 / corpora CI