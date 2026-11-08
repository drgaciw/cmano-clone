# Post-Release Gate Matrix — S49-01 Re-baseline (Baseline + Gate Matrix)

**Date:** 2026-06-21  
**Sprint:** 49 — Agentic Kickoff: MCP/OSINT Production + Infra Foundations (E2 Lead)  
**Story/Task:** S49-01 (must-have; Day-1 baseline + gate matrix per sprint plan)  
**Branch:** `main` @ post-S48 Release (RC1 cut; stage Release)  
**Worktree:** baseline-qa track (per `production/agentic/sprint-49-parallel-kickoff-2026-06-21.md` → `stack/sprint49/baseline-qa`)  
**Authority:** [`production/post-release-scope-boundary-2026-06-21.md`](../post-release-scope-boundary-2026-06-21.md) (PUBLISHED 2026-06-21); [`production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`](../sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md); [`production/agentic/sprint-49-parallel-kickoff-2026-06-21.md`](../agentic/sprint-49-parallel-kickoff-2026-06-21.md); S48 gate [`production/gate-checks/s48-release-gate-2026-06-20.md`](../gate-checks/s48-release-gate-2026-06-20.md)  

> **Every S49+ artifact MUST cite `production/post-release-scope-boundary-2026-06-21.md`** (per boundary §Cut-line rules, §Standing invariants & gate matrix). This matrix is produced by S49-01 as specified. No scope creep into S49-02+ or deferred items (E7 launch, full Req 05/07 MVP, etc.).

**Scope citation (post-release-scope-boundary-2026-06-21.md mandatory):** S49 E2 lead (Req 05 + Req 07 foundations only). Standing invariants carried forward from S48/v1.0. **S49-01 produces `production/qa/gate-matrix-post-release-2026-06-21.md`.** Floor ≥1227 tests (monotonic); ReplayGolden 6/6; C2 proxy 18/18+; Baltic hash `17144800277401907079` immutable; DelegationBridge **ZERO**; CatalogWriteGate **extend-only**; GitNexus `impact()` + `detect_changes()`; boundary cite on all artifacts.

## Verdict: **PASS** (0 errors; baseline gates held; S49-01 ACs met)

## Hard Gates Matrix (Post-Release S49 baseline @ S48 floor)

All standing invariants from `production/post-release-scope-boundary-2026-06-21.md` §Standing invariants & gate matrix enforced. **Verification-before-completion applied.** No code changes in this baseline (docs + status only).

| Gate | Floor / Policy | Status (2026-06-21) | Evidence / Command Output |
|------|----------------|---------------------|---------------------------|
| Full headless tests (sln) | **≥1227** at S49 start (`s48-release-gate-2026-06-20.md`); monotonic growth; never regress | **PASS — 1227/1227** (Data 403 + Sim 279 + Delegation 246 + UA 252 + Cli 42 + Excel 5; 0 failures) | `dotnet test ProjectAegis.sln --no-build --logger "console;verbosity=minimal"` → all projects PASS 0 failed. (Monotonic hold from S48 1227.) |
| ReplayGoldenSuiteTests | **6/6** every sprint | **PASS — 6/6** | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"` → Passed! 6/6, 175 ms. Baltic hash `17144800277401907079` preserved. |
| PlayModeSmokeHarness (C2 proxy) | **18/18+** | **PASS — 18/18** | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"` → Passed! 18/18, 273 ms. |
| dotnet build | 0 errors | **PASS — 0 Error(s)** | `dotnet build ProjectAegis.sln --no-restore -v q` → "Build succeeded. 0 Error(s)". (Pre-existing warnings only; no new critical.) |
| Baltic hash | **`17144800277401907079`** immutable | **PASS — unchanged** | Confirmed in ReplayGolden + S48 gate + all prior; per boundary. |
| DelegationBridge | **ZERO touch** default — ADR required | **PASS — ZERO** | `git diff --name-only | grep -i delegation` → none (clean); `find src -name "*DelegationBridge*"` shows only pre-existing; no edits in this baseline or S48 post-cut. |
| CatalogWriteGate / IWriteGate | **extend-only** default | **PASS** (no edits) | GitNexus pre-flight CRITICAL on Catalog* noted (per kickoff); baseline touches none. Extend-only invariant held (no deviation). |
| GitNexus | index + `impact()` + `detect_changes()` | **PASS** (status noted; stale index on docs) | `node .gitnexus/run.cjs status` → Indexed commit: 53426a3; Current: b5994c6; ⚠️ stale (re-run analyze if editing); `node .gitnexus/run.cjs detect_changes --scope unstaged` → Affected processes: 0; Risk level: low (doc changes only). No src touched; impact() not required for pure baseline. |
| Production invariants held | Per post-release-scope-boundary-2026-06-21.md | **PASS** | Tests ≥1227 monotonic; Replay 6/6; proxy 18/18; hash pinned; ZERO bridge; extend-only WriteGate; boundary cites; no hash change. |

**Verification-before-completion outputs (key gates, captured pre-claim 2026-06-21):**
- Build: succeeded (0e).
- Tests full: 1227 PASS (0 fail) across projects.
- Replay: 6/6.
- Smoke: 18/18.
- Git/bridge: clean (ZERO DelegationBridge).
- GitNexus: detect low risk on docs; status stale (no impact needed).
- Exact command outputs embedded below.

## Per-project counts (S49-01 baseline run)

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 403 |
| ProjectAegis.Sim.Tests | 279 |
| ProjectAegis.Delegation.Tests | 246 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 252 |
| ProjectAegis.MissionEditor.Cli.Tests | 42 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **1227** |

## Baseline delta / no-regression note (cite post-release-scope-boundary-2026-06-21.md + S48)

- S48 gate: 1227/1227 (S48 release packet)
- S49-01 baseline: **1227** (hold; no regression)
- Per boundary §Standing invariants: ≥1227 at S49 start; monotonic growth; never regress below post-S48 baseline.

## Commands executed (c-sharp-devops-engineer)

```bash
export DOTNET_ROOT=/home/username01/.dotnet ; export PATH=$DOTNET_ROOT:$PATH
cd /home/username01/cmano-clone/cmano-clone

# Build
dotnet build ProjectAegis.sln --no-restore -v q
# Output: Build succeeded. 0 Error(s). 5 Warning(s). Time Elapsed 00:00:04.99

# Full test
dotnet test ProjectAegis.sln --no-build -v minimal --logger "console;verbosity=minimal"
# ... Passed!  - Failed:     0, Passed:   279, ... Sim
# ... Passed!  - Failed:     0, Passed:    42, ... Cli
# ... Passed!  - Failed:     0, Passed:   246, ... Delegation
# ... Passed!  - Failed:     0, Passed:     5, ... Excel
# ... Passed!  - Failed:     0, Passed:   252, ... UnityAdapter
# ... Passed!  - Failed:     0, Passed:   403, ... Data
# Total: 1227 PASS

# Replay
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
# Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 175 ms

# Smoke
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --no-build -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
# Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18, Duration: 273 ms

# GitNexus
node .gitnexus/run.cjs status
# Repository: ... Indexed commit: 53426a3 Current commit: b5994c6 Status: ⚠️ stale
node .gitnexus/run.cjs detect_changes --scope unstaged
# Affected processes: 0 Risk level: low (doc changes)
```

**csharpexpert (.NET / determinism notes):** Per S48 + prior audits (0 CRIT/HIGH/MED); SeededRng pure; SimTickRunner fixed clock; no wall-clock/unordered in sim paths; Sort/Comparers; projections safe. Baseline confirms 1227 stable. No changes here.

## Unblocks

| Next | Owner | Dependency satisfied |
|------|-------|---------------------|
| S49-02 | team-qa | QA plan (Req 05/07 scope; cites this + boundary + gate matrix); blocks waves |
| S49-03+ | MCP/OSINT/Infra tracks | Baseline gates + matrix + boundary cite; GitNexus pre-flights per kickoff |

## Coordinator / verification-before-completion (S49-01 pattern)

- First actions: Full reads of `production/sprints/sprint-49-agentic-kickoff-mcp-osint-infra.md`, `production/agentic/sprint-49-parallel-kickoff-2026-06-21.md`, `production/post-release-scope-boundary-2026-06-21.md` (mandatory cite everywhere), S48 gate packet + sprint-48-release-gate.md, prior gate-matrix-track-b-2026-06-20.md, sprint-status.yaml (post-S48), AGENTS.md, determinism/replay notes, GitNexus.
- Cmds executed + cross-checks (re-runs of build/test/replay/smoke; git checks for bridge; GitNexus status/detect; re-reads of boundary, S48, sprint plans).
- Hard gates maintained (per post-release-scope-boundary-2026-06-21.md + S48): 1227 tests, 6/6 replay, 18/18 proxy, hash `17144800277401907079` pinned, ZERO DelegationBridge, extend-only CatalogWriteGate, GitNexus discipline (low on docs), boundary cited.
- GitNexus: no re-index (baseline pure doc; impact not triggered as no src/code edits per "prefer no code change").
- Gate matrix produced as part of S49-01 (this file). ACs: 0 errors; gate-matrix-post-release-2026-06-21.md created.

**S49-01 FORMALLY COMPLETE.** Baseline re-established. All gates PASS. Ready for S49-02 QA plan.

*Generated by S49-01 (c-sharp-devops-engineer baseline track). verification-before-completion applied (sequential reads of plans/boundary/S48 + cmds + re-checks + GitNexus + no assumptions). Cites `production/post-release-scope-boundary-2026-06-21.md` on every table/claim.*

---
**Next:** Dispatch S49-02. All future S49 artifacts cite boundary + this matrix/sprint-status updates. Maintain invariants. No deviation without ADR + boundary amendment.