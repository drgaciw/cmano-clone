# Smoke — Sprint 23 Full-Solution Baseline (S23-02)

**Date:** 2026-06-17  
**Story:** S23-02 — Full-solution test gate baseline  
**Indexed commit:** `fb3fc75` (`fb3fc75d0f90eb35bc966cb02c4cfbe8bd5091a4`)  
**Kickoff baseline:** `7253381` (pre-work @ main)  
**Branch:** `stack/sprint23/full-sln-gate`

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors, 1 warning (xUnit2012 advisory) |
| `dotnet test ProjectAegis.sln` | **PASS** — **498/498** (0 failed, 0 skipped) |

## Per-project counts

| Project | Passed |
|---------|--------|
| ProjectAegis.Sim.Tests | 87 |
| ProjectAegis.MissionEditor.Cli.Tests | 21 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 90 |
| ProjectAegis.Delegation.Tests | 162 |
| ProjectAegis.Data.Tests | 138 |

## Stack-tip reference (upper Graphite branches)

| Ref | Commit | Total | Notes |
|-----|--------|-------|-------|
| Stack bottom closeout | `fb3fc75` | **498** | Authoritative gate on `full-sln-gate` |
| Stack tip (S23-01/03/04 merged) | `aa36dc9` | **513** | +15 vs baseline; verified on `approve-batch-multi` |

## Triage

No failures — latent post-S22 merge failures: **none**. No triage owners required.

Release-configuration re-run deferred (Debug closeout sufficient for sprint gate; optional CI parity per advisory).

## Commands executed

```bash
export PATH="/home/username01/.dotnet:$PATH"
cd /home/username01/cmano-clone/cmano-clone
git rev-parse HEAD   # fb3fc75d0f90eb35bc966cb02c4cfbe8bd5091a4

dotnet build ProjectAegis.sln -v minimal
dotnet test ProjectAegis.sln -v minimal --no-build
```

## Evidence chain

- Pre-work: `production/agentic/sprint-23-baseline-2026-06-17.md`
- QA plan: `production/qa/qa-plan-sprint-23-2026-07-08.md`
- Graphite stack: `docs/superpowers/plans/sprint-23-graphite-stack.md`