# Smoke — Sprint 28 Full Closeout (S28-13)

**Date:** 2026-06-18  
**Sprint:** 28 — CMO Corpus v2 + Platform Write Path + Combat Phase 2  
**Stories:** S28-01..11 complete; S28-12 deferred (nice-to-have); S28-13 closeout  
**Branch:** `main` @ `d210d3d`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **787/787** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 11,851 nodes \| 24,408 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |
| ADR-009 smoke fixture (separate hash) | **PASS** — `combat-domains-smoke` pinned (S27-16; unchanged) |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 149 |
| ProjectAegis.Delegation.Tests | 197 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 146 |
| ProjectAegis.MissionEditor.Cli.Tests | 25 |
| ProjectAegis.Data.Tests | 265 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S27 closeout | 741 | `2e4fb07` |
| S28 day-1 (S28-01) | 741 | `e680075` — unchanged baseline |
| S28 must-have (S28-02..04) | 741+ | corpus v2 + Phase D write path landed |
| **S28 full closeout** | **787** | +46 should-have/nice-to-have (S28-05..11) |

## Sprint tier summary

| Tier | Stories | Verdict |
|------|---------|---------|
| Must-have | S28-01..04 | **PASS** |
| Should-have | S28-05..08, S28-13 | **PASS** |
| Nice-to-have | S28-09..11 | **PASS** |
| Deferred | S28-12 (CI/local gate refresh) | **DEFER** — doc-only; non-blocking |

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
git branch -a | grep sprint27 || true
```