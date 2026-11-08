# Smoke — Sprint 26 Full-Solution Baseline (S26-01)

**Date:** 2026-06-18  
**Story:** S26-01 — Full-solution re-baseline  
**Indexed commit:** `076a7eb` (`076a7eb` — S26 must-have data track merged locally)  
**Kickoff baseline:** `76b57e6` — 661/661 tests, ReplayGolden 6/6  
**Branch:** `main`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors, 2 warnings (xUnit2012/xUnit2031 advisory) |
| `dotnet test ProjectAegis.sln` | **PASS** — **669/669** (0 failed, 0 skipped) |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** (0 failed, 0 skipped) |
| `npx gitnexus analyze . --force` | **PASS** — 10,385 nodes \| 21,510 edges \| 268 clusters \| 300 flows |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch (0 lines changed) |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 105 |
| ProjectAegis.MissionEditor.Cli.Tests | 23 |
| ProjectAegis.Delegation.Tests | 177 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 108 |
| ProjectAegis.Data.Excel.Tests | 5 |
| ProjectAegis.Data.Tests | 251 |

## Baseline delta vs Sprint 25

| Ref | Commit | Total | Notes |
|-----|--------|-------|-------|
| S25 closeout | `76b57e6` | **661** | S25-01 authoritative closeout |
| S26 must-have closeout | `076a7eb` | **669** | +8 vs S25 closeout (S26-02..04 import tests) |

## Data-track filtered gates (S26-02..04 prep)

| Filter | Result |
|--------|--------|
| `CmoMarkdown\|WriteGate\|Platform\|CatalogImport` (Data.Tests) | **148/148 PASS** |
| `CatalogImport` (MissionEditor.Cli.Tests) | **5/5 PASS** |

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git rev-parse HEAD   # 76b57e6 (trunk @ kickoff)

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```

## Evidence chain

- Sprint kickoff: `production/sprints/sprint-26-cmo-phase2-presentation-closeout.md`
- QA plan: `production/qa/qa-plan-sprint-26-2026-06-18.md`
- S25 closeout reference: `production/qa/smoke-sprint-25-2026-06-17.md`