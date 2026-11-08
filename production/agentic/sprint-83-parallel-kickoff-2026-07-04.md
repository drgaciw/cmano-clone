# Sprint 83 Parallel Kickoff — Export/undo + ferry (track D)

**Date:** 2026-07-04  
**Sprint:** S83  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S83), `scenario-editor-scope-boundary-2026-07-04.md`, `future-sprint-roadpmap-07042026.md` §3/§4/§10, `qa-plan-scenario-editor-2026-07-01.md` (units #5, #13, #14), `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md`, S81/S82 parallel kickoffs + sprint plans + closeouts, AGENTS.md + CLAUDE.md

## Context
S82 validation tracks A+C (doctrine, schema, save-vs-export) complete with editor subset green. In-flight work from `fix-scenario-publish-cli-wiring` @ 17d426c integrated or ready per S81-02 plan.

**S83 focuses on track D (command surface + ferry):**
- Export command polish (S83-01)
- Undo CLI wiring (AME-8.5) (S83-02) — includes design lock on persistence
- Ferry sample + AC-5 fixture (S83-03) — completes Strike+Patrol+Support+Ferry headless sample (unblocked by prior AME-8.4 ferry verbs)

All 3 tracks **cloud** (parallel) → local closeout.

**GitNexus status (pre-kickoff):** ✅ up-to-date (branch 17d426c). ScenarioDocumentEditor upstream: CRITICAL (20 impacted, 6 processes). Ferry rules and export/undo symbols require preflight impact before edits.

## Dispatch Model (superpowers + AGENTS.md)
- Use `dispatching-parallel-agents` skill for the 3 independent tracks.
- **Isolated worktrees required:** `.worktrees/stack/sprint83/scenario-export`, `/scenario-undo`, `/ferry-sample`
- **Mandatory per track (before any symbol edit):**
  - GitNexus: `node .gitnexus/run.cjs status` + `impact <symbol> --direction upstream --summary-only` (ScenarioDocumentEditor, ScenarioExportCommand, ScenarioUndoStackStore, Ferry*Rule, ValidationRules, Mission*Ferry*)
  - Cite **boundary** + execute-plan + qa-plan units #5/#13/#14 + S81/S82 in every artifact, commit, and subagent prompt.
- TDD (RED→GREEN) + verification-before-completion on all PASS claims.
- No hotpath changes to DelegationBridge; extend-only on CatalogWriteGate.
- Persistence design lock (AME-8.5) confirmed/documented **before** S83-02 implementation.
- Use `gt` for stack work (no raw git push or gh pr create).

## Track Assignments (this sprint)

**S83-01 Export command polish (Cloud, team-data)**
- Goal: Polish `ScenarioExportCommand.Prepare` / transforms / manifest; ensure clean export paths for publish + simulate_sample; update MCP/CLI surface if needed. Add/extend tests.
- Primary files: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioExportCommand.cs`, related CLI callers, tests.
- Constraints: Impact pre on export + editor; run subset filters; no bridge touch.
- Output: Updated command + passing tests + notes citing roadmap §4 + boundary.

**S83-02 Undo CLI wiring (AME-8.5) (Cloud, team-data)**
- Goal: Complete wiring + full round-trip (mutate → snapshot → rollback) for `scenario_undo` / `ScenarioUndoCommand` + `ScenarioUndoStackStore`. Ensure persists across process (CLI invocations).
- Primary files: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioUndoStackStore.cs`, `src/ProjectAegis.MissionEditor.Cli/ScenarioUndoCommand.cs`, `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs`, editor undo APIs.
- **AME-8.5 design lock:** Per roadmap-execute-plan-07042026.md §4 + qa-plan #14: disk persistence (current impl via JSON sidecar) vs in-process. Confirm + document decision explicitly before TDD. Round-trip test must survive separate CLI runs.
- Constraints: GitNexus impact; editor subset + undo tests; cite #14.
- Output: Green round-trip tests + CLI verb complete.

**S83-03 Ferry sample + AC-5 fixture (Cloud, game-designer)**
- Goal: Author ferry-inclusive sample (Strike+Patrol+Support+Ferry) using existing `mission_add_ferry`/`mission_update_ferry`; extend `ScenarioSimulateSampleCliTests.cs` (and pipeline tests) to cover AC-5; validate end-to-end headless sample.
- Primary files: `src/ProjectAegis.MissionEditor.Cli/MissionAddFerryCommand.cs`, `MissionUpdateFerryCommand.cs`, `MissionAddFerryCommandTests.cs`, `ScenarioSimulateSampleCliTests.cs`, `SampleCompletePipelineTests.cs`; add/update fixture under `data/scenarios/examples/` or test assets.
- Constraints: Builds on #13 (AME-8.4) completion; uses validation ferry rules; cite #5 + #13.
- Output: Ferry sample fixture + AC-5 passing assertions.

**S83-04 Closeout (Local, c-sharp-devops-engineer)**
- Aggregate evidence from 3 tracks.
- Re-run **full** Phase 0 gates (build, sln test, Replay 6/6, C2 18/18, hash grep, GitNexus detect_changes + impacts).
- Produce `production/qa/smoke-sprint-83-closeout-2026-07-*.md` (or per pattern).
- Update `production/sprint-status.yaml`.
- Drive user ack + `gt submit --stack --no-interactive` + restack/sync.
- Confirm ZERO bridge, editor filters, AME-8.5 note documented.

## Hard Gates for S83 Close
- Build: 0 errors / 0 warnings (`dotnet build ProjectAegis.sln`)
- Tests: ≥1232 floor, 0 new failures (excl. 2 known UA in UA.Tests); full sln + targeted editor/ferry/undo/export
- ReplayGolden 6/6
- PlayModeSmokeHarnessTests 18/18
- Hash `17144800277401907079` preserved (no change)
- ZERO `DelegationBridge.cs` source edits (grep verification)
- GitNexus: fresh status + impacts on CRITICALs (ScenarioDocumentEditor 20 CRITICAL) + editor symbols; `detect_changes` pre-commit
- Editor subset filters green (Data.Tests + Cli.Tests for touched)
- All artifacts cite boundary + roadmap-execute-plan-07042026.md + qa-plan units #5/#13/#14 + S81/S82
- AME-8.5 persistence decision documented
- Stage remains Release
- (Optional AC-6 if touched): `bash tools/ci/smoke-ac6.sh` PASS

**Full gate script (RUN+READ before closeout claim):**
```bash
cd /home/username01/projects/active/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only

dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
rg "17144800277401907079" tests/regression/ data/ -l
# Editor subset example (extend for export/undo/ferry):
dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "DerivedOnly|SaveVsExport|SchemaConformance|ScenarioDocument"
dotnet test src/ProjectAegis.MissionEditor.Cli.Tests/ProjectAegis.MissionEditor.Cli.Tests.csproj --filter "MissionAddFerry|ScenarioUndo|SimulateSample"
```

## Communication
- Surface questions early (NEEDS_CONTEXT / BLOCKED).
- Coordinator (local) owns merge order, `ScenarioDocumentEditor` contention (S83 owner), human gates.
- Report GitNexus blast radius + risk in track artifacts.
- Do not dispatch S84 until S83 closeout PASS + user ack + gt submit.

## References (cite in all work)
- `docs/reports/roadmap-execute-plan-07042026.md` (S83 tables, primary files, wave, AME-8.5 note, Phase 0/1/2, hard gates, ownership matrix)
- `docs/reports/future-sprint-roadpmap-07042026.md`
- `production/scenario-editor-scope-boundary-2026-07-04.md` (standing invariants, scope in/out, file ownership, CRITICAL symbols)
- `production/qa/qa-plan-scenario-editor-2026-07-01.md` (detailed #5 AC-5 sample, #13 ferry verbs, #14 undo wiring + open persistence item)
- `Game-Requirements/implementation-tracker-2026-07-04.md` (track D partial status, CLI verbs inventory)
- S81: `production/sprints/sprint-81-scenario-editor-foundations.md`, `production/agentic/sprint-81-parallel-kickoff-2026-07-04.md`
- S82: `production/sprints/sprint-82-validation-tracks-ac.md`, `production/agentic/sprint-82-parallel-kickoff-2026-07-04.md`
- AGENTS.md (GitNexus MUSTs, test baseline, ZERO bridge, graphite, collab protocol)
- `production/agentic/local-cloud-agent-routing.md`

**Kickoff complete.** GitNexus pre passed on current branch. Dispatch S83-01, S83-02, S83-03 in parallel (3 cloud agents). S83-04 local after.

---
*Parallel prep style per roadmap-execute-plan-07042026.md §4. Ready for subagent dispatch. 2026-07-04.*
