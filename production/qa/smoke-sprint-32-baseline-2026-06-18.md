# Smoke — Sprint 32 Day-1 Baseline (S32-01)

**Date:** 2026-06-18  
**Sprint:** 32 — Release Train Ops, Combat Phase 6 & Platform Editor Phase F  
**Story:** S32-01 — Full-solution re-baseline  
**Branch:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors (4 warnings) |
| `dotnet test ProjectAegis.sln` | **PASS** — **1006/1006** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 14,424 nodes \| 29,218 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 229 |
| ProjectAegis.Delegation.Tests | 230 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 189 |
| ProjectAegis.MissionEditor.Cli.Tests | 33 |
| ProjectAegis.Data.Tests | 320 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S31 closeout | 1006 | `3406bc4` indexed commit (plan ref) |
| **S32 day-1** | **1006** | @ `d3db76db` stack tip |

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
```

## Next

Wave 1 parallel: `/dev-story dispatch S32-02 S32-04`