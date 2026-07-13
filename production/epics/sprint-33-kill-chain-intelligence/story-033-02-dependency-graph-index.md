---
id: S33-02
status: Complete
last_updated: 2026-06-19
completed: 2026-06-19
type: Integration
priority: must-have
graphite_branch: stack/sprint33/dependency-graph-index
estimate_days: 2.5
dependencies:
  - S33-01 green baseline
owner: team-data
sprint: 33
req_trace: DBI-1.5, DBI-1.1, DBI-1.4, DBI-7.3
sprint_gate: true
---

# Story 033-02 — Weapon→Mount→Sensor Dependency Graph (DBI-1.5)

> **Epic:** sprint-33-kill-chain-intelligence  
> **ADR:** ADR-006, ADR-011

## Summary

Materialize sorted dependency edges (`platform → mount → weapon`, `platform → sensor`) in `CatalogDependencyGraphIndex`; expose via `ICatalogReader`; invalidate on `CatalogWriteGate.ApproveBatch` commit.

## Acceptance Criteria

- [x] `GetSortedDependencyEdges()` with stable `(platformId, mountId, weaponId, sensorId)` keys
- [x] Cache invalidated on batch commit (same pattern as `SqliteCatalogReader`)
- [x] Baltic + `ship-slice-100` fixtures only in CI
- [x] Evidence: `production/agentic/sprint-33-dependency-graph-2026-06-19.md`
- [x] `CatalogWriteGate` extend-only; ZERO touch `DelegationBridge.cs`

## Verify Commands

```bash
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "DependencyGraph|WriteGate|Platform" -v minimal
npx gitnexus impact CatalogWriteGate
npx gitnexus impact ICatalogReader
```