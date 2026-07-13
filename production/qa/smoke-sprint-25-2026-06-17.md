# Smoke — Sprint 25 Full-Solution Baseline (S25-01)

**Date:** 2026-06-17  
**Story:** S25-01 — Full-solution re-baseline  
**Indexed commit:** `9ecbf2c` (`9ecbf2cb95938687fb70045ef7c915c5f4a05fa1`)  
**Kickoff baseline:** `9ecbf2c` (S24 stack merged @ main; PRs #203–#211)  
**Branch:** `stack/sprint25/full-sln-gate`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors, 2 warnings (xUnit2012/xUnit2031 advisory) |
| `dotnet test ProjectAegis.sln` | **PASS** — **592/592** (0 failed, 0 skipped) |
| `ReplayGoldenSuiteTests` kickoff | **PASS** — **6/6** (0 failed, 0 skipped) |
| `npx gitnexus analyze . --force` | **PASS** — 9,761 nodes \| 20,194 edges \| 251 clusters \| 300 flows (17.6s) |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 93 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.Tests | 171 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 93 |
| ProjectAegis.Data.Tests | 214 |

## Baseline delta vs Sprint 24

| Ref | Commit | Total | Notes |
|-----|--------|-------|-------|
| S24 day-1 gate | `e77696d` | **540** | S24-01 authoritative day-1 |
| S24 closeout | `fd80953` | **577** | S24-09 closeout hygiene |
| S25 day-1 gate (this run) | `9ecbf2c` | **592** | +15 vs S24 closeout; +52 vs S24 day-1 (S24 stretch + merge deltas) |

## Triage

No failures — latent post-S24 merge failures: **none**. No triage owners required.

Release-configuration re-run deferred (Debug closeout sufficient for sprint gate; optional CI parity per advisory).

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone/.worktrees/sprint24-cesium
git fetch origin main && git checkout main && git pull --ff-only origin main
git rev-parse HEAD   # 9ecbf2cb95938687fb70045ef7c915c5f4a05fa1

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## Evidence chain

- Sprint kickoff: `production/sprints/sprint-25-phase-b-damage-assurance.md`
- Parallel kickoff: `production/agentic/sprint-25-parallel-kickoff-2026-06-17.md`
- QA track plan: `production/agentic/sprint-25-plan-qa-2026-06-17.md`
- Graphite stack: `docs/superpowers/plans/sprint-25-graphite-stack.md`
- S24 closeout reference: `production/qa/smoke-sprint-24-closeout-2026-06-17.md`