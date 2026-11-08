# Smoke — Sprint 41 Baseline Re-Baseline (S41-01)

**Date:** 2026-06-20  
**Sprint:** 41 — Polish Hardening + Release-Readiness Pre-Flight  
**Story:** S41-01 (must-have; blocks S41-02+ waves)  
**Branch:** `main` @ `ec0d3d9` (foreground baseline run; API-limited subagent did not complete)  
**Worktree:** `/home/username01/cmano-clone/.worktrees/sprint41-baseline` → `stack/sprint41/baseline`  
**Review Mode:** lean (per `production/review-mode.txt`)  
**Authority:** `production/polish-scope-boundary-2026-06-19.md` + `sprint-41-polish-hardening-release-preflight.md`

## Verdict: **PASS**

## Gate results

| Gate | Result |
|------|--------|
| `dotnet build ProjectAegis.sln` | **PASS** — 0 errors (7 pre-existing warnings) |
| `dotnet test ProjectAegis.sln` | **PASS** — **1226/1226** (target ≥1226 post-S40) |
| `ReplayGoldenSuiteTests` | **PASS** — **6/6** (~227 ms, UnityAdapter filter) |
| C2 headless proxy checks | **PASS** — **18/18** (`PlayModeSmokeHarnessTests`) |
| `DelegationBridge.cs` | **PASS** — no edits this story |
| Production Baltic world hash | **PASS** — unchanged per ReplayGolden (S40 closeout `17144800277401907079`) |

## Per-project counts (observed @ S41-01 run)

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 403 |
| ProjectAegis.Sim.Tests | 279 |
| ProjectAegis.Delegation.Tests | 245 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 252 |
| ProjectAegis.MissionEditor.Cli.Tests | 42 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **1226** |

## GitNexus

- **Status:** Re-index deferred to **S41-04** (determinism audit pass); trunk index expected @ HEAD post-merge.
- **Note:** Run `node .gitnexus/run.cjs analyze` in S41-04 before Polish exit.

## Unblocks

| Next | Owner | Dependency satisfied |
|------|-------|---------------------|
| S41-02 | team-qa | QA plan (`production/qa/qa-plan-sprint-41-*.md`) |
| S41-04 | determinism-engineer | Partial — full audit still requires S41-02 |

## Coordinator note

Background subagent [S41-01 baseline](9034d29a-138c-435a-92ea-f4dd2c0c33d9) hit API usage limit; this smoke doc records the foreground verification run.
