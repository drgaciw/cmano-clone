# Epic: Sprint 29 — Catalog Sim Bridge

> **Status:** Ready  
> **Sprint:** 29  
> **Dates:** 2026-10-02 → 2026-10-15  
> **Trunk:** `main` @ `1d93e86` (801/801; ReplayGolden 6/6)  
> **Layer:** Data + Simulation  
> **GDD:** `design/gdd/` (combat domains / sensor readiness)  
> **Req:** 06 Catalog Phase B consumption

## Goal

Wire **Catalog Phase B** rows — mobility, signatures, EMCON — into bounded sim validation/readiness paths. Sim reads catalog-sourced metadata via `ICatalogReader`; **no direct SQLite in Sim**.

## Governing ADRs

| ADR | Status | Relevance |
|-----|--------|-----------|
| ADR-006 | Accepted | Engine-free catalog read path |
| ADR-011 | Accepted | Catalog data sourced via gate-approved snapshots |
| ADR-001 | Accepted | Deterministic validation/readiness evaluation |

## Graphite Stack (merge order)

```
main
 └── stack/sprint29/full-sln-gate              (S29-01)
      └── stack/sprint29/corpus-approve        (S29-03 — data prerequisite)
           └── stack/sprint29/catalog-sim-bridge (S29-06)
```

**Dependency:** S29-06 blocked on S29-03 (approve workflow must land first).

## Stories

| # | Story | ID | Type | Priority | Est. | Status |
|---|-------|-----|------|----------|------|--------|
| 06 | [Catalog Phase B → sim consumption](story-029-06-phaseb-sim-consumption.md) | S29-06 | Integration | should-have | 2d | Not Started |

Note: **S29-01** day-1 baseline lives in `sprint-29-closeout-devops` epic (shared gate).

## GitNexus Mandatory Rules

- **CRITICAL extend-only:** `CatalogWriteGate` — no direct SQLite writes outside gate
- **ZERO touch:** `DelegationBridge.cs`
- **HIGH:** `ICatalogReader`, `DomainValidatorRegistry`
- **Read path only** in Sim — no hot-tick SQLite; writes remain propose→approve via gate

## Should-Have Cut Line

Cut order 2: drop S29-06 before S29-04 import UI if capacity tight.

## Definition of Done

- [ ] Sim tests PASS with catalog-sourced mobility/signatures/EMCON metadata
- [ ] No direct SQLite in `ProjectAegis.Sim`
- [ ] ReplayGolden 6/6 unchanged on default path
- [ ] Evidence: `production/qa/sprint-29-catalog-sim-bridge-*.md`
- [ ] Tracker row 06 updated

## References

- S28-06 pattern: `production/epics/sprint-28-logistics-catalog-bridge/story-028-06-live-magazines.md`
- Kickoff: `production/sprints/sprint-29-operationalize-data-fight-loop.md`
- Parallel kickoff: `production/agentic/sprint-29-parallel-kickoff-2026-06-18.md` *(create at kickoff)*
- Track plan: `production/agentic/sprint-29-plan-data-2026-06-18.md` *(create at kickoff)*
- QA plan: `production/qa/qa-plan-sprint-29-2026-10-02.md` *(create before implementation)*