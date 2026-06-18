---
id: S29-06
status: Complete
type: Integration
priority: should-have
graphite_branch: stack/sprint29/catalog-sim-bridge
estimate_days: 2
dependencies:
  - S29-03 complete
owner: team-data + team-simulation
sprint: 29
req_trace: Req 06 Catalog Phase B; mobility/signatures/EMCON sim consumption
---

# Story 029-06 — Catalog Phase B → Sim Consumption

> **Epic:** sprint-29-catalog-sim-bridge  
> **ADR:** ADR-006 (catalog read), ADR-011 (no ad-hoc DB writes), ADR-001 (deterministic validation)

## Summary

Wire Catalog **Phase B** rows — mobility, signatures, EMCON — into bounded sim validation/readiness paths. Sim reads catalog-sourced metadata via `ICatalogReader`; **no direct SQLite in Sim**.

## Acceptance Criteria

- [x] `ICatalogReader` exposes mobility, signatures, EMCON metadata for sim validation
- [x] Sim tests PASS with catalog-sourced Phase B metadata
- [x] Bounded validation/readiness paths consume catalog rows (not hardcoded stubs)
- [x] No direct SQLite in `ProjectAegis.Sim`
- [x] `ReplayGoldenSuiteTests` — 6/6 unchanged on default path
- [x] Evidence: `production/qa/sprint-29-catalog-sim-bridge-2026-06-18.md`
- [x] ZERO touch `DelegationBridge.cs`

## QA Test Cases

- **AC-1**: Phase B metadata from catalog
  - Given: Baltic fixture with known mobility/signatures/EMCON rows
  - When: sim validator/readiness evaluator queries catalog
  - Then: metadata matches fixture; validation reflects catalog values
  - Edge cases: missing row; partial Phase B coverage; zero mobility

- **AC-2**: No SQLite in Sim hot path
  - Given: sim tick with catalog-sourced metadata
  - When: validation/readiness evaluation runs
  - Then: reads via `ICatalogReader` only; no direct SQLite connection in Sim
  - Edge cases: test fixture seeding vs production read path

## Verify Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj \
  --filter "Combat|Domain|Readiness|Magazine" -v minimal
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "Platform|WriteGate|CatalogImport" -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
# Confirm no direct SQLite in Sim
rg -l "SQLite|SqliteConnection" src/ProjectAegis.Sim/ --glob "*.cs" || true
```

## GitNexus Symbols

| Symbol | Risk |
|--------|------|
| `ICatalogReader` | HIGH |
| `DomainValidatorRegistry` | HIGH |
| `CatalogWriteGate` | CRITICAL — no bypass |
| `DelegationBridge.cs` | ZERO touch |

## References

- S28-06 pattern: `production/epics/sprint-28-logistics-catalog-bridge/story-028-06-live-magazines.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md` (S29-06)
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*