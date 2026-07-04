# Sprint 87 — Unity AC-8 Round-Trip (local)

**Dates:** Following S86 (est. 6–8 days)  
**Lead:** E11 / unity-ui-specialist + qa-tester + c-sharp-devops-engineer  
**Program:** S81–S88 Scenario Editor (req 11)  
**Authority:** `roadmap-execute-plan-07042026.md` §3/§4 (S87), `future-sprint-roadpmap-07042026.md`, `scenario-editor-scope-boundary-2026-07-04.md`, `qa-plan-scenario-editor-2026-07-01.md`, `implementation-tracker-2026-07-04.md`, `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8), AGENTS.md

## Sprint Goal
Deliver Unity AC-8 round-trip evidence (S87 only): headless-authored `.scenario.json` (from `data/scenarios/examples/`) loads in the Unity host (via PlayModeSmokeHarnessTests extension or manual fallback) with ORBAT, missions, events intact and `editorState` populated with schema defaults (camera at theater centroid, all layers on). No map placement or visual editing (Phase 2 out of scope). All local tracks. Maintain standing invariants.

## Tracks (local only; serial waves or parallel where isolated)

| Track | Stack prefix | Worktree path | Agent env | Owner | Story |
|-------|--------------|---------------|-----------|-------|-------|
| PlayMode headless load (S87-01) | `stack/sprint87/playmode-roundtrip` | `.worktrees/stack/sprint87/playmode-roundtrip` | **Local** | unity-ui-specialist | S87-01 |
| Manual QA evidence pack (S87-02) | `stack/sprint87/manual-qa-ac8` | `.worktrees/stack/sprint87/manual-qa-ac8` | **Local** | qa-tester | S87-02 |
| Closeout | `stack/sprint87/closeout` | `.worktrees/stack/sprint87/closeout` | **Local** | c-sharp-devops-engineer | S87-03 |

**Wave order:** S87-01 PlayMode extension (primary) ∥ S87-02 manual prep (if needed) → S87-03 closeout.

**Primary:** Extend `PlayModeSmokeHarnessTests` (Unity Adapter) for headless `.scenario.json` load. Fallback: manual Editor load-and-inspect checklist.

## Primary Files & Changes (S87-01 primary)

- `src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/PlayModeSmokeHarnessTests.cs` — new test(s) exercising load of e.g. `data/scenarios/examples/strike-package.scenario.json` (or baltic-patrol.scenario.json) via scenario loading path + bridge / ISimWorldSnapshot / C2PresentationController assertions.
- Assert: ORBAT (units/groups), missions, events intact and matching source; `editorState` present with defaults (camera centroid, layers enabled). Do not mutate canonical file outside `editorState`.
- Supporting (read-only where possible): `ScenarioPackage`, editor load paths in UnityAdapter, `C2PresentationController` (for state projection), any scenario document loader.
- Manual fallback evidence (S87-02): `production/qa/evidence/s87-ac8-unity-roundtrip-evidence.md` (screenshots + checklist sign-off per qa-plan #8).
- Closeout: `production/qa/smoke-sprint-87-closeout-2026-07-*.md`

**Note:** Extension targets headless PlayMode harness load (no full Editor UI authoring). "Headless .scenario.json load" per execute-plan. Use existing BalticReplayHarness / PlayModeHarness patterns for setup.

## AME AC-8 (from qa-plan + doc 11)
**AC-8 (Integration → AME-1.4, AME-2.4):** A scenario authored entirely via headless MCP loads in the Unity host with intact ORBAT/missions/events and `editorState` populated with schema defaults (camera at theater centroid, all layers on). *Integration test.*

**Verification (qa-plan #8):**
- Extend `PlayModeSmokeHarnessTests` (preferred) or Manual Editor load-and-inspect fallback.
- Checklist:
  - [ ] A scenario authored entirely via headless MCP (no Unity ever touched it) loads without error in the Unity host.
  - [ ] ORBAT, missions, and events are intact and match the source file.
  - [ ] `editorState` is populated with schema defaults on first Unity load: camera at theater centroid, all layers on.
  - [ ] Saving from the Unity host and reloading headlessly still round-trips (no Unity-only field pollutes the canonical file outside `editorState`).
- Evidence: PlayMode test result, or screenshot of loaded scenario in Unity host.

**Out of scope:** Map placement (AME-4.x), visual event graph editing, full Unity edit-mode authoring — Phase 2 (post S88).

## Hard Gates
- Editor / AC-8 specific filter (new + existing):
  ```bash
  dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
    --filter "PlayModeSmokeHarnessTests|AC8|ScenarioRoundtrip|EditorState"
  ```
- Full relevant C2 proxy: `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj --filter PlayModeSmokeHarnessTests` → **18/18**
- Overall: `dotnet test ProjectAegis.sln -v minimal` (floor ≥1232; 0 failures excl. known UA pair)
- ReplayGolden 6/6
- Hash grep: `rg "17144800277401907079" tests/regression/ data/ -l`
- ZERO DelegationBridge edits (grep outside bridge file)
- GitNexus preflight + post (see below)
- `bash tools/ci/smoke-ac6.sh` (if serialization paths touched)
- Cite boundary + this sprint plan + roadmap-execute-plan-07042026.md + qa-plan in all changes

## GitNexus Discipline (MANDATORY)
Before any symbol edit (PlayModeSmokeHarnessTests, related bridge/presentation symbols if touched):

```bash
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact PlayModeSmokeHarnessTests --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only
node .gitnexus/run.cjs detect_changes --scope unstaged
```

Report blast radius. Re-verify at sprint start (CatalogWriteGate 178 CRITICAL etc. exact). Run `detect_changes` before closeout commit. **No hotpath changes to DelegationBridge.**

From roadmap/execute-plan §5 (baseline): editor symbols + CRITICALs reported.

## Verification-before
All agents: run targeted filter + full gates (build + full test + replay + C2 18/18 + hash + ZERO bridge) before claiming PASS. Use verification-before-completion. TDD (RED→GREEN) on new AC-8 test coverage.

## Parallel Wave & Dispatch Prep
Per execute-plan §5 Phase 1 + §4 (S87): After S86 close + S87 plan/kickoff + user ack, dispatch local tracks (only) via superpowers dispatching-parallel-agents + isolated worktrees (`.worktrees/stack/sprint87/*`).

- S87-01 unity-ui-specialist (local): PlayModeSmokeHarnessTests extension for headless `.scenario.json` load + ORBAT/missions/events + editorState assertions. Use `data/scenarios/examples/*.scenario.json`.
- S87-02 qa-tester (local): Manual QA evidence pack (screenshots + signed checklist) as fallback / complement per qa-plan #8.
- S87-03 local closeout: aggregate, gt submit --stack, restack on main, full Phase 0 re-run + editor/C2 filters, produce smoke closeout, sprint-status update, GitNexus detect_changes.

**Local only:** Unity evidence requires local (no cloud Editor). Manual fallback local.

**Commands (run in every track + closeout):**
```bash
cd /home/username01/cmano-clone/cmano-clone
export PATH="$HOME/.dotnet:$PATH"

# Preflight (MANDATORY)
node .gitnexus/run.cjs status
node .gitnexus/run.cjs impact ScenarioDocumentEditor --direction upstream --summary-only
node .gitnexus/run.cjs impact DelegationBridge --direction upstream --summary-only

# Targeted AC-8 / C2
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "PlayModeSmokeHarnessTests|AC8|ScenarioRoundtrip|EditorState"

# Full baseline gates (Phase 0)
dotnet build ProjectAegis.sln
dotnet test ProjectAegis.sln -v minimal
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter "FullyQualifiedName~ReplayGoldenSuiteTests"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj \
  --filter PlayModeSmokeHarnessTests
rg "17144800277401907079" tests/regression/ data/ -l
bash tools/ci/smoke-ac6.sh
```

## Standing Invariants (from boundary + execute-plan)
- Stage = Release (no stage.txt change)
- Test baseline ≥1232 monotonic
- ReplayGolden 6/6; C2 18/18
- Baltic hash `17144800277401907079` preserved (no ADR)
- ZERO DelegationBridge
- CatalogWriteGate extend-only
- Baltic v2/v3 + goldens frozen (use `data/scenarios/examples/` + schema only)
- Single owner coordination for ScenarioDocumentEditor (read/load here)
- GitNexus exact CRITICAL counts reported in closeout
- No map placement / Phase 2 authoring

## Cites (MANDATORY on all artifacts / commits / PRs)
- `roadmap-execute-plan-07042026.md` §3/§4 (S87)
- `future-sprint-roadpmap-07042026.md`
- `scenario-editor-scope-boundary-2026-07-04.md`
- `qa-plan-scenario-editor-2026-07-01.md` (unit #8 AC-8)
- `implementation-tracker-2026-07-04.md`
- `Game-Requirements/requirements/11-Agentic-Mission-Editor.md` (AC-8)
- AGENTS.md + CLAUDE.md + graphite plan
- This sprint plan

## Risks / Notes
- PlayMode extension must remain "headless" (no Editor UI interaction required for the test; use harness + snapshot load).
- If automation infeasible this cycle: full manual Editor load checklist + screenshots + qa-lead sign-off is acceptable fallback (per qa-plan).
- Preserve editorState as derived-only; round-trip must not pollute canonical fields.
- Local worktree only for this sprint (Unity).
- Cite AC-8 + "no map placement (Phase 2 out)" everywhere.

## Next
After S87-03 closeout + user ack → S88 (Gate verification ∥ Human sign-off "scenario editor program complete").

---
*Part of S81–S88 Scenario Editor program per roadmap-execute-plan-07042026.md. Local-only for Unity AC-8. Ready for dispatch after boundary + prior sprints.*
