# Smoke — Sprint 31 Day-1 Baseline (S31-01)

**Date:** 2026-06-18  
**Sprint:** 31 — Corpus Complete, Combat Phase 5 & Presentation Polish  
**Story:** S31-01 full-solution re-baseline  
**Branch:** `main` @ `3406bc4`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **956/956** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 13,720 nodes \| 27,933 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 213 |
| ProjectAegis.Delegation.Tests | 213 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 185 |
| ProjectAegis.MissionEditor.Cli.Tests | 30 |
| ProjectAegis.Data.Tests | 310 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S30 closeout | 956 | `3406bc4` |
| **S31 day-1 (S31-01)** | **956** | unchanged baseline — gate green |

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