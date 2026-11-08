# Smoke — Sprint 29 Day-1 Baseline (S29-01)

**Date:** 2026-06-18  
**Sprint:** 29 — Operationalize Data-to-Fight Loop  
**Story:** S29-01 full-solution re-baseline  
**Branch:** `main` @ pre-kickoff commit (see `indexed_commit` in sprint-status after push)

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **801/801** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 12,240 nodes \| 24,819 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 156 |
| ProjectAegis.Delegation.Tests | 204 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 146 |
| ProjectAegis.MissionEditor.Cli.Tests | 25 |
| ProjectAegis.Data.Tests | 265 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S28 closeout | 801 | `1d93e86` |
| **S29 day-1 (S29-01)** | **801** | unchanged baseline — gate green |

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