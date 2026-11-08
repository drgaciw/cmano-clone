# Sprint 27 — QA / Sim / DevOps Track Plan

**Date:** 2026-06-18  
**Kickoff:** `production/sprints/sprint-27-cmo-corpus-combat-bounded.md`  
**Trunk:** `main` @ `ab30d35` (698/698; ReplayGolden 6/6)  
**Review mode:** lean — Buildkite = merge authority; GHA CodeQL advisory (billing)

## Goal

Gate Sprint 27 on ≥698 test floor, 6/6 replay, fresh GitNexus @ trunk, scoped filters per track, and closeout sign-off.

## Woven stories

| ID | Gate | Deliverable |
|----|------|-------------|
| S27-Q01 | Day-1 | `smoke-sprint-27-*.md`; `tests_passed_sprint27_baseline` |
| S27-Q02 | Per data merge | WriteGate filter + `gitnexus impact CatalogWriteGate` |
| S27-Q03 | Per sim merge | ReplayGolden 6/6; `combatDomainsEnabled=false` guard |
| S27-Q04 | Per Unity merge | Headless PlayMode filters; ZERO touch DelegationBridge |
| S27-Q07 | Before S27-02+ | `qa-plan-sprint-27-*.md` |
| S27-Q08 | Closeout | Full Release parity + prune `stack/sprint26/*` |

Maps to program stories **S27-01** (day-1) and **S27-13** (closeout).

## Day-1 bash gate

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git checkout main && git pull --ff-only
dotnet build ProjectAegis.sln && dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## Merge policy

- **Required:** `buildkite/cmano-clone`
- **Mandatory filters:** WriteGate on data PRs; ReplayGolden on sim PRs
- **DelegationBridge:** reject PRs with diffs unless ADR waiver

## Per-project baseline (S26 closeout)

| Project | Passed |
|---------|--------|
| Sim.Tests | 115 |
| Delegation.Tests | 182 |
| Delegation.UnityAdapter.Tests | 121 |
| MissionEditor.Cli.Tests | 24 |
| Data.Tests | 251 |
| Data.Excel.Tests | 5 |
| **Total** | **698** |

*Condensed from parallel QA/DevOps planning agent — 2026-06-18.*