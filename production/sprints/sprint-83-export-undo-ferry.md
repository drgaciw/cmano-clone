# Sprint 83 — Export/undo + ferry (track D)

**Dates:** 2026-07-04 start (est. 6–8 days)  
**Lead:** E11 / team-data + game-designer + c-sharp-devops-engineer  
**Program:** S81–S88 Scenario Editor (req 11 / E11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S83), `future-sprint-roadpmap-07042026.md` §3/§4/§10, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md` (units #5, #13, #14), `implementation-tracker-2026-07-04.md`, prior S81 `sprint-81-scenario-editor-foundations.md` + S82 `sprint-82-validation-tracks-ac.md` + closeouts, AGENTS.md

## Sprint Goal
Deliver track D command surface per roadmap: 
- Export command polish (S83-01)
- Undo CLI wiring + round-trip for AME-8.5 (S83-02)
- Ferry sample + AC-5 fixture (S83-03, unblocking/completing Strike+Patrol+Support+Ferry headless sample)
Followed by local closeout (S83-04). Preserve all standing invariants from boundary.

## Tracks (parallel after preflight)

| Track | Stack prefix | Worktree path | Agent env | Owner |
|-------|--------------|---------------|-----------|-------|
| Export command polish (S83-01) | `stack/sprint83/scenario-export` | `.worktrees/stack/sprint83/scenario-export` | Cloud | team-data |
| Undo CLI wiring (AME-8.5) (S83-02) | `stack/sprint83/scenario-undo` | `.worktrees/stack/sprint83/scenario-undo` | Cloud | team-data |
| Ferry sample + AC-5 fixture (S83-03) | `stack/sprint83/ferry-sample` | `.worktrees/stack/sprint83/ferry-sample` | Cloud | game-designer |
| Closeout (S83-04) | `stack/sprint83/closeout` | `.worktrees/stack/sprint83/closeout` | **Local** | c-sharp-devops-engineer |

**Wave order:** 3 tracks parallel (Export ∥ Undo ∥ Ferry) → Closeout

## Primary Files (from roadmap-execute-plan-07042026.md §4)

| File | QA unit / Track |
|------|-----------------|
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioExportCommand.cs` | S83-01 (export pipeline + Prepare/ApplyTeleport...) |
| `src/ProjectAegis.MissionEditor.Cli/ScenarioExportCommand.cs` (CLI wrapper if split; see usage in ScenarioPublishCommand / ScenarioSimulateSampleCommand) | S83-01 |
| `src/ProjectAegis.Data/Scenario/Authoring/ScenarioUndoStackStore.cs` | #14 / S83-02 |
| `src/ProjectAegis.MissionEditor.Cli/ScenarioUndoCommand.cs` | #14 / S83-02 |
| `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioUndoCliTests.cs` | #14 / S83-02 (round-trip) |
| `src/ProjectAegis.MissionEditor.Cli/MissionAddFerryCommand.cs` | #13 / S83-03 |
| `src/ProjectAegis.MissionEditor.Cli/MissionUpdateFerryCommand.cs` | #13 / S83-03 |
| `src/ProjectAegis.MissionEditor.Cli.Tests/MissionAddFerryCommandTests.cs` | #13, #5 / S83-03 |
| `src/ProjectAegis.MissionEditor.Cli.Tests/ScenarioSimulateSampleCliTests.cs` | #5 / S83-03 (Strike+Patrol+Support+Ferry) |
| Supporting: `src/ProjectAegis.Data/Scenario/Authoring/ScenarioDocumentEditor.cs`, `src/ProjectAegis.Data/Validation/Rules/ValidationRules.cs` (Ferry*Rule), `data/scenarios/examples/*.scenario.json` (new ferry sample fixture), `src/ProjectAegis.MissionEditor.Cli/Program.cs` (verb dispatch) |

## AME-8.5 Note on Persistence Design Lock
**Critical per roadmap-execute-plan-07042026.md §4 and qa-plan-scenario-editor-2026-07-01.md #14:** Confirm snapshot persistence (disk vs in-process) **before** S83-02 TDD. 

Current implementation (confirmed via source): `ScenarioUndoStackStore` uses disk-backed JSON sidecar stacks (`ResolveStackPath`, `Push`/`TryPop`/`Count` serialize to scenario-adjacent file). This enables cross-process CLI undo (unlike pure in-memory stub on single `ScenarioDocumentEditor`). 

**Action in S83-02:** Document the lock decision (disk confirmed) in closeout artifact or addendum to `scenario-editor-scope-boundary-2026-07-04.md` if any change. Do not assume in-memory; verify round-trip survives process boundaries in tests.

## Hard Gates (per roadmap-execute-plan-07042026.md §6 + boundary + AGENTS.md)
- **Editor subset + touched areas:** `dotnet test src/ProjectAegis.Data.Tests/ProjectAegis.Data.Tests.csproj --filter "ScenarioDocument|SaveVsExport|SchemaConformance|...|Ferry|Undo|Export"` (extend S82 pattern)
- **Full test floor:** `dotnet test ProjectAegis.sln -v minimal` → **≥1232** (current baseline ~1312 runs with 2 known pre-existing UA failures in `BalticReplayHarnessPolicyEngageTests`; 0 new failures)
- ReplayGolden 6/6 (`--filter FullyQualifiedName~ReplayGoldenSuiteTests`)
- C2 proxy PlayModeSmokeHarnessTests 18/18
- Production Baltic hash **`17144800277401907079`** preserved (grep confirmed in `tests/regression/*.txt` and data)
- **ZERO DelegationBridge edits** (grep `DelegationBridge` in src/ hotpath must stay 0 new changes)
- **GitNexus pre (mandatory):** 
  - `node .gitnexus/run.cjs status` (current: ✅ up-to-date on 17d426c)
  - `node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only` (CRITICAL 20, 6 processes)
  - impacts on ScenarioExportCommand / ScenarioUndoStackStore / ferry rules / ValidationRules
  - `node .gitnexus/run.cjs detect_changes` (pre-commit)
- `bash tools/ci/smoke-ac6.sh` (if serialization/AC-6 paths touched)
- Cite boundary + execute-plan + S81/S82 + qa-plan #5/#13/#14 in all artifacts and commits
- Stage remains **Release**

**Mandatory verification-before-completion (RUN+READ all, per AGENTS.md + roadmap §5):**
```bash
export PATH="$HOME/.dotnet:$PATH"
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact ScenarioValidationEngine --direction upstream --summary-only || true

dotnet build ProjectAegis.sln                   # 0 errors, 0 warnings
dotnet test ProjectAegis.sln -v minimal          # ≥1232 / 0 failures (excl known UA)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests   # 18/18
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter "FullyQualifiedName~ReplayGoldenSuiteTests" # 6/6
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh   # if applicable
```

## Verification-before + TDD
- GitNexus impact preflight **before any edit** to symbols (ScenarioExportCommand, ScenarioUndoStackStore, Mission*Ferry*, editor flows).
- TDD: RED (extend test) → GREEN for new coverage on #5/#13/#14.
- Use superpowers:test-driven-development.
- No direct edits to hotpath without impact report in track notes.

## Standing Invariants (from scenario-editor-scope-boundary-2026-07-04.md + prior)
- Test baseline monotonic ≥1232; Replay 6/6; C2 18/18
- ZERO DelegationBridge touch
- CatalogWriteGate extend-only
- Baltic v2/v3 frozen; editor uses examples/ + schema
- Single owner per sprint for ScenarioDocumentEditor (coordinate S83)
- GitNexus discipline (status + impact)
- Stage: Release (no auto-advance at S88)

## Risks / Notes
- AME-8.5 persistence design lock must be explicit (disk already implemented).
- Ferry sample completes AC-5 (transitively unblocked by prior #13 ferry verbs).
- Export polish may touch shared Prepare path used by publish/simulate — impact pre + subset tests required.
- 2 UA failures remain S86-owned; do not regress or touch BalticReplayHarness hot paths.
- Use Graphite: gt create / submit --stack per worktree.

## Next after Closeout
User ack + gt submit / restack → S84 dispatch (debugger + teleport + ADR-016).

**References (heavy citation required in all deliverables):**
- `docs/reports/roadmap-execute-plan-07042026.md` §3/§4 S83 + §5/§6/§7
- `docs/reports/future-sprint-roadpmap-07042026.md`
- `production/scenario-editor-scope-boundary-2026-07-04.md` (invariants, ownership, scope)
- `production/qa/qa-plan-scenario-editor-2026-07-01.md` (#5 AC-5, #13 AME-8.4 ferry, #14 AME-8.5 undo, open item on persistence)
- `Game-Requirements/implementation-tracker-2026-07-04.md` (track D status, CLI verbs)
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-5, AME-8.4/8.5)
- S81/S82 sprint + agentic kickoff/closeout docs
- AGENTS.md (GitNexus always, ZERO bridge, test floor, hash)

---
*Part of S81–S88 Scenario Editor program. Parallel prep style. Created 2026-07-04.*
