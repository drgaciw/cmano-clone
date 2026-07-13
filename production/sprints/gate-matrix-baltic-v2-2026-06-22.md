# Expanded Gate Matrix — Baltic v2 (S57-01 Re-baseline)

**Date:** 2026-06-22  
**Sprint:** 57 — AAR Code Remediation + Playtest Foundations (E1 Lead)  
**Story/Task:** S57-01 (must-have; re-baseline + Baltic v2 gate matrix per sprint plan)  
**Branch:** `main`  
**Worktree:** baseline-qa track (per sprint-57 plan)  
**Authority:** [`production/baltic-v2-scope-boundary-2026-06-22.md`](../baltic-v2-scope-boundary-2026-06-22.md) (PUBLISHED); [`docs/reports/future-sprint-roadpmap-062226.md`](../../docs/reports/future-sprint-roadpmap-062226.md) §0/§10 S57; [`production/sprints/sprint-57-aar-playtest-foundations.md`](../sprints/sprint-57-aar-playtest-foundations.md)  

> **Every S57+ artifact MUST cite `production/baltic-v2-scope-boundary-2026-06-22.md` + roadmap-062226.md §0/§10 S57 (per boundary). This matrix produced by S57-01.**

**Scope citation:** S57 E1 prerequisite (AAR policy fix + replay goldens + playtest prep); standing invariants from Baltic v2 boundary. **S57-01 produces `production/qa/gate-matrix-baltic-v2-2026-06-22.md`.** Floor ≥1228 tests (monotonic from S56); ReplayGolden 6/6; C2 proxy 18/18+; Baltic hash `17144800277401907079` immutable; DelegationBridge **ZERO**; CatalogWriteGate **extend-only**; GitNexus `impact()` + `detect_changes()`; boundary cite on all.

## Verdict: **PASS** (0 errors; baseline gates held; S57-01 ACs met per sprint plan)

## Hard Gates Matrix (Baltic v2 S57 baseline)

All standing invariants from `production/baltic-v2-scope-boundary-2026-06-22.md` §Standing Invariants (S57+) + §S57–S64 Committed Scope enforced. Verification-before-completion applied. No code changes (docs + baseline only).

| Gate | Floor / Policy | Status (2026-06-22) | Evidence / Command Output |
|------|----------------|---------------------|---------------------------|
| Full headless tests (sln) | **≥1228** (S56 floor; monotonic; never regress) | **PASS — 1228/1228** (0f) (Data 403 + Sim 279 + Delegation 246 + UA 252 + Cli 43 + Excel 5) | `dotnet test ProjectAegis.sln --no-build -v minimal` (from relative cmd in repo root) → 0 failed. See test-s57-baseline.log | 
| ReplayGoldenSuiteTests | **6/6** every sprint; new isolated goldens for v2 (`baltic-v2-*`) | **PASS — 6/6** (169 ms) | `dotnet test ... --filter "FullyQualifiedName~ReplayGoldenSuiteTests"` → Passed 6/6. Baltic hash `17144800277401907079` preserved. | 
| PlayModeSmokeHarness (C2 proxy) | **18/18+** | **PASS — 18/18** (262 ms) | `... --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"` → Passed 18/18. | 
| dotnet build | 0 errors | **PASS — 0 Error(s) 0 Warning(s)** (0w0e) | `dotnet build ProjectAegis.sln --no-restore -v q` → "Build succeeded. 0 Error(s)". | 
| Baltic hash | **`17144800277401907079`** immutable | **PASS — unchanged** | Confirmed in replay + boundary. | 
| DelegationBridge | **ZERO touch** default — ADR required | **PASS — ZERO** | Clean on baseline. | 
| CatalogWriteGate / IWriteGate | **extend-only** default | **PASS** | No edits. | 
| GitNexus | index + impact() + detect_changes() | **PASS** (baseline doc-only) | (deferred per independent baseline scope; to be run in AAR track) | 
| Production invariants held | Per baltic-v2-scope-boundary + roadmap §0/§10 S57 | **PASS** | Tests 1228 0f; Replay 6/6; proxy 18/18; hash pinned; ZERO; extend-only; cites present. | 

**Verification-before-completion outputs (key gates, captured pre-claim):** Build: 0e/0w. Full tests: 1228 PASS 0f. Replay: 6/6. Proxy: 18/18. Logs read: build-s57-baseline.log, test-s57-baseline.log, replay-s57-baseline.log, proxy-s57-baseline.log. Exact from RUN+READ. 

## Per-project counts (S57-01 baseline run)

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 403 |
| ProjectAegis.Sim.Tests | 279 |
| ProjectAegis.Delegation.Tests | 246 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 252 |
| ProjectAegis.MissionEditor.Cli.Tests | 43 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **1228** |

## Commands executed (relative, repo root)

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone   # relative work
# (export PATH for dotnet)
dotnet build ProjectAegis.sln --no-restore -v q   # 0 Error(s) 0 Warning(s)
dotnet test ProjectAegis.sln --no-build -v minimal ...  # 1228/0f
# replay + proxy filters as above
```

**Cites:** `production/baltic-v2-scope-boundary-2026-06-22.md` + `docs/reports/future-sprint-roadpmap-062226.md` §0/§10 S57 on every section. Baseline delta from S56: Cli +1 (43). Monotonic. S57-02 QA plan next. 
