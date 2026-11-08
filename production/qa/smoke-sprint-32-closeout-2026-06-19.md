# Smoke — Sprint 32 Full Closeout (S32-13)

**Date:** 2026-06-19  
**Sprint:** 32 — Release Train Ops, Combat Phase 6 & Platform Editor Phase F  
**Stories:** S32-01..11, S32-12, S32-13 complete (13/13; S32-12/13 landed together)  
**Branch:** `main` @ `d3db76dbca07237200f5e7ad69eb5d4fdcaa118f`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors |
| `dotnet test ProjectAegis.sln` | **PASS** — **1073/1073** |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** |
| `npx gitnexus analyze . --force` | **PASS** — 15,064 nodes \| 30,605 edges |
| `DelegationBridge.cs` diff | **PASS** — ZERO touch |
| Production Baltic world hash | **PASS** — unchanged `17144800277401907079` |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 264 |
| ProjectAegis.Delegation.Tests | 232 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 201 |
| ProjectAegis.MissionEditor.Cli.Tests | 36 |
| ProjectAegis.Data.Tests | 335 |
| ProjectAegis.Data.Excel.Tests | 5 |

## Baseline delta

| Ref | Tests | Notes |
|-----|-------|-------|
| S31 closeout | 1006 | `3406bc4` |
| S32 day-1 (S32-01) | 1006 | unchanged baseline @ `d3db76db` |
| S32 Wave 1 (S32-02/04) | 1025+ | unified manifest + facility validator |
| S32 Wave 2 (S32-03/05) | 1040+ | quarantine triage + ECCM factor |
| S32 Wave 3 (S32-06/08) | 1055+ | Phase F damage + mine transit hazard |
| S32 Wave 4 (S32-07/09/10) | 1065+ | release diff + BDA lifecycle + presentation |
| S32 Wave 5 (S32-11) | 1073 | C2 sign-off upgrade headless proxy |
| **S32 closeout** | **1073** | +67 from S32 day-1; +27 from S31 closeout |

## Sprint tier summary

| Tier | Stories | Verdict |
|------|---------|---------|
| Must-have | S32-01..06 | **PASS** |
| Should-have | S32-07..11, S32-13 | **PASS** |
| Nice-to-have | S32-12 | **PASS** — CI/local gate refresh (S31-12 carryover) |

## Stack prune (`stack/sprint31/*`)

| Check | Result |
|-------|--------|
| Local `stack/sprint31/*` refs | **0** (`git branch -a \| grep sprint31` → empty) |
| Merged branches (documented) | `full-sln-gate`, `nightly-sensor-approve`, `tl-release-train`, `tl-release-train-load`, `balance-drift-nightly`, `weapon-approve-scale`, `entity-approve-scale`, `combat-phase5`, `presentation-evidence`, `c2-signoff-refresh`, `closeout` |
| Prune action | Merged branches only; no stale local refs @ closeout verify |

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
git branch -a | grep -i sprint31 || echo "0 sprint31 refs"
```