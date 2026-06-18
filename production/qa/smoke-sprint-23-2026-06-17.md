# Smoke — Sprint 23 Full-Solution Baseline (S23-02)

**Date:** 2026-06-17  
**Story:** S23-02 — Full-solution test gate baseline  
**Indexed commit:** `7253381` (`f81f1f9` planning trunk)  
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

## Triage

No failures — formalize at sprint kickoff in Release config; re-run before closeout.

## Evidence chain

- Pre-work: `production/agentic/sprint-23-baseline-2026-06-17.md`
- QA plan: `production/qa/qa-plan-sprint-23-2026-07-08.md`
- Graphite stack: `docs/superpowers/plans/sprint-23-graphite-stack.md`