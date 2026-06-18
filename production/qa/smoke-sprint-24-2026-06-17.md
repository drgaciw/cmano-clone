# Smoke — Sprint 24 Full-Solution Baseline (S24-01)

**Date:** 2026-06-17  
**Story:** S24-01 — Full-solution re-baseline  
**Indexed commit:** `e77696d` (`e77696d627222bce728ef9e4a8e575c1c987e4d5`)  
**Kickoff baseline:** `ca52cd0` (S23 merged @ main)  
**Branch:** `stack/sprint24/full-sln-gate`

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors, 2 warnings (xUnit2012/xUnit2031 advisory) |
| `dotnet test ProjectAegis.sln` | **PASS** — **540/540** (0 failed, 0 skipped) |
| `ReplayGoldenSuiteTests` kickoff | **PASS** — **6/6** (0 failed, 0 skipped) |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 87 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 94 |
| ProjectAegis.Delegation.Tests | 162 |
| ProjectAegis.Data.Tests | 176 |

## Baseline delta vs Sprint 23

| Ref | Commit | Total | Notes |
|-----|--------|-------|-------|
| S23 closeout baseline | `dea2151` / `1e27ed6` | **538** | Authoritative S23 stack closeout |
| S24 day-1 gate (this run) | `e77696d` | **540** | +2 vs S23; Data +38, UnityAdapter +4 (net +2 after other deltas) |

## Triage

No failures — latent post-S23 merge failures: **none**. No triage owners required.

Release-configuration re-run deferred (Debug closeout sufficient for sprint gate; optional CI parity per advisory).

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git rev-parse HEAD   # e77696d627222bce728ef9e4a8e575c1c987e4d5

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal --no-build
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "ReplayGoldenSuiteTests" -v minimal
```

## Evidence chain

- QA plan: `production/qa/qa-plan-sprint-24-2026-06-17.md`
- Sprint kickoff: `production/sprints/sprint-24-phase-b-import-present-polish.md`
- Graphite stack: `docs/superpowers/plans/sprint-24-graphite-stack.md`
- S23 baseline reference: `production/qa/smoke-sprint-23-2026-06-17.md`