# Sprint 25 Parallel Planning Kickoff

**Date:** 2026-06-17  
**Method:** `/superpowers:dispatching-parallel-agents` — 4 isolated domain agents  
**Output:** Integrated kickoff @ `production/sprints/sprint-25-phase-b-damage-assurance.md`  
**Trunk:** `main` @ `9ecbf2c` (592/592; Sprint 24 PRs #203–#211 merged)

## Dispatch Summary

| Agent | Domain | Stories proposed | Loaded days |
|-------|--------|------------------|-------------|
| Data/Platform | Req 21 Phase B remainder, ADR-011 | S25-D01..D09 → **S25-01..07, S25-13** | 9.5 (must 6.5) |
| Unity/Presentation | ADR-007 Phase C, Req 13 | S25-U01..U07 → **S25-08..11, S25-14** | 5.25 (must 2.5) |
| QA/Sim/DevOps | Replay, GitNexus, CI hygiene | S25-Q01..Q08 → woven into **S25-01, S25-12** | 4 |
| Program coordinator | Integrated map | S25-01..14 unified | 8 effective |

## Unified Sprint 25 Goal

Close ADR-011 Phase B (damage columns + round-trip); merge S24-10/S24-11 stretch; close presentation assurance gaps (Cesium Editor, tri-batch, APP-6 atlas); maintain ≥592 tests and 6/6 replay.

## Critical Path (Must-Have — ~7d)

```
S25-01 baseline (1d)
  → S25-02 damage schema (1d)
    → S25-03 reader + export (1.5d)
      → S25-04 write-gate (2d)
        → S25-05 importer E2E (1.5d)
```

**Sprint fails** if damage round-trip commit loop does not land.

## Should-Have Cut Line

| Priority | Stories | Defer if buffer consumed |
|----------|---------|--------------------------|
| 1 (cut first) | S25-14 Cesium APP-6 billboards | Globe+atlas fusion slips |
| 2 | S25-13 damage sim consumer | Data authored; sim-unaware OK |
| 3 | S25-11 Editor tri-batch | Headless proxy only (conditions remain) |
| 4 | S25-08 APP-6 atlas (narrow to USS) | Unicode glyphs sufficient |

## Graphite Stack (merge order)

1. `stack/sprint25/full-sln-gate` (S25-01)
2. `stack/sprint25/damage-schema-009` (S25-02)
3. `stack/sprint25/damage-reader-export` (S25-03)
4. `stack/sprint25/damage-write-gate` (S25-04) — **CRITICAL review**
5. `stack/sprint25/damage-importer` (S25-05)
6. `stack/sprint25/damage-validator` (S25-06)
7. `stack/sprint25/closedxml-phase-b-ux` (S25-07) — parallel after #1
8. `stack/sprint25/doctrine-emcon-readonly` (S25-10) — parallel after #1
9. `stack/sprint25/app6-atlas-phase-c` (S25-08)
10. `stack/sprint25/cesium-editor-evidence` (S25-09)
11. `stack/sprint25/c2-editor-tri-batch` (S25-11)
12. `stack/sprint25/closeout-gitnexus` (S25-12)

## Day-1 Gates (all tracks)

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git checkout main && git pull --ff-only

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal   # expect ≥592

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal

npx gitnexus analyze --force
```

## Top Risks (cross-track)

| # | Risk | Owner hint |
|---|------|------------|
| R1 | `CatalogWriteGate` extend-only violation | team-data — impact + sensor regression |
| R2 | Damage scope creep into combat runtime | producer — catalog columns only |
| R3 | Unity Editor unavailable for U09/U11 | team-unity — headless merge authority |

## Producer Decisions Needed

1. Damage column schema sign-off before S25-02?
2. CMO Phase 2 — spike-only vs full defer to S26?
3. GitHub Actions billing remediation path by Day 2?

## Next Steps

1. Producer approves kickoff + open questions
2. `/qa-plan sprint` → `production/qa/qa-plan-sprint-25-*.md`
3. `/create-epics sprint-25-*` + `/create-stories` per epic
4. `gt stack create` per `docs/superpowers/plans/sprint-25-graphite-stack.md`
5. Day-1: S25-01 baseline before any symbol edit
6. Early parallel: merge S24-10/S24-11 branches (S25-07, S25-10) after baseline green

*Parallel dispatch complete — 2026-06-17.*