# Smoke — Sprint 28 Day-1 Baseline (S28-01)

**Date:** 2026-06-18  
**Story:** S28-01 — Full-solution re-baseline  
**Branch:** `main` @ `e680075`

## Verdict: **PASS**

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **741/741** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 11,327 nodes \| 23,129 edges |
| `DelegationBridge.cs` ZERO touch | **PASS** — no diff vs HEAD |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 123 |
| ProjectAegis.Delegation.Tests | 195 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 136 |
| ProjectAegis.MissionEditor.Cli.Tests | 25 |
| ProjectAegis.Data.Tests | 257 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S27 closeout | 741 | trunk @ `7088ce6` |
| **S28 day-1** | **741** | unchanged; trunk @ `e680075` |

## GitNexus index

| Metric | Value |
|--------|-------|
| Indexed commit | `e680075` |
| Nodes | 11,327 |
| Edges | 23,129 |
| Clusters | 279 |
| Flows | 300 |
| Duration | 20.2s |

## Commands

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