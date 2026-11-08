# Sprint 85 — Determinism + AC-6 CI + stub pins

**Dates:** Following S84 (est. 5–7 days)  
**Lead:** E11 / determinism-engineer + c-sharp-devops-engineer + c-sharp-test-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S85), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md` (units #2, #6, #17), `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-2, AC-6, #17 stub-scope pins), AGENTS.md + GitNexus discipline

## Sprint Goal
Complete S85 tracks: AC-2 determinism integration (byte-identical `fire_order`; world-state hash excluding `editorState`; `SEED=/HASH=` stdout contract in `scenario_simulate_sample`), AC-6 CI wiring (integrate `tools/ci/smoke-ac6.sh` into `.buildkite/pipeline.yml` and related agents), Stub-scope pins (#17: harden `StubScopePinTests` for event debugger / TCA / AI scaffold / IncompatibleHost/BrokenRef). Advance primary files `ScenarioSimulateSampleCommand*`, `smoke-ac6.sh`, `StubScopePinTests.cs`, CI pipeline. Maintain all standing invariants (≥1232 floor, ReplayGolden 6/6, C2 18/18, Baltic hash, ZERO DelegationBridge, GitNexus pre).

## Tracks (parallel day 1)

| Track | Stack prefix | Worktree path | Agent env | Owner |
|-------|--------------|---------------|-----------|-------|
| AC-2 determinism integration | `stack/sprint85/determinism` | `.worktrees/stack/sprint85/determinism` | Cloud | determinism-engineer |
| AC-6 CI wiring | `stack/sprint85/ac6-ci` | `.worktrees/stack/sprint85/ac6-ci` | Cloud | c-sharp-devops-engineer |
| Stub-scope pins (#17) | `stack/sprint85/stub-pins` | `.worktrees/stack/sprint85/stub-pins` | Cloud | c-sharp-test-engineer |
| Closeout | `stack/sprint85/closeout` | `.worktrees/stack/sprint85/closeout` | **Local** | c-sharp-devops-engineer |

**Wave order:** Parallel AC-2 + AC-6 + stub-pins (coordinate if `ScenarioSimulateSample*` or Validation shared) → closeout

**Primary files (per execute-plan §4 S85):**

| File | QA unit |
|------|---------|
| `src/ProjectAegis.MissionEditor.Cli/ScenarioSimulateSampleCommand.cs` | #2 |
| `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` | #2 |
| `tools/ci/smoke-ac6.sh` | #6 |
| `.buildkite/pipeline.yml` (or CI step referencing smoke-ac6) | #6 |
| `src/ProjectAegis.Data.Tests/Scenario/StubScopePinTests.cs` | #17 |

**AC-2 assertions (per execute-plan + qa-plan #2 + 11-Agentic-Mission-Editor.md AME-6.6/6.7):**
- Byte-identical `fire_order` (ordered array of `event.id` strings; sort key `(trigger_time_resolved, priority, event.id)`).
- Identical world-state hash = SHA-256 over canonical post-run world state **excluding `editorState`**.
- Both runs emit `SEED=<v> HASH=<sha256>` stdout contract.
- Holds under parallel CI runners (sim isolation in `ScenarioSimulateSampleCommand` + `BalticReplayHarness`; no shared event queue / variables).

See `ScenarioSimulateSampleCommand.Run` (emits via `SimulateSampleJsonDto.fire_order` + `WorldStateSha256` + `SEED=/HASH=`), `ScenarioSimulateSampleCliTests` (existing `scenario_simulate_sample_determinism_two_runs...` + `emits_fire_order_seed_hash_contract`), `SimulateSampleGoldenHashes`.

## AME ACs & QA Units
- **AC-2 (Logic → AME-6.6, AME-6.7):** Given fixed `metadata.seed` + identical knobs, two independent `scenario_simulate_sample` runs produce (a) byte-identical `fire_order` and (b) identical world-state hash excl. `editorState`; emit `SEED=/HASH=`; sim isolation holds in CI.
- **AC-6 (Config → AME-2.5, AME-7.2):** Byte-stability across independent CLI runs + minimal focused diff (≤2 hunks incl. mandatory editVersion) with no key reordering. Enforced by `tools/ci/smoke-ac6.sh`.
- **#17 (Stub-scope pins):** Pin current demonstrative/stub behavior for event debugger (ExplainEventTrace / scenario_event_trace), TCA static analysis, AI scaffold/NL (`AiAuthoringServices.NlScaffold` / mission_plan_suggest), IncompatibleHost/BrokenRef (ref: convention heuristic). Tests assert *exact current output shape* (not correctness) so future upgrade is deliberate + paired with doc update. See qa-plan #17 + StubScopePinTests.cs.

See `qa-plan-scenario-editor-2026-07-01.md` §Automated Tests Required for #2, #6, #17. `scenario-editor-scope-boundary-2026-07-04.md` (AC-2/AC-6 + stub pins in scope).

## Hard Gates
- Editor subset filter (includes SimulateSample + StubScope + SaveVsExport + SchemaConformance + DerivedOnly etc.):
  ```bash
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
    --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly|SimulateSample"
  ```
- Targeted AC-2 / determinism:
  ```bash
  dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj \
    --filter "ScenarioSimulateSample"
  ```
- Targeted stub pins:
  ```bash
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
    --filter "StubScopePin"
  ```
- AC-6 (always in this sprint):
  ```bash
  bash tools/ci/smoke-ac6.sh
  ```
- Full baseline (Phase 0 from execute-plan):
  ```bash
  cd /home/username01/cmano-clone/cmano-clone
  export PATH="$HOME/.dotnet:$PATH"

  node .gitnexus/run.cjs status
  node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
  node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
  node .gitnexus/run.cjs impact CatalogWriteGate --direction upstream --summary-only
  node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only

  dotnet build ProjectAegis.sln
  dotnet test ProjectAegis.sln -v minimal
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
    --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
    --filter PlayModeSmokeHarnessTests
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
    --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly|SimulateSample"
  rg "17144800277401907079" tests/regression/ data/ -l
  bash tools/ci/smoke-ac6.sh
  ```
- Overall: `dotnet test ProjectAegis.sln -v minimal` (floor ≥1232; 0 failures excl. known UA pair)
- ReplayGolden 6/6 + C2 proxy 18/18
- Hash grep preserved
- ZERO DelegationBridge edits (grep `DelegationBridge` in src/ outside the bridge file == 0 new)
- GitNexus preflight + post (see below)
- Cite boundary + this sprint plan + roadmap-execute-plan-07042026.md + qa-plan in all changes

## GitNexus Discipline (MANDATORY)
Before any symbol edit (ScenarioSimulateSampleCommand, SimulateSample*, StubScopePinTests, ScenarioValidationEngine, ScenarioDocumentEditor, etc.):

```bash
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact ValidationRules --direction upstream --summary-only
node .gitnexus/run.cjs detect_changes --scope unstaged
```

Report blast radius (e.g. ScenarioValidationEngine HIGH 17 impacted; direct callers in Cli.Tests, Authoring, Validation; SimulateSample paths touch BalticReplayHarness). Run `detect_changes` before closeout commit.

From roadmap §1 (plan authoring baseline): CatalogWriteGate 178 CRITICAL, Patrol 97, DelegationBridge 127, Baltic 52; editor symbols 20 CRITICAL / 17 HIGH. Re-verify at sprint start. `node .gitnexus/run.cjs impact` on ScenarioSimulateSampleCommand paths.

## Verification-before
All agents: run targeted filter + relevant full gates (incl. `bash tools/ci/smoke-ac6.sh` + editor subset) before claiming PASS. Use verification-before-completion. TDD (RED→GREEN) on extensions to AC-2 assertions / CI wiring / stub pins (e.g. non-empty fire_order cases, parallel CI determinism, pipeline step addition).

## Parallel Wave & Dispatch
Per execute-plan §5 Phase 1: After S84 close + S85 plan/kickoff + user ack, dispatch 3 cloud tracks in parallel via superpowers dispatching-parallel-agents + isolated worktrees (`.worktrees/stack/sprint85/*`).

- S85-01 determinism-engineer (AC-2): extend/harden ScenarioSimulateSample* for full fire_order + worldStateSha256 (excl editorState) + SEED/HASH contract; add negative seed control + parallel isolation asserts; use existing golden_clean + new fixtures with events.
- S85-02 c-sharp-devops-engineer (AC-6 CI): add smoke-ac6 step to `.buildkite/pipeline.yml` (post-build or dedicated label; run on serialization paths or always for editor program); update agent-dotnet-ci / preflight as needed; verify PASS in pipeline context.
- S85-03 c-sharp-test-engineer (stub-pins #17): review/extend `StubScopePinTests.cs` (ensure all 4: event debugger stub shape, TCA, NlScaffold keyword-only, ref: heuristic for Incompatible/BrokenRef); assert exact current output; pair any change with doc note.
- S85-04 local closeout: aggregate, gt submit --stack, restack, full Phase 0 re-run, smoke closeout doc, sprint-status update, detect_changes.

Coordinate: if S85-01 touches simulation paths shared with AC-6, single verification.

## Standing Invariants (from boundary + execute-plan)
- Stage = Release (no stage.txt change)
- Test baseline ≥1232 monotonic
- ReplayGolden 6/6; C2 18/18
- Baltic hash `17144800277401907079` preserved (no ADR)
- ZERO DelegationBridge
- CatalogWriteGate extend-only
- Baltic v2/v3 + goldens frozen (use examples/ + schema only)
- Single owner per sprint for ScenarioDocumentEditor (read here); coordinate ValidationEngine (S85 owner per matrix)
- GitNexus exact CRITICAL counts reported in closeout
- AC-6: `bash tools/ci/smoke-ac6.sh` PASS when serialization touched (here: always)

## Cites (MANDATORY on all artifacts / commits / PRs)
- `roadmap-execute-plan-07042026.md` §3/§4 (S85)
- `future-sprint-roadpmap-07042026.md`
- `scenario-editor-scope-boundary-2026-07-04.md`
- `qa-plan-scenario-editor-2026-07-01.md` (units #2, #6, #17)
- `implementation-tracker-2026-07-04.md`
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-2, AC-6, #17)
- AGENTS.md + CLAUDE.md + graphite plan + GitNexus rules
- This sprint plan

## Risks / Notes
- AC-2 must survive parallel CI runners (isolation already in command; assert explicitly).
- smoke-ac6.sh approximates AC-6 (editVersion bump means 2 hunks; documented in script header) — do not "fix" to literal 1-hunk without doc/req update.
- Stub pins are **not** correctness tests; they guard "stub" status per doc 11 + qa-plan. Changing a pin requires deliberate review + doc update.
- Use TDD + fixtures in data/scenarios/examples/ or validation/. Prefer extending existing tests (ScenarioSimulateSampleCliTests already has determinism skeleton).
- No hotpath changes; no sim core mutation; no Baltic frozen goldens.
- GitNexus: ScenarioSimulateSampleCommand participates in export → simulate flows (impact ScenarioValidationEngine + Baltic).

## Commands (run in every track + closeout)
See Hard Gates + GitNexus sections above. Always:
```bash
bash tools/ci/smoke-ac6.sh
```

## Next
After S85-04 closeout + user ack → S86 (CLI/MCP polish + UA triage).

---
*Part of S81–S88 Scenario Editor program per roadmap-execute-plan-07042026.md. Graphite-first. Superpowers dispatching-parallel-agents. All gates RUN+READ. Ready for dispatch.*
