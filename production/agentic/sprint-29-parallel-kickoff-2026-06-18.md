# Sprint 29 Parallel Kickoff — 2026-06-18

**Sprint:** 29 — Operationalize Data-to-Fight Loop  
**Plan:** `production/sprints/sprint-29-operationalize-data-fight-loop.md`  
**QA Plan:** `production/qa/qa-plan-sprint-29-2026-10-02.md`  
**Trunk:** `main` @ S29-01 baseline (801/801, ReplayGolden 6/6)  
**Review mode:** `lean`

## Sprint Goal (one line)

Operationalize **data-to-fight**: TL export Phases 1–2, nightly corpus **approve**, Unity import UI, bounded Baltic combat enable — without hot-tick damage or full-corpus CI.

## Parallel Tracks

| Track | Owner skill | Stories | Graphite prefix | Gate filter |
|-------|-------------|---------|-----------------|-------------|
| **Data** | team-data | S29-02, S29-03, S29-06, S29-10 | `stack/sprint29/tl-export`, `stack/sprint29/corpus-approve` | WriteGate\|Platform\|Snapshot\|CatalogImport |
| **Unity** | team-unity | S29-04, S29-07, S29-08 | `stack/sprint29/platform-import-ui`, `stack/sprint29/c2-loop` | PlatformCatalog\|Doctrine\|PlayModeSmoke |
| **Simulation** | team-simulation | S29-05, S29-09, S29-11 | `stack/sprint29/combat-phase3` | Combat\|Domain\|Datalink |
| **DevOps** | c-sharp-devops-engineer | S29-01, S29-12, S29-13 | `stack/sprint29/closeout` | Full sln + ReplayGolden |

## Dispatch Order (after S29-01 baseline ✅)

**Wave 1 (parallel):**
- S29-02 TL export Phase 1–2
- S29-03 Nightly corpus approve *(sprint gate — blocks S29-04)*

**Wave 2:**
- S29-04 Unity import UI (after S29-03)
- S29-05 Baltic combat enable (parallel with Wave 1 after S29-01)

**Wave 3:**
- S29-06 Phase B sim consumption (after S29-03)
- S29-07 Doctrine visual ∥ S29-08 Begin Execution

**Closeout:**
- S29-13 (after S29-03+ must-have gate)

## Hard Constraints

- **ZERO touch** `DelegationBridge.cs`
- **CatalogWriteGate** extend-only
- **ReplayGolden 6/6** on sim/delegation merges
- **`combatDomainsEnabled=false`** on Baltic until S29-05 golden passes
- **No TL runtime binding** (`TlBranch`, `BranchDatabase`) in S29
- **No 7208 sensor.md in CI**

## Prerequisites (complete)

- [x] `/sprint-plan new` — sprint-29-operationalize-data-fight-loop.md
- [x] `/qa-plan sprint` — qa-plan-sprint-29-2026-10-02.md
- [x] 7 epics + 13 story files scaffolded
- [x] S29-01 day-1 baseline GREEN 801/801

## Next Command

`/dev-story dispatch S29-02/03/05` — Wave 1 parallel (after commit @ baseline)