---
id: S28-06
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint28/live-magazines
estimate_days: 1.5
dependencies:
  - S28-03 complete
owner: team-data + team-simulation
sprint: 28
req_trace: Req 16 Logistics; engage readiness gap
---

# Story 028-06 — Live Magazine Counts from Catalog

> **Epic:** sprint-28-logistics-catalog-bridge  
> **ADR:** ADR-006 (catalog read), ADR-011 (no ad-hoc DB writes)

## Summary

Req 16 bridge: catalog loadout/magazine → engage readiness / validation. Wire live magazine counts from catalog browse/import data — no direct SQLite writes outside `CatalogWriteGate`.

## Acceptance Criteria

- [x] Catalog reader exposes loadout/magazine counts for engage readiness evaluation
- [x] Readiness tests PASS with catalog-sourced magazine counts
- [x] Engage validation uses live counts (not hardcoded stub values)
- [x] No direct SQLite writes outside write gate
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Magazine count from catalog
  - Given: Baltic fixture with known loadout/magazine rows
  - When: readiness evaluator queries catalog
  - Then: counts match fixture; engage guard reflects depletion state
  - Edge cases: zero magazine; partial depletion; missing loadout row

- **AC-2**: No write-gate bypass
  - Given: magazine count update scenario
  - When: data changes required
  - Then: only propose→approve via `CatalogWriteGate` (no direct SQLite)
  - Edge cases: test fixture seeding vs production write path

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Readiness|Magazine|Combat" -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|WriteGate|CatalogImport" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `ICatalogReader` | HIGH |
| `CatalogWriteGate` | CRITICAL — no bypass |
| `DelegationBridge.cs` | ZERO touch |

## References

- S27-03 pattern: `production/epics/sprint-27-cmo-corpus-import/story-027-03-loadout-magazine-import.md`
- Kickoff: `production/sprints/sprint-28-corpus-write-combat-v2.md` (S28-06)
- QA plan: `production/qa/qa-plan-sprint-28-2026-09-18.md` *(create before implementation)*

## Completion Notes

**Completed**: 2026-06-18  
**Criteria**: All AC passing  
**Deviations**: None  
**Test Evidence**: Integration — `CatalogMagazineResolverTests`, `CatalogMagazineLedgerSeederTests`, `CatalogMagazineReadinessEngageTests`; `production/agentic/stacks/sprint28/S28-06-DONE.md`  
**Code Review**: Skipped (lean mode)