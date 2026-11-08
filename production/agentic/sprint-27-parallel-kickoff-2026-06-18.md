# Sprint 27 Parallel Planning Kickoff

**Date:** 2026-06-18  
**Method:** `/superpowers:dispatching-parallel-agents` — 4 isolated domain agents  
**Output:** Integrated kickoff @ `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`  
**Trunk:** `main` @ `ab30d35` (Sprint 26 complete; 698/698 baseline)

## Dispatch Summary

| Agent | Domain | Stories proposed | Loaded days |
|-------|--------|------------------|-------------|
| Data/Platform | Req 06, ADR-011 Phase C data | S27-D01..D10 → **S27-01..04, S27-09** | 6.0 must + 2.5 should |
| Unity/Presentation | ADR-011 Phase C UX, APP-6 Addressables | S27-U01..U05 → **S27-07..08, S27-10..11** | 4.0 should |
| Sim/Combat | ADR-009 bounded validators + BDA | S27-SIM-01..05 → **S27-05..06** | 2.25–3.25 should |
| QA/DevOps | Replay, GitNexus, CI hygiene | S27-Q01..Q09 → woven into **S27-01, S27-13, qa-plan** | 1.5 |

## Unified Sprint 27 Goal

Scale **bounded CMO corpus intake** to a nightly off-CI pipeline, complete **mount→loadout→magazine** markdown import through the write gate, advance **ADR-011 Phase C** platform viewer UX and **APP-6 Addressables**, and land **ADR-009 bounded** validator + order-log BDA slices — while keeping **`combatDomainsEnabled=false`** on Baltic production fixtures and **698+** tests / **6/6** replay green.

## Critical Path (Must-Have — ~6d)

```
S27-01 baseline (1d)
  → S27-03 loadout/magazine CMO import (2d)
    → S27-04 import golden hygiene (1d)
  ∥ S27-02 nightly corpus job (2d, parallel after S27-01)
```

**Sprint fails** if S27-04 loadout/magazine round-trip does not land through `CatalogWriteGate`.

## Should-Have Cut Line

| Cut order | Drop | Keep |
|-----------|------|------|
| 1 | S27-06 order-log BDA slice | S27-05 validator stubs |
| 2 | S27-11 platform viewer harness | S27-08 minimum list panel |
| 3 | S27-10 Editor evidence captures | Headless proxy only |
| 4 | S27-07 Addressables live load | S26-06 USS/atlas stub |
| 5 | S27-09 browse enrichment | CLI browse unchanged |

**Minimum shippable beyond must-have:** S27-05 + S27-13 closeout.

## Graphite Stack (merge order)

1. `stack/sprint27/full-sln-gate` (S27-01)
2. `stack/sprint27/cmo-loadout-magazine` (S27-03) — **CRITICAL**
3. `stack/sprint27/import-golden-hygiene` (S27-04)
4. `stack/sprint27/nightly-cmo-corpus` (S27-02) — parallel after #1
5. `stack/sprint27/adr009-validators-bda` (S27-05..06)
6. `stack/sprint27/addressables-app6-atlas` (S27-07)
7. `stack/sprint27/platform-viewer-panel` (S27-08..11)
8. `stack/sprint27/closeout-gitnexus` (S27-13)

## Day-1 Gates (all tracks)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git checkout main && git pull --ff-only

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal   # expect ≥698

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

npx gitnexus analyze . --force
```

## Top Risks (cross-track)

| # | Risk | Owner hint |
|---|------|------------|
| R1 | CMO corpus scope creep into CI | team-data — nightly only; curated fixtures in `dotnet test` |
| R2 | `CatalogWriteGate` extend-only violation | team-data — impact + WriteGate regression before merge |
| R3 | ADR-009 runtime creep / Baltic golden drift | team-simulation — flag-off default; test-only fixtures for flag-on |
| R4 | Unity Addressables package churn | team-unity — land S27-07 first; headless degradation path |
| R5 | Platform viewer overlaps data+unity edits | coordinator — S27-09 data projection first; S27-08 Unity bind |

## Producer Decisions (Approved 2026-06-18)

1. **S27-06** — **APPROVED** — order-log-only BDA slice (projection-only)
2. **Nightly corpus v1** — **sensor + weapon only** (platform slices → S27-14 / nightly v2)
3. **GHA billing** — **permanent local-gate advisory**; S27-12 documents path

## Next Steps

1. Producer approves kickoff + open questions
2. `/qa-plan sprint` → `production/qa/qa-plan-sprint-27-*.md` (**required before S27-02+ impl**)
3. `/create-epics sprint-27-*` + `/create-stories` per epic
4. `gt stack create` per graphite stack above
5. Day-1: S27-01 baseline before any symbol edit

*Parallel dispatch complete — 2026-06-18.*