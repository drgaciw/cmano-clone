# Smoke — Sprint 30 Day-1 Baseline (S30-01)

**Date:** 2026-06-18  
**Sprint:** 30 — TL Bind, Corpus Scale & Combat Phase 4  
**Story:** S30-01 full-solution re-baseline  
**Branch:** `main` @ `3406bc4`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **878/878** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 12,852 nodes \| 26,452 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 186 |
| ProjectAegis.Delegation.Tests | 212 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 169 |
| ProjectAegis.MissionEditor.Cli.Tests | 26 |
| ProjectAegis.Data.Tests | 280 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S29 closeout | 878 | `e447159` |
| **S30 day-1 (S30-01)** | **878** | unchanged baseline — gate green |

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