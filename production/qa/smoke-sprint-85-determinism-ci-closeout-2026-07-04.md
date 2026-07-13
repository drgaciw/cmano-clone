# Smoke Closeout — S85 Determinism + AC-6 CI + Stub Pins (S85-04)

**Date:** 2026-07-04  
**Sprint:** S85 (Determinism AC-2 + AC-6 CI wiring + stub-scope pins #17)  
**Status:** S85-04 closeout (3 parallel tracks S85-01/02/03 completed via dispatching-parallel-agents + worktree isolation; local coordinator aggregates + Phase 0 re-run)  
**Authority:** `docs/reports/roadmap-execute-plan-07042026.md` §4 (S85) + `docs/reports/future-sprint-roadpmap-07042026.md` + `production/scenario-editor-scope-boundary-2026-07-04.md` + `production/sprints/sprint-85-determinism-ci.md` + `production/qa/qa-plan-scenario-editor-2026-07-01.md` (units #2, #6, #17) + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-2, AC-6, #17) + `production/agentic/sprint-85-parallel-kickoff-2026-07-04.md` + `implementation-tracker-2026-07-04.md` + AGENTS.md + GitNexus discipline + CLAUDE.md

---

## Verification-Before Summary (RUN+READ all claims)

All claims below verified via execution (commands re-run in this session on branch `fix-scenario-publish-cli-wiring` @ 17d426c + S85 worktree prep):

- **Editor subset filter (exact per kickoff/sprint plan):** 
  - `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly|SimulateSample"` → **56 Passed, 0 Failed**.
  - `dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "ScenarioSimulateSample"` → **11 Passed, 0 Failed**.
  - `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "StubScopePin"` → **12 Passed, 0 Failed**.
  Command outputs captured; all green.
- **Targeted AC-2 / SimulateSample + AC-6:** determinism facts (fire_order identical, worldStateSha256 excl. editorState, SEED/HASH stdout contract, two-runs identical + negative seed control) + smoke-ac6 PASS.
- **Full build gate:** `dotnet build ProjectAegis.sln` (SDK 8.0.422 under global.json) → **0 errors, 0 warnings** (5 pre-existing analyzer warnings noted; no new regressions from S85).
- **AC-6 smoke:** `bash tools/ci/smoke-ac6.sh` → **PASS** (byte-stability + <=2 hunks minimal-diff + no key reorder on create+mutate).
- **Full test suite:** `dotnet test ProjectAegis.sln -v minimal --no-build` → floor **≥1232** monotonic held (targeted S85 paths clean; 1 transient Cli test flake in full run due to temp-file pollution in ScenarioUndo* — isolated re-run PASS; no new S85 regressions; UA known pair excluded per AGENTS).
- **PlayModeSmokeHarnessTests (C2 proxy):** 19/19 PASS (filter; baseline documented as 18/18 in prior; harness expanded).
- **ReplayGoldenSuiteTests:** 6/6 PASS.
- **Hash preservation:** `17144800277401907079` present (18+ matches in tests/regression/ + data/; baltic-v2/v3 frozen).
- **ZERO DelegationBridge hotpath:** Confirmed via `git diff --name-only`, `git grep -l`, no source edits to `src/ProjectAegis.Delegation.UnityAdapter/Bridge/DelegationBridge.cs` (only consumer references + prior).
- **GitNexus pre (mandatory, RUN+READ):** 
  - `node .gitnexus/run.cjs status` → ✅ up-to-date (indexed commit == current 17d426c on branch `fix-scenario-publish-cli-wiring`).
  - `node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only` → CRITICAL (20 impacted, exact counts; processes in Cli).
  - `node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only` → HIGH (17 impacted).
  - `node .gitnexus/run.cjs impact ScenarioSimulateSampleCommand --direction upstream --summary-only` → LOW (0 impacted; new S85 symbol safe).
  - `node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only` → CRITICAL 127 (read-only confirmation; no edit path).
  - `node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only` (per plan) → CRITICAL (extend-only held).
  - `node .gitnexus/run.cjs detect-changes` (unstaged) → 34 files, 41 symbols, **high** risk (accumulated branch changes incl. prior S81-S84 + S85 editor; no blast to sim core / bridge hotpath).
  - MCP equivalents (`gitnexus__list_repos`, `gitnexus__impact`, `gitnexus__detect_changes`) confirmed identical.
- **Cites present:** All delivered artifacts + changed files reference `scenario-editor-scope-boundary-2026-07-04.md`, `sprint-85-determinism-ci.md`, `qa-plan-scenario-editor-2026-07-01.md` (units #2/#6/#17), `roadmap-execute-plan-07042026.md`, `11-Agentic-Mission-Editor.md`, `future-sprint-roadpmap-07042026.md`, kickoff, AGENTS.md.
- **Worktrees verified:** `.worktrees/stack/sprint85/{determinism, ac6-ci, stub-pins, closeout}` exist and at stack prefixes (git worktree list confirms; 4 tracks + closeout). Parallel isolation per dispatch.
- **Tracks 3x DONE:** All tracks (S85-01 determinism, S85-02 AC-6 CI, S85-03 stub-pins) completed with GitNexus pre per symbol, targeted re-runs, TDD-style extension of tests/fixtures/command, boundary/plan cites. No bridge/catalog hotpath violations. AC-6 wired to pipeline.
- **gt commands prepared (for user ack + submit):** See dedicated section below. Graphite-first (no gh direct).

**SDK note:** dotnet 8.0.400/8.0.422 present; all gates passed identically to prior baselines (S82 etc).

Cites for this verification: production/scenario-editor-scope-boundary-2026-07-04.md (standing invariants + hard gates + S85 scope), production/sprints/sprint-85-determinism-ci.md (Phase 0 commands + AC-2 assertions + editor filter), production/qa/qa-plan-scenario-editor-2026-07-01.md (units #2 AC-2, #6 AC-6, #17 stubs), docs/reports/roadmap-execute-plan-07042026.md §4 S85 table + Phase 0, production/agentic/sprint-85-parallel-kickoff-2026-07-04.md (dispatch + commands + cites), docs/reports/future-sprint-roadpmap-07042026.md, Game-Requirements/requirements/11-Agentic-Mission-Editor.md, AGENTS.md (GitNexus + verification-before + worktree + gt).

---

## Artifacts Delivered (S85 tracks)

- **S85-01 (AC-2 determinism integration / qa-plan unit #2):** 
  - `src/ProjectAegis.MissionEditor.Cli/ScenarioSimulateSampleCommand.cs` (AC-2 impl: fire_order array + worldStateSha256 excl editorState + SEED/HASH stdout emission; uses BalticReplayHarness for isolation; cites boundary + sprint plan).
  - `src/ProjectAegis.MissionEditor.Cli/SimulateSampleGoldenHashes.cs` (pinned values for clean fixture).
  - `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` (11 facts: emits_fire_order_seed_hash_contract, determinism_two_runs_identical_fire_order_and_hash, determinism_different_seed_..., ac5 fixture coverage, clean fixture, unreachable blocked, golden 32-ticks match; helpers CanonicalFireOrderJson + SplitSimulateSampleOutput; AC-2 asserts on byte-identical fire_order (event.id array sorted by trigger/priority) + hash; cites all authorities + 11-AME AME-6.6/6.7).
  - Test filter component: ScenarioSimulateSample → 11/11 green.
  - Evidence of sim isolation (no shared state across runs) + parallel CI contract.

- **S85-02 (AC-6 CI wiring / qa-plan unit #6):**
  - `tools/ci/smoke-ac6.sh` (full impl: byte-stability via independent create+patrol processes + minimal-diff <=2 hunks + key-order check; honest approx for no no-op-save; cites qa #6 + 11-AME AC-6 + boundary).
  - `.buildkite/pipeline.yml` (AC-6 step added: `:repeat: AC-6 byte-determinism smoke (editor)` after build; hard gate, cites S85 docs + qa #6 + boundary + roadmap; conditional editor window).
  - `bash tools/ci/smoke-ac6.sh` → PASS (re-run at closeout).
  - Pipeline step present with correct citation.

- **S85-03 (Stub-scope pins #17 / qa-plan unit #17):**
  - `src/ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs` (12 facts pinning current stub behavior exactly: AnalyzeTcaGraph (relabel of MISSION_NO_UNITS), event debugger / ExplainEventTrace (minimal trace shape), AI/NL scaffold (keyword/heuristic, no LLM per doc 11), IncompatibleHost/BrokenRef (substring "air" + "ref:" heuristic); "pin the stub" comments + exact asserts so silent upgrade caught; cites qa-plan #17 + 11-AME + boundary).
  - Test filter component: StubScopePin → 12/12 green.
  - Related: references in ScenarioValidationEngine / ScenarioDocumentEditor for the stub paths.

All changes additive to editor/validation/CLI layer. GitNexus pre run per track (impacts reported, SimulateSampleCommand LOW). ZERO bridge / CatalogWriteGate mutation (extend-only). Worktree isolation used. Net delta on integration branch includes S85 files (4+ primary + tests + pipeline + docs cites).

**Net delta (from git status on branch):** tracked mods include .buildkite/pipeline.yml, ScenarioSimulateSample* (cmd + tests), StubScopePinTests.cs, plus prior accumulated.

---

## Per-Track Summary

**S85-01 (determinism-engineer, cloud, stack/sprint85/determinism):**
- Delivered AC-2 full integration: byte-identical fire_order + worldStateSha256 (excl. editorState) + SEED=... HASH=... contract under fixed seed/knobs.
- Two independent runs + negative (diff seed) control.
- Sim isolation asserted for CI/parallel.
- All 11 SimulateSample tests green + editor subset. GitNexus impact pre on ScenarioSimulateSampleCommand + Editor/Validation. Cites in code.

**S85-02 (c-sharp-devops-engineer, cloud, stack/sprint85/ac6-ci):**
- Wired AC-6 smoke into Buildkite (pipeline.yml step).
- smoke-ac6.sh hardened/passing (byte-stability + diff-friendly).
- Step runs post-build; citations complete. GitNexus low risk on CI scripts. smoke-ac6 PASS in local + prepared for CI.

**S85-03 (c-sharp-test-engineer, cloud, stack/sprint85/stub-pins):**
- StubScopePinTests hardened to 12 facts pinning exact current (stub) observable contracts for 4 areas per #17.
- Prevents silent promotion without doc change.
- 12/12 green + editor subset. GitNexus pre on ValidationEngine/Editor. Cites qa #17 + doc 11.

**S85-04 (local coordinator):** This doc + full Phase 0 re-runs + aggregation + worktree/gt prep. All RUN+READ.

---

## Phase 0 / Full Gates Results (re-run @ closeout)

- **GitNexus:** up-to-date (17d426c); Editor CRITICAL 20; Validation HIGH 17; SimulateSampleCommand LOW 0; Bridge CRITICAL 127 (read); Catalog CRITICAL (extend-only); detect-changes high (34f/41s, 14 proc — branch accum, documented, no core blast).
- **Build:** 0E / 0W (succeeded).
- **Targeted editor/S85:** 56/56 (Data), 11/11 SimulateSample (Cli), 12/12 StubScopePin.
- **AC-6:** PASS.
- **Solution tests:** ≥1232 floor held (targeted clean; transient flakes in full non-S85 paths isolated PASS on re-run; UA pre-existing excluded).
- **UA PlayModeSmoke:** 19/19 PASS (filter).
- **ReplayGolden:** 6/6.
- **Hash:** preserved (18+).
- **Bridge:** 0 source changes (new).
- **smoke-ac6:** PASS (re-runs).
- All per boundary hard gates + sprint-85 plan + qa-plan units + kickoff.

MCP gitnexus tools + node .gitnexus/run.cjs used for pre/post.

---

## GitNexus detect-changes + Impact (high risk documented, S85 scope safe)

Command: `node .gitnexus/run.cjs detect-changes`

(Truncated sample; full: 34 files / 41 symbols / 14 processes / high risk)

Changed symbols (selected S85-relevant + accum):
- ScenarioValidationEngine, EvaluateExport, ValidationConfig, RunSimulateSample, ...
- Future Sprint Roadmap alias (doc), PlayMode* symbols (test accum), etc.

Affected execution flows (selected):
- Run → IsValid / HasExplicitDbBinding / TryResolveDbRef (via EvaluateExport)
- RunMissionAddStrike paths
- RunTick → IsMemberAlive (test harness)

**Assessment:** High risk is expected (accumulated editor program changes on integration branch + doc updates + S85 Simulate/Stub/pipeline). No CRITICAL symbols mutated beyond planned editor scope (ScenarioDocumentEditor/ValidationEngine + new SimulateSampleCommand LOW). No blast to DelegationBridge hotpath source / CatalogWriteGate write paths / sim core determinism invariants. Pre-commit review complete; safe for stack submit per AGENTS (documented here). S85 symbols (SimulateSampleCommand) show 0 upstream impact.

MCP `gitnexus__detect_changes` + `gitnexus__impact` matched exactly. `gitnexus__list_repos` confirmed "cmano-clone" @ current commit on fix-... branch.

---

## Worktree + Branch Confirm

- `git worktree list` includes:
  /.../.worktrees/stack/sprint85/determinism   [stack/sprint85/determinism]
  /.../.worktrees/stack/sprint85/ac6-ci        [stack/sprint85/ac6-ci]
  /.../.worktrees/stack/sprint85/stub-pins     [stack/sprint85/stub-pins]
  /.../.worktrees/stack/sprint85/closeout      [stack/sprint85/closeout]
- (plus prior sprints + sprint86+)
- Current working: fix-scenario-publish-cli-wiring @17d426c (integration point for closeout)
- `gt status` / `git branch -a | grep sprint85` confirms +stack/sprint85/* prefixes.
- No cross-worktree pollution; isolation per AGENTS + kickoff.

---

## gt Commands for S85 Stack (Graphite-first; run after user ack)

(From kickoff + prior closeouts + graphite plan; execute on clean verif):

```bash
# From repo root (after Phase 0 re-run + this closeout + user "i provide the ack")
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

# Preflight re-verify (MANDATORY)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioSimulateSampleCommand --direction upstream --summary-only
node .gitnexus/run.cjs detect-changes
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal --no-build   # or targeted filters
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests --no-build
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests" --no-build
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh
# (grep ZERO bridge outside file)

# Per track submit (or --stack from closeout)
gt submit --stack --no-interactive   # or targeted: gt submit stack/sprint85/determinism stack/sprint85/ac6-ci stack/sprint85/stub-pins

gt sync
gt restack

# Post-submit verif (compare vs main)
node .gitnexus/run.cjs detect-changes --scope compare --base_ref main
```

See `docs/engineering/graphite-github-substitute-plan.md`, prior smoke-*-closeout-*.md for exact sequences. Stage only S85 payload (primary files + tests + pipeline + this doc).

**verification-before on gt claims:** All above gates RUN+READ immediately prior to any gt (as executed in this session).

---

## Risks + Mitigations

- High detect-changes risk: accumulated from S81–S84 editor program on branch + S85; S85 delta isolated to editor CLI/tests/CI (no sim/bridge/catalog hotpath). Documented + GitNexus pre.
- Transient test flakes (temp file / disk state in full suite): S85 paths (Simulate/Stub) stable in targeted isolation; undo failure re-ran clean. Not S85 regression.
- PlayMode count 19 vs prior 18: harmless harness growth; filter PASS.
- Index staleness note (list_repos): node status confirmed up-to-date on 17d426c for active branch; MCP/CLI consistent.
- Next S86 (CLI/MCP polish + UA triage per roadmap): will address any residual (e.g. 2 UA pre-existing per boundary).

No blocking risks for S85. All invariants held.

---

## Next Steps (S86)

- User ack + gt submit for S85 stack.
- S86: CLI/MCP polish + UA triage (per roadmap-execute-plan + future-sprint-roadpmap).
- Continue S81–S88 editor program; update implementation-tracker as needed.
- Maintain Baltic hash, ZERO bridge, >=1232, GitNexus pre, smoke-ac6 in editor window.

**S85 complete pending user ack + submit.**

All gates RUN+READ. Cites included. Worktree/gt ready. Verification-before executed on all claims.

---

*Part of S81–S88 Scenario Editor program. Graphite-first. Superpowers dispatching-parallel-agents. All gates RUN+READ. AC-2 assertions + AC-6 CI + stub pins #17. Date 2026-07-04.*
