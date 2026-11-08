# Smoke — Sprint 27 Day-1 Baseline (S27-01)

**Date:** 2026-06-18  
**Story:** S27-01 — Full-solution re-baseline  
**Branch:** `main` @ `f32174b`

## Verdict: **PASS**

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **701/701** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 10,943 nodes \| 22,424 edges |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 115 |
| ProjectAegis.Delegation.Tests | 182 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 121 |
| ProjectAegis.MissionEditor.Cli.Tests | 24 |
| ProjectAegis.Data.Tests | 254 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S26 closeout | 698 | trunk @ `ab30d35` |
| **S27 day-1** | **701** | +3 S27-03/04 import tests |

## Commands

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
```