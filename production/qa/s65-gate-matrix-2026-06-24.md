# S65 Gate Matrix Refresh — Post-S64 Baseline (Release Train S65-02)

**Date:** 2026-06-24  
**Sprint:** 65 — Release train foundation (E10 Lead)  
**Story/Task:** S65-02 (Gate matrix refresh; post-S64 baseline per execute plan)  
**Track:** Gate matrix (Cloud agent; isolated; per roadmap-execute-plan-062426 §4 / §8 Agent B)  
**Worktree / Env:** Cloud (doc-only track)  
**Authority / Citations (mandatory):**  
- [`production/release-train-scope-boundary-2026-06-24.md`](../release-train-scope-boundary-2026-06-24.md) (PUBLISHED; supersedes baltic-v2 for S65+; standing invariants; §Standing Invariants)  
- [`docs/reports/roadmap-execute-plan-062426.md`](../../docs/reports/roadmap-execute-plan-062426.md) §4 (S65 tracks), §6 (Hard gates + exact commands), §7 (File ownership), §8 (Agent B prompt: "Refresh post-S64 gate matrix... Run gates fresh; READ full output before claims"), §9 (prereqs + verification)  
- [`docs/reports/future-sprint-roadpmap-062426.md`](../../docs/reports/future-sprint-roadpmap-062426.md) §7 (Standing invariants), §10 (S65-02 Gate matrix), §0.4 (merge gate), §0.6 (pre-flight)  
- Post-S64 closeout: [`production/qa/s57-s64-program-closeout-2026-06-22.md`](../s57-s64-program-closeout-2026-06-22.md); prior gate matrices (baltic-v2, post-release) for format precedent.  
- AGENTS.md / CLAUDE.md (verification-before on claims; docs-only for this track; no GitNexus required per task).  

> **Every S65+ artifact MUST cite `production/release-train-scope-boundary-2026-06-24.md` + roadmap-062426.md §7 + execute-plan-062426 §6 (per boundary §9 and roadmap §10). This matrix produced by isolated S65-02 track.**

**Scope citation:** S65 E10 foundation (boundary ∥ gate matrix ∥ manifest ∥ re-index). Post-S64 baseline: ≥1229 tests (monotonic; never regress), ReplayGolden **6/6**, C2 proxy **18/18**, production Baltic hash **`17144800277401907079`** immutable, DelegationBridge **ZERO** touch, CatalogWriteGate **extend-only**, GitNexus discipline. **S65-02 produces `production/qa/s65-gate-matrix-2026-06-24.md`.** Docs only (no code unless TDD test fix; none required).

## Verdict: **PASS** (0 errors; baseline gates held at post-S64 floor 1229; S65-02 ACs met per execute plan; verification-before applied on all claims)

## Hard Gates Matrix (Post-S64 / S65 baseline)

All standing invariants from `production/release-train-scope-boundary-2026-06-24.md` §Standing Invariants (S65+) and `docs/reports/future-sprint-roadpmap-062426.md` §7 enforced. Exact commands from execute-plan §6 used. **Verification-before-completion applied on all RUN+READ outputs** (full logs captured + inspected before claims). Fresh runs executed 2026-06-24 in this session. No code changes (pure doc refresh track).

| Gate | Floor / Policy | Status (2026-06-24) | Evidence / Command Output |
|------|----------------|---------------------|---------------------------|
| Full headless tests (sln) | **≥1229** (post-S64 floor; monotonic; never regress) | **PASS — 1229/1229** (0f) (Data 403 + Sim 279 + Delegation 247 + UA 252 + Cli 43 + Excel 5) | `dotnet test ProjectAegis.sln -v minimal` → 0 failed across all projects. Full output read from /tmp/full-test-s65.log (RUN+READ). |
| ReplayGoldenSuiteTests | **6/6** every sprint; hash preservation | **PASS — 6/6** (166 ms) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"` → Passed! 6/6. Baltic hash `17144800277401907079` preserved (confirmed in goldens + runs). Full output read from /tmp/replay-s65.log. |
| PlayModeSmokeHarness (C2 proxy) | **18/18+** | **PASS — 18/18** (263 ms) | `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"` → Passed! 18/18. Full output read from /tmp/c2-s65.log. |
| dotnet build | 0 errors | **PASS — 0 Error(s) 0 Warning(s)** (0w0e) | `dotnet build ProjectAegis.sln --no-restore -v q` → "Build succeeded. 0 Error(s). 0 Warning(s)". Full output read from /tmp/build-s65.log + prior run. |
| Baltic hash | **`17144800277401907079`** immutable (unless ADR) | **PASS — unchanged** | Confirmed via grep in tests/regression/replay-golden-baltic-*.txt (17+ occurrences) + goldens + prior closeouts. Hash preserved post-S64. |
| DelegationBridge | **ZERO touch** default — ADR required | **PASS — ZERO** (this track) | `git status --porcelain` + find + log inspection: no edits to DelegationBridge.cs or tests in current session (doc-only track; pre-existing files untouched). Invariant held. |
| CatalogWriteGate / IWriteGate | **extend-only** default | **PASS** (no edits) | This track: doc-only. No touch. (GitNexus not required for doc track per task scope.) |
| Production invariants held | Per release-train-scope-boundary-2026-06-24.md + roadmap-062426 §7 | **PASS** | Tests 1229/0f; Replay 6/6; proxy 18/18; hash pinned; ZERO bridge (this track); all RUN+READ verification-before; boundary + roadmap cites. |

**Verification-before-completion outputs (key gates, captured + fully read pre-claim 2026-06-24):**
- Build: succeeded (0e 0w). Log: /tmp/build-s65.log
- Tests full: 1229 PASS (0 fail). Summary: Sim 279, Del 247, Excel 5, Cli 43, UA 252, Data 403. Log: /tmp/full-test-s65.log (60+ lines inspected; grep for Passed/Failed confirmed 0f).
- Replay filter: 6/6. Log: /tmp/replay-s65.log (full cat read).
- C2 proxy filter: 18/18. Log: /tmp/c2-s65.log (full cat read).
- Hash: preserved (grep read).
- Bridge: clean (git + find read).
- Exact command outputs embedded; all executed from repo root with PATH export.
- Determinism / no regression: matches S57–S64 closeout + roadmap post-S64 baseline.

## Per-project counts (S65-02 fresh baseline run @ post-S64)

| Project | Passed |
|---------|--------|
| ProjectAegis.Data.Tests | 403 |
| ProjectAegis.Sim.Tests | 279 |
| ProjectAegis.Delegation.Tests | 247 |
| ProjectAegis.Delegation.UnityAdapter.Tests | 252 |
| ProjectAegis.MissionEditor.Cli.Tests | 43 |
| ProjectAegis.Data.Excel.Tests | 5 |
| **Total** | **1229** |

## Baseline delta / no-regression note (cite release-train-scope-boundary-2026-06-24.md + s57-s64 closeout + roadmap §7)

- S57–S64 closeout / post-S64: **1229/1229** (0f) floor established + human ack.
- S65-02 refresh: **1229** (hold; no regression; monotonic).
- Per boundary §Standing Invariants and roadmap §7: floor ≥1229; Replay 6/6; C2 18/18; hash immutable; ZERO bridge; extend-only WriteGate; cites required; verification-before on claims.

## Commands executed (exact from roadmap-execute-plan-062426.md §6; repo root)

```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

# Build (execute-plan §6)
dotnet build ProjectAegis.sln --no-restore -v q
# Output (full read): Build succeeded. 0 Warning(s) 0 Error(s). Time Elapsed 00:00:01.82 (and prior identical run)

# Full test (execute-plan §6)
dotnet test ProjectAegis.sln -v minimal
# Output (full read from /tmp/full-test-s65.log + live):
# ... Passed!  - Failed:     0, Passed:   279 ... Sim.Tests
# ... Passed!  - Failed:     0, Passed:   247 ... Delegation.Tests
# ... Passed!  - Failed:     0, Passed:     5 ... Data.Excel.Tests
# ... Passed!  - Failed:     0, Passed:    43 ... MissionEditor.Cli.Tests
# ... Passed!  - Failed:     0, Passed:   252 ... Delegation.UnityAdapter.Tests
# ... Passed!  - Failed:     0, Passed:   403 ... Data.Tests
# Total: 1229 PASS 0f (monotonic hold)

# Replay (execute-plan §6)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
# Output (full read): Passed!  - Failed:     0, Passed:     6 ... Duration: 166 ms

# C2 proxy (execute-plan §6)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -v minimal --filter "FullyQualifiedName~PlayModeSmokeHarnessTests"
# Output (full read): Passed!  - Failed:     0, Passed:    18 ... Duration: 263 ms
```

**Additional verification reads (pre-claim):**
- `dotnet --version`: 8.0.422 (PATH exported)
- Hash grep: confirmed in goldens (e.g. baltic-engage, baltic-destroyed-reengage, v2-*.txt)
- Bridge invariant: `find src -name "*DelegationBridge*"` (pre-existing only; no session mods to src)
- Full logs: /tmp/build-s65.log, /tmp/full-test-s65.log, /tmp/replay-s65.log, /tmp/c2-s65.log (all cat/tail/grep inspected)

**csharpexpert (.NET / determinism notes):** Per S57–S64 + roadmap (seeded RNG, fixed-tick sim, deterministic projections). Fresh baseline confirms 1229 stable. No behavior changes in this doc track. Hash + filters held.

## Unblocks / Next (per execute-plan §4 / §9)

| Next | Owner | Dependency satisfied |
|------|-------|---------------------|
| S65 manifest track | team-data | Gate matrix baseline + boundary + commands cited |
| S65 closeout / full S65 gates re-run | c-sharp-devops-engineer (local) | This matrix + replay/C2 6/6+18/18 |
| S66 evidence | qa-lead | S65 gate matrix (per roadmap §10) |

## References / Related

- Hard gates table + commands: roadmap-execute-plan-062426.md §6
- S65 decomposition: future-sprint-roadpmap-062426.md §10
- Post-S64 baseline: s57-s64-program-closeout-2026-06-22.md + sprint-65-stub + sprint-status.yaml
- Prior matrices: production/qa/gate-matrix-baltic-v2-2026-06-22.md ; gate-matrix-post-release-2026-06-21.md

**All claims RUN+READ verified before inclusion. Self-contained doc track. No shared edits.**

*Generated 2026-06-24 by Cloud agent S65-02 per roadmap/execute-plan §8 Agent B. Cite release-train-scope-boundary-2026-06-24.md + future-sprint-roadpmap-062426.md §7 on all follow-on artifacts.*
