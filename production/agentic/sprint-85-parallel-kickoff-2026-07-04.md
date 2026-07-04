# Sprint 85 Parallel Kickoff — Determinism + AC-6 CI + stub pins

**Date:** 2026-07-04 (post S84)  
**Sprint:** S85  
**Authority:** `roadmap-execute-plan-07042026.md` §4 (S85), `scenario-editor-scope-boundary-2026-07-04.md`, `future-sprint-roadpmap-07042026.md`, `qa-plan-scenario-editor-2026-07-01.md` (units #2, #6, #17), `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-2, AC-6, #17), S81–S84 parallel kickoffs + sprint plans + closeouts, AGENTS.md + CLAUDE.md

## Context
S84 (Event debugger + teleport + ADR-016) complete. In-flight editor work integrated or ready.

**S85 focuses on Determinism + AC-6 CI + stub pins (3 parallel cloud tracks + local closeout):**

1. AC-2 determinism integration (S85-01) — determinism-engineer
2. AC-6 CI wiring (S85-02) — c-sharp-devops-engineer
3. Stub-scope pins (#17) (S85-03) — c-sharp-test-engineer
4. Closeout S85-04 (local)

All tracks cite boundary + sprint plan + execute-plan + qa units. Primary: `ScenarioSimulateSampleCommand.cs`, `ScenarioSimulateSampleCliTests.cs`, `tools/ci/smoke-ac6.sh`, `.buildkite/pipeline.yml`, `StubScopePinTests.cs`.

**AC-2 assertions (explicit):** byte-identical `fire_order`; world-state hash excluding `editorState`; `SEED=<v> HASH=<sha256>` stdout contract. Holds under parallel CI (sim isolation).

## Dispatch Rules (superpowers)
- Use `dispatching-parallel-agents` skill for the three tracks (S85-01∥S85-02∥S85-03).
- Each subagent: **isolated context**, GitNexus preflight on editor symbols + CRITICALs (see gates), cite all authorities, TDD where extending (RED→GREEN on new/extended tests), verification-before-completion on all PASS claims.
- Work exclusively in `.worktrees/stack/sprint85/<track>` (Graphite stack prefixes per execute-plan).
- Prefer extending existing (`ScenarioSimulateSampleCliTests.cs` determinism methods, `StubScopePinTests.cs`, smoke-ac6.sh) or adding pipeline step.
- Run targeted Data.Tests / Cli.Tests filters + `bash tools/ci/smoke-ac6.sh` before/after every change.
- No DelegationBridge, no CatalogWriteGate mutation, no frozen Baltic goldens.
- Stage remains Release.
- Single-owner coordination if ScenarioSimulateSampleCommand or Validation paths shared.

## Track Details

**S85-01 AC-2 determinism integration (Cloud, determinism-engineer)**
- Primary files: `ScenarioSimulateSampleCommand.cs`, `ScenarioSimulateSampleCliTests.cs`, `SimulateSampleGoldenHashes.cs`, related (ScenarioExportCommand, BalticReplayHarness read paths).
- Goal: Full AC-2 integration + coverage. Ensure two independent runs (same seed + file + knobs) yield byte-identical `fire_order` array + identical `worldStateSha256` (SHA-256 over post-run canonical excluding `editorState`). Both emit `SEED=... HASH=...` exactly. Add/expand asserts for non-empty fire_order (events that fire), empty, never-fire cases, negative different-seed control. Confirm sim isolation (no shared state) under parallel.
- Test filter component: SimulateSample
- Evidence: `scenario_simulate_sample_determinism_two_runs_identical_fire_order_and_hash` (extend), `emits_fire_order_seed_hash_contract` (extend), new facts for firing events + parallel runner parity.
- GitNexus: impact ScenarioDocumentEditor + ScenarioValidationEngine + ScenarioSimulateSampleCommand --direction upstream --summary-only. Report callers (Cli, Data, Sim paths).
- Output: green determinism tests + CLI `scenario_simulate_sample --path <file> --ticks N` producing stable fire_order + SEED/HASH contract.

**S85-02 AC-6 CI wiring (Cloud, c-sharp-devops-engineer)**
- Primary files: `tools/ci/smoke-ac6.sh` (review/harden), `.buildkite/pipeline.yml` (add step), `tools/buildkite/agent-dotnet-ci.sh` / related agents if needed, docs references.
- Goal: Wire AC-6 smoke into CI. Add labeled step (e.g. ":repeat: AC-6 byte-determinism smoke (editor)") that runs `bash tools/ci/smoke-ac6.sh` (post-build or dedicated; conditional on editor paths or always during S81–S88 program). Ensure PASS in pipeline context; update preflight / agent scripts for parity with local. Verify no regression to other steps (build, replay, gitnexus).
- Test / evidence: smoke-ac6 PASS output in CI log; step present in pipeline.yml with correct citation.
- GitNexus: impact on any touched CI scripts (low risk).
- Output: pipeline step wired + smoke-ac6 green in Buildkite context + update to sprint closeout / implementation tracker notes.

**S85-03 Stub-scope pins (#17) (Cloud, c-sharp-test-engineer)**
- Primary files: `src/ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs` (extend/harden 4 pins), related stubs (ExplainEventTrace paths if wired, AiAuthoringServices, ScenarioValidationEngine for Incompatible/BrokenRef, AnalyzeTcaGraph).
- Goal: Ensure all 4 stub pins from qa-plan #17 are covered and pinned:
  - Event debugger (`scenario_event_trace` / ExplainEventTrace): current minimal trace shape (documented stub, not full AC-7 projection).
  - TCA static-analysis: dead/unreachable etc. is relabel of existing findings + trivial chain (not real graph analysis).
  - AI scaffold/NL (`NlScaffold` / mission_plan_suggest): keyword/heuristic only, not LLM (per doc 11 "no LLM in any blocking path").
  - IncompatibleHost/BrokenRef: substring "air" + "ref:" prefix convention (naive heuristic).
- Assert *exact current output strings / behavior* so silent promotion of stub is caught.
- Test filter component: StubScopePin
- Evidence: 4+ facts (or expansion) with "pin the stub" comments citing qa-plan #17 + doc 11.
- GitNexus: impact ScenarioValidationEngine + ScenarioDocumentEditor.
- Output: green StubScopePin filter; any behavior change is explicit + documented.

**S85-04 Closeout (local)**
- Aggregate evidence from all tracks.
- `gt submit --stack --no-interactive` on each; `gt sync`; `gt restack` on main.
- Re-run full Phase 0 gates (see sprint plan) + editor subset filter + smoke-ac6.
- GitNexus post: `detect_changes` (compare vs main), re-index if needed, report stats + impacts (ScenarioValidationEngine, SimulateSample paths).
- Produce `production/qa/smoke-sprint-85-closeout-2026-07-*.md`
- Update `production/sprint-status.yaml`
- Record: test deltas (esp. SimulateSample / StubScope), GitNexus exact numbers, no unexpected blast radius, AC-2/AC-6 evidence.
- Verification: all gates RUN+READ before close claim.

## Commands (run in every track + closeout)
```bash
# Preflight (MANDATORY per AGENTS + execute-plan)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioSimulateSampleCommand --direction upstream --summary-only || true

# Targeted
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
  --filter "ScenarioSimulateSample"
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "StubScopePin"

# Editor subset (full relevant)
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
  --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly|SimulateSample"

# AC-6 (required this sprint)
bash tools/ci/smoke-ac6.sh

# Full baseline gates (Phase 0 — RUN+READ)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh
```

## Standing Invariants & Exclusions
- Test floor ≥1232 (0 failures excl. known UA pair until S86)
- ReplayGolden 6/6; C2 proxy 18/18
- Hash `17144800277401907079` preserved
- ZERO DelegationBridge (grep `DelegationBridge` in src/ outside the bridge file == 0 new)
- Catalog extend-only
- Stage: Release
- GitNexus discipline on all edits (impact before symbol change)
- Frozen: baltic-v* corpora/goldens; use `data/scenarios/examples/`
- Never commit: `.cursor/hooks/`, `.pi/settings.json`, `.polly/`
- AC-6 smoke run on every relevant change + in CI

## Cites (include in every commit message / doc / PR description)
`roadmap-execute-plan-07042026.md` §4 S85 + `future-sprint-roadpmap-07042026.md` + `scenario-editor-scope-boundary-2026-07-04.md` + `qa-plan-scenario-editor-2026-07-01.md` (units #2,#6,#17) + `implementation-tracker-2026-07-04.md` + `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-2, AC-6, #17) + AGENTS.md + this kickoff + `production/sprints/sprint-85-determinism-ci.md`

## GitNexus Pre (per AGENTS.md — always before edit)
Run `impact({target: "...", direction: "upstream"})` and report blast radius (direct callers, affected processes, risk HIGH/CRITICAL). Use `context()` for full callers/callees/flows. `detect_changes()` before any commit.

Current baseline (from execute-plan): ScenarioValidationEngine HIGH (17 upstream); ScenarioDocumentEditor CRITICAL (20). Recompute on dispatch for SimulateSample / Stub paths. Report exact numbers in track output.

## Ready for Dispatch
Kickoff complete. Boundary + S81–S84 assumed complete + user ack received.

**Dispatch the three tracks in parallel now** (S85-01, S85-02, S85-03) using subagent-driven-development + worktree isolation.

Local coordinator owns S85-04 closeout + final verif + human gates.

**Verification required before any track claims complete (RUN+READ):**
- Targeted SimulateSample + StubScopePin filters
- Editor subset
- `bash tools/ci/smoke-ac6.sh`
- Full Phase 0 gates (build/test/replay/C2/hash/bridge-zero/GitNexus)
- GitNexus impacts + detect_changes

**Next after close:** S86 CLI/MCP polish + UA triage.

---
*Part of S81–S88 Scenario Editor program. Graphite-first. Superpowers dispatching-parallel-agents. All gates RUN+READ. AC-2 assertions + AC-6 CI + stub pins #17.*
