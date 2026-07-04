# Sprint 84 — Event Debugger (track B + ADR-016)

**Dates:** Following S83 (est. 6–8 days)  
**Lead:** E11 / team-simulation + c-sharp-test-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S84), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-7, AC-11, #16 ADR-016), AGENTS.md

## Sprint Goal
Complete Event debugger track B: deliver AC-7 (structured event debugger JSON for unmet conditions), AC-11 (TeleportUnit logged export transform), and ADR-016 event-graph complexity caps unit test (#16). Advance primary files `EventDebuggerTrace`, CLI command, teleport export tests, `EventGraphComplexityTests`, `ValidationRules`. Maintain all standing invariants.

## Tracks (parallel day 1)

| Track | Stack prefix | Worktree path | Agent env | Owner |
|-------|--------------|---------------|-----------|-------|
| Event debugger JSON (AC-7) | `stack/sprint84/event-debugger` | `.worktrees/stack/sprint84/event-debugger` | Cloud | team-simulation |
| Teleport export transform (AC-11) | `stack/sprint84/teleport-export` | `.worktrees/stack/sprint84/teleport-export` | Cloud | team-simulation |
| ADR-016 complexity caps | `stack/sprint84/event-graph-caps` | `.worktrees/stack/sprint84/event-graph-caps` | Cloud | c-sharp-test-engineer |
| Closeout | `stack/sprint84/closeout` | `.worktrees/stack/sprint84/closeout` | Local | c-sharp-devops-engineer |

**Wave order:** Debugger + teleport ∥ caps (single owner on `ValidationRules` if both touch) → closeout

**Primary files (per execute-plan §4):**

| File | QA unit / AC |
|------|--------------|
| `src/ProjectAegis.Data/Scenario/Authoring/EventDebuggerTrace.cs` | AC-7 |
| `src/ProjectAegis.Data.Tests/Scenario/EventDebuggerTests.cs` | AC-7 |
| `src/ProjectAegis.MissionEditor.Cli/ScenarioEventTraceCommand.cs` (or equivalent in Program.cs) | AC-7 |
| `src/ProjectAegis.Data.Tests/Scenario/TeleportUnitExportTests.cs` | AC-11 |
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioExportCommand.cs` (ApplyTeleportUnitExportTransform) | AC-11 |
| `src/ProjectAegis.Data.Tests/Validation/EventGraphComplexityTests.cs` | #16 (ADR-016; create if missing) |
| `src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs` | #16 (ADR-016) |
| Related: `ScenarioDocumentEditor.cs` (ExplainEventTrace), `ManifestBuilder`, `ExportTransformManifest` | supporting |

## AME ACs & QA Units
- **AC-7 (Logic → AME-5.5):** Event debugger JSON projects unmet conditions (e.g. UnitEntersZone never holds) with `fired:false`, `last_evaluated_tick`, `unmet_conditions[]`. Matches order-log projection semantics. Test: `EventDebuggerTests.UnitEntersZone_never_holds...` vs `event-no-fire` fixture.
- **AC-11 (Logic → AME-6.8):** TeleportUnit actions stripped at export with explicit logged manifest entries (never silent). Post-transform event set identical between export path and simulate sample. UI "edit-test only" badge contract.
- **#16 (ADR-016):** Hard cap 32 conditions/event = blocking error. Soft warnings for complexity > WARN_THRESHOLD (default 400) and peak_tick_density > DENSITY_THRESHOLD (default 20); warnings never block export. Tests assert boundary, zero-event, exact threshold comparison, export still allowed on soft breach.

See `qa-plan-scenario-editor-2026-07-01.md` §Automated Tests Required for #7, #11 (note: AC-11 requires TeleportUnit action impl prior), #16.

## Hard Gates
- Editor subset filter (includes EventDebugger|Teleport|EventGraphComplexity|SchemaConformance etc.):
  ```bash
  dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj \
    --filter "EventDebugger|TeleportUnitExport|EventGraphComplexity"
  ```
- Full relevant: `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioDocumentEditor|ScenarioValidation|SaveVsExport|DoctrineInheritance|EventDebugger|SchemaConformance|StubScope|DerivedOnly"`
- Overall: `dotnet test ProjectAegis.sln -v minimal` (floor ≥1232; 0 failures excl. known UA pair)
- ReplayGolden 6/6 + C2 proxy 18/18
- Hash grep: `rg "17144800277401907079" tests/regression/ data/ -l`
- ZERO DelegationBridge edits (grep outside bridge file)
- GitNexus preflight + post (see below)
- `bash tools/ci/smoke-ac6.sh` (if serialization paths touched)
- Cite boundary + this sprint plan + roadmap-execute-plan-07042026.md + qa-plan in all changes

## GitNexus Discipline (MANDATORY)
Before any symbol edit (EventDebuggerTrace, ValidationRules, ScenarioValidationEngine, ScenarioDocumentEditor, ScenarioExportCommand, etc.):

```bash
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ValidationRules --direction upstream --summary-only
# (EventDebuggerTrace may need re-index if on stack branch)
node .gitnexus/run.cjs detect_changes --scope unstaged
```

Report blast radius (e.g. ScenarioValidationEngine HIGH 17 impacted; direct callers in Cli.Tests, Authoring, Validation). Run `detect_changes` before closeout commit.

From roadmap §1 (plan authoring baseline): CatalogWriteGate 178 CRITICAL, Patrol 97, DelegationBridge 127, Baltic 52; editor symbols 20 CRITICAL / 17 HIGH. Re-verify at sprint start.

## Verification-before
All agents: run targeted filter + relevant full gates before claiming PASS. Use verification-before-completion. TDD (RED→GREEN) on new coverage (EventGraphComplexityTests new file).

## Parallel Wave & Dispatch
Per execute-plan §5 Phase 1: After S83 close + S84 plan/kickoff + user ack, dispatch 3 cloud tracks in parallel via superpowers dispatching-parallel-agents + isolated worktrees (`.worktrees/stack/sprint84/*`).

- S84-01 team-simulation (event-debugger): focus EventDebuggerTrace + tests + CLI command surface for AC-7 JSON.
- S84-02 team-simulation (teleport-export): harden TeleportUnitExportTests + export transform + manifest; assert identical post-transform sets.
- S84-03 c-sharp-test-engineer (event-graph-caps): create EventGraphComplexityTests; extend ValidationRules for complexity/tick-density calc + 32-cap error + soft warning findings. Single owner on ValidationRules.
- S84-04 local closeout: aggregate, gt submit --stack, restack, full Phase 0 re-run, smoke closeout doc, sprint-status update, detect_changes.

Coordinate: if S84-01/03 both need ValidationRules, one owner.

## Standing Invariants (from boundary + execute-plan)
- Stage = Release (no stage.txt change)
- Test baseline ≥1232 monotonic
- ReplayGolden 6/6; C2 18/18
- Baltic hash `17144800277401907079` preserved (no ADR)
- ZERO DelegationBridge
- CatalogWriteGate extend-only
- Baltic v2/v3 + goldens frozen (use examples/ + schema only)
- Single owner per sprint for ScenarioDocumentEditor (read here)
- GitNexus exact CRITICAL counts reported in closeout

## Cites (MANDATORY on all artifacts / commits / PRs)
- `roadmap-execute-plan-07042026.md` §3/§4 (S84)
- `future-sprint-roadpmap-07042026.md`
- `scenario-editor-scope-boundary-2026-07-04.md`
- `qa-plan-scenario-editor-2026-07-01.md` (units 7,11,16)
- `implementation-tracker-2026-07-04.md`
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-7, AC-11, ADR-016)
- AGENTS.md + CLAUDE.md + graphite plan
- This sprint plan

## Risks / Notes
- AC-11 requires TeleportUnit action type to be implemented in DTOs/models before full transform test (per qa-plan).
- Event debugger JSON is filtered view of order-log (not duplicate store); align with AME-5.5 + doc 17.
- ADR-016: warnings soft (never block), hard cap only at 32 conditions/event. Thresholds from config (provisional).
- Use TDD + fixtures in data/scenarios/examples/ or validation/.
- No hotpath changes; no sim core mutation.

## Next
After S84-04 closeout + user ack → S85 (Determinism + AC-6 CI + stub pins).

---
*Part of S81–S88 Scenario Editor program per roadmap-execute-plan-07042026.md. Ready for dispatch after boundary + prior sprints.*
