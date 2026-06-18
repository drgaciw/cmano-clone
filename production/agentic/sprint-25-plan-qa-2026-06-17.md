# Sprint 25 — QA / Sim / DevOps Track Plan

**Date:** 2026-06-17  
**Kickoff:** `production/sprints/sprint-25-phase-b-damage-assurance.md`  
**Trunk:** `main` @ `9ecbf2c` (592/592; ReplayGolden 6/6)  
**Review mode:** lean — Buildkite = merge authority; GHA CodeQL advisory (billing)

## Goal

Gate Sprint 25 on ≥592 test floor, 6/6 replay, fresh GitNexus @ trunk, CI hygiene, and closeout sign-off.

## Woven stories

| ID | Gate | Deliverable |
|----|------|-------------|
| S25-Q01 | Day-1 | `smoke-sprint-25-*.md`; `tests_passed_sprint25_baseline` |
| S25-Q02 | Per sim/data merge | ReplayGolden 6/6 when sim/catalog touched |
| S25-Q03 | Day-1 + closeout | `sprint-25-gitnexus-*.md` @ trunk HEAD |
| S25-Q04 | Mid-sprint | Scoped filter matrix per track |
| S25-Q05 | Day-2 | Stale `stack/sprint24/*` cleanup; CI hygiene doc |
| S25-Q06 | Sim merges | Determinism audit refresh |
| S25-Q07 | Before last story | `qa-plan-sprint-25-*.md` |
| S25-Q08 | Closeout | QA sign-off + full Release parity |

Maps to program stories **S25-01** (day-1) and **S25-12** (closeout).

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
- **Advisory:** GitNexus Security Checks (billing); dismiss_stale_approvals
- **Mandatory filters:** WriteGate on data PRs; ReplayGolden on sim PRs
- **DelegationBridge:** reject PRs with diffs unless ADR waiver

*Condensed from parallel QA/DevOps planning agent — 2026-06-17.*