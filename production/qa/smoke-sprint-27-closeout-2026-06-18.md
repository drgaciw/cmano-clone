# Smoke — Sprint 27 Full Closeout (S27-13)

**Date:** 2026-06-18  
**Sprint:** 27 — CMO Corpus Pipeline + Combat Bounded + Phase C Viewer  
**Stories:** S27-01..16 complete  
**Branch:** `main` @ `2e4fb07`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **741/741** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 11,192 nodes \| 22,977 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |
| ADR-009 smoke fixture (separate hash) | **PASS** — `combat-domains-smoke` pinned |

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
| S26 closeout | 698 | `ab30d35` |
| S27 must-have | 701 | S27-02..04 |
| S27 should-have | 729 | S27-05..12 |
| **S27 full closeout** | **741** | +12 nice-to-have (S27-14..16) |

## Sprint tier summary

| Tier | Stories | Verdict |
|------|---------|---------|
| Must-have | S27-01..04 | **PASS** |
| Should-have | S27-05..13 | **PASS** |
| Nice-to-have | S27-12, S27-14..16 | **PASS** |

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "ReplayGoldenSuiteTests" -v minimal
npx gitnexus analyze . --force
git diff HEAD -- src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs
git branch -a | grep sprint26 || true
```