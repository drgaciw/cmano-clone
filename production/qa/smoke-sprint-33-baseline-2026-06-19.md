# Smoke — Sprint 33 Day-1 Baseline (S33-01)

**Date:** 2026-06-19  
**Sprint:** 33 — Kill-Chain Intelligence, Comms Integration & Platform Editor Phase G  
**Story:** S33-01 — Full-solution re-baseline  
**Branch:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **1073/1073** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 15,210 nodes \| 30,768 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 335 |
| ProjectAegis.Sim.Tests | 264 |
| ProjectAegis.Delegation.Tests | 232 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 201 |
| ProjectAegis.MissionEditor.Cli.Tests | 36 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S32 closeout | 1073 | @ `d3db76db` |
| **S33 day-1** | **1073** | unchanged — trunk healthy |

## ReplayGolden cases (6/6)

1. `replay-golden-baltic-engage-2026-06-02.txt`
2. `replay-golden-baltic-comms-2026-06-02.txt`
3. `replay-golden-baltic-classify-2026-06-02.txt`
4. `replay-golden-baltic-stale-2026-06-04.txt`
5. `replay-golden-baltic-spoof-2026-06-04.txt`
6. `replay-golden-baltic-readiness-2026-06-04.txt`

Production Baltic world hash `17144800277401907079` — pinned in regression fixtures (unchanged).

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

Wave 1a parallel: `/dev-story dispatch S33-02 S33-04`