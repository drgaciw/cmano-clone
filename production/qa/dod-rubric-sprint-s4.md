# Unified Sprint 4 Definition of Done Rubric (Jev-Gated)

**Sprint:** Sprint 4 — H4 Bounded Editor Workflow, Capability Disclosure & G3 Evidence (`sprint/s4-h4-editors-g3`, stacked on `sprint/s3-coord-ai-depth` @ `d25e017d`)
**Sprint 4 Atomic Track Commits:** `039531a3` (Track A: `DRG-371`, `DRG-346` headless), `730e2007` (Track B: `DRG-346` UI, `DRG-274` PlayMode tests), plus QA/G3 evidence commit (`dod-rubric-sprint-s4.md`, `production/qa/evidence/sprint-4/`)
**Jev Launch Decision:** `gen-dec-1795431960-XxFmQstt8OQouMWMGBkD` (2026-11-23, `typesafe/jev-1.14`)
**Jev DoD Gate Decision:** `gen-dec-1795436779-EghqACMsaBRxFTk4RcP7` (2026-11-23, `typesafe/jev-1.14-20261014`, Round 1 **PASS — `more_rounds = 0`**)
**Unified Standard:** Notion **Project Aegis Rubrics (DRAFT v1)** (`H1`–`H8` + `S1`–`S6 ≥ 9/12`) + Play Mode UAT (`C1`–`C13`).
**Pass Rule:** Every mechanical Hard Gate passes, every non-owner Jev `noul ≥ 0.80`, Scored `S1`–`S6 ≥ 9/12`, and `more_rounds = 0`. **Script-first:** Jev never overrides a failed mechanical gate.

## Scope (3 Unblocked Linear Issues; `DRG-236` & `DRG-231` remain `hold:owner-go`)

| Track | Commit | Issues | Surface & Key Deliverables | Status |
|---|---|---|---|---|
| Track A (Headless H4 Capability & Authoring Pipeline) | `039531a3` (23 files) | `DRG-371`, `DRG-346` (headless) | `MissionRoleCapabilityManifest` (`GenericOnly`, `CliSimulateBackendId`, `CodeRoleGenericOnly`, trimmed case-insensitive backend lookup), `ScenarioValidationEngine` / `ScenarioValidationExportGate` / `ScenarioExportCommand` / `ScenarioSaveExportGate` additive `backendId` overloads, `ScenarioDocumentEditor` (`RestoreSnapshot`, `AssignUnitToMission`), `ScenarioEditCommandBus.AssignUnitToMission`, `ScenarioSimulateSampleCommand` non-blocking JSON `warnings` before `SEED=`, `MissionRoleCapabilityProjection` / `ScenarioExportGateState` / `LiveFindingsPresenter` (`HasGenericExecutionWarnings`, `GenericExecutionFindings`), `BoundedAuthoringWorkflow` / `BoundedAuthoringContracts` (`OpenDraft` session preservation, `CATALOG_UNBOUND`, pre-validated `AssignToMissionId`, undo/redo, ungated `SaveDraft`, error-gated `ExportPackage`, `PlayEntrySession` handoff), 5 deterministic fixtures under `assets/data/scenarios/validation/` | COMPLETE |
| Track B (Unity Authoring UI & PlayMode/Editor Tests) | `730e2007` (6 tracked files + `Assets/Plugins/ProjectAegis/` sync) | `DRG-346` (UI), `DRG-274` (G3) | `ScenarioEditorShellHost` (`D26` `LoadRequested`, `SaveRequested`, `UndoRequested`, `RedoRequested`, `PlayRequested`, `SampleRequested`, `AttachAuthoringWorkflow`, `AttachDefaultAuthoringWorkflow`, `DetachAuthoringWorkflow`, `OnDisable` reset/unsubscription, pre-Awake null guards, `DelegationBridgeHost.BindExternalSession`), `ScenarioEditorShell.uxml` (`#scenario-editor-shell-map-summary`), non-rewriting `ScenarioMapAuthoringWindow` (`GenericOnly` disclosure without claiming export allowed when blocked), `ScenarioEditorShellHostContractTests` (`3/3`), `Sprint4AuthoringPlayModeTests` (`8/8`) | COMPLETE |
| Coordinator / QA & G3 Evidence | QA commit (`production/qa/`) | `DRG-274` | `s4-c8-red-evidence.md`, `s4-g3-me-workflow-evidence.md`, 8 inspected live Unity `:8081` Editor & Play Mode screenshots (`s4-g3-*.png`), Linear evidence comments (`DRG-371`, `DRG-346`, `DRG-274`), Jev DoD gate (`jev-sprint4-dod-round1-*.json`) | COMPLETE (agent-prep complete; `H6`/`C10` owner sign-off pending) |

## Hard Gates (All Must Be YES)

| ID | Gate | Mechanical Check & Verified Evidence | Status | Owner-Only |
|---|---|---|---|---|
| H1 / C1 | Zero-warning build & full headless suite green | `dotnet build ProjectAegis.sln --no-restore` (`0` warnings, `0` errors); `ProjectAegis.Data` & `ProjectAegis.Delegation.UnityAdapter` `-c Release -f netstandard2.1` (`0` warnings, `0` errors); `dotnet test ProjectAegis.sln -v minimal --no-build`: **`4,938/4,938` passed, `0` failed, `0` skipped** across all 9 assemblies (`Ai.Contracts.Tests` `18`, `Delegation.Demo.Tests` `15`, `Ai.Host.Tests` `88`, `Sim.Tests` `653`, `Delegation.Tests` `1,541`, `Data.Excel.Tests` `24`, `Delegation.UnityAdapter.Tests` `1,461`, `Data.Tests` `967`, `MissionEditor.Cli.Tests` `171`; ≥ 4,912 floor) | PASS | no |
| H2 | Tracker honesty | Linear evidence comments posted via `save_comment` ONLY on `DRG-371` (`8b25dd84-634e-4bab-9135-b60f443f47ca`), `DRG-346` (`191eaaf0-a4f3-4f84-b2c3-e2efb80ae822`), and `DRG-274` (`f2ed36a4-d689-4fd9-b271-39fed729951c`), with all statuses kept `Backlog` (NEVER moved to `Done` before owner PR merge) | PASS | no |
| H3 / C7 | Hard-no & hold surfaces untouched | `git diff --stat d25e017d..HEAD` across `DelegationBridge.cs`, `src/ProjectAegis.Data/WriteGate/`, `SimulationSession.cs`, `MapPlaceholderPanelHost.cs`, `GlobeMapProductHost.cs`, `tests/regression/`, `DelegationSmoke.unity`, `assets/data/catalog/`: **empty (`0` diff)**; `UnitySimImportForbidTests\|NamespaceSccBaselineTests\|CsprojReferenceGraphPinTests` **`31/31`**; `0` `UnityEngine` refs in `ProjectAegis.Delegation.UnityAdapter`; no replacement hosts created; `DRG-236` & `DRG-231` verified `In Progress` with `hold:owner-go` and untouched | PASS | no |
| H4 / C2 | Determinism preserved | `ReplayGolden` **`17/17`** pass; Baltic v2 hash `17144800277401907079` unchanged (`26` hits in `tests/` & `data/`); CLI `scenario_simulate_sample` deterministic `SEED=42 HASH=a717c33911b1d5ddd3b0e4c5c5feb587cdd26be9e6b7da0e6e7425999b819c4d` on `role-capability-generic-only.json` | PASS | no |
| H5 / C8 | Strict TDD RED→GREEN + source-fence tests | Pre-fix RED failures + GREEN + Track A Iter 2 + Track B Iter 2 remediations recorded verbatim in `production/qa/evidence/sprint-4/s4-c8-red-evidence.md`; `MissionRoleCapabilityManifestTests` (`28/28`), `ScenarioRoleCapabilityCliTests\|ScenarioSimulateSampleCliTests` (`17/17`), `MissionRoleCapabilityProjectionTests` (`10/10`), `BoundedAuthoringWorkflowTests` (`11/11`), `ScenarioEditorShellHostContractTests` (`3/3`), `ScenarioMapAuthoringPanelUiTests\|ScenarioEditorActionAvailabilityTests` (`16/16`) | PASS | no |
| H6 / C10 | Owner Play Mode walk preserved | Agent supplies fresh live `:8081` Editor & Play Mode MCP tests (`61/61`, `8/8`) + 8 screenshots (`s4-g3-me-workflow-evidence.md`); **agent-prep complete / owner sign-off pending** (interactive owner Play Mode walk & PR merge remain owner-only) | AGENT-PREP COMPLETE (owner sign-off pending) | **yes** |
| H8 / C3 / C12 | Smoke floor & shipped data integrity | `PlayModeSmokeHarnessTests` **`27/27`**; `sha256sum assets/data/catalog/*.db` byte-identical (`82d262129d41c6fbb78fa636a3c2923471dedf37db071f7a4e7c99bdec4760fd` `aegis_public_corpus.db`, `75ea357a22de185808c4097b3ab1ddfcd836a4da37635576c0816b514d0b88ab` `baltic_patrol.db`); SHA-256 parity verified across `ProjectAegis.*.dll` in `Assets/Plugins/ProjectAegis/` | PASS | no |
| C4 / C13 | Unity Console & PlayMode Runner | Live Unity `:8081` `ProjectAegis.Unity.Tests` PlayMode **`61/61` PASSED** (`0` failed, `0` skipped, `00:00:05.3660460`), including `Sprint4AuthoringPlayModeTests` **`8/8` PASSED** (`00:00:01.0608190`); **`0` Console `Error`/`Exception` logs** | PASS | no |
| C9 / C11 | Live Editor/Play Mode G3 verification | Both shipped authoring surfaces (`ScenarioMapAuthoringWindow` X11 `0x4c0118b` + `ScenarioEditorShellHost` Play Mode `1477x721`) exercised live via Unity MCP (`:8081`) across success, `DRG-371` `GenericOnly` warnings, blocked/draft-save-allowed, and recovery→Play paths with **8 inspected PNG screenshots** (`s4-g3-*.png`) and `s4-g3-me-workflow-evidence.md` | PASS | no |

## Scored Criteria (0 = missing, 1 = partial, 2 = complete; Pass ≥ 9 / 12 — **Total: `12 / 12`**)

- **S1**: Clean per-track commits (`039531a3`, `730e2007`, plus QA commit), `0` compiler/review warnings, `unity-csharp-architect` `pr-finish.md` **PASS** verdict. (`2/2`)
- **S2**: Owner-only gates (`H6` Play Mode walk & PR submit/merge) explicitly preserved; `DRG-236` & `DRG-231` remain `hold:owner-go` and untouched. (`2/2`)
- **S3**: Architecture boundaries (`ADR-010 §2–3`, `ADR-007`, `ADR-001`, `ADR-017`) honored with `0` `UnityEngine` references in `UnityAdapter`, `0` `ProjectAegis.Sim` imports in runtime UI (`UnitySimImportForbidTests` pass), and no replacement hosts. (`2/2`)
- **S4**: All 3 Linear evidence comments posted (`DRG-371`, `DRG-346`, `DRG-274`) with exact test counts, file paths, evidence docs, `pr-finish.md` `PASS`, and `H6` owner-walk boundary note (statuses kept `Backlog`). (`2/2`)
- **S5**: Scope honesty — every claim empirically verified (`4,938/4,938` headless, `61/61` Unity PlayMode, `8/8` `Sprint4AuthoringPlayModeTests`, `0` Console errors, `0` Hard-No diff). (`2/2`)
- **S6**: Test quality — covers production composition & adversarial edge cases (`CATALOG_UNBOUND` on missing/unknown `dbRef`, failed `OpenDraft` session preservation, pre-validated `AssignToMissionId`, mixed capability error+warning without claiming Play allowed, `OnDisable` re-enable rewiring, pre-Awake null guards, `D26` unattached `NOT_AVAILABLE`). (`2/2`)

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`
**Skill:** unity-csharp-architect
**ADRs:** ADR-010 §2–3, ADR-007, ADR-001, ADR-017 (never ADR-018 for presentation)

**Verdict:** PASS

**Evidence:**
- Presentation reads: snapshot / projection / `BoundedAuthoringWorkflow` / `ScenarioEditorShellProjection` / `ScenarioExportGateState` / `PlayEntryState` only — no live ECS/sim session reads or `DecisionLog` writes.
- Command path: `ScenarioEditorShellHost` routes authoring mutations and Play handoff exclusively through `BoundedAuthoringWorkflow` (`ScenarioEditCommandBus` + `PlayEntrySession` + `DelegationBridgeHost.BindExternalSession`) — zero `IOrderSink.ApplyOrder` calls from UI.
- Assemblies: `0` new `.csproj` / `<ProjectReference>` or asmdef edges; `DelegationBridge.cs` hotpath & `SimulationSession.cs` `0` diff from `d25e017d`; `UnitySimImportForbidTests|NamespaceSccBaselineTests|CsprojReferenceGraphPinTests` (`31/31`) confirm `0` `using ProjectAegis.Sim` in `ScenarioEditorShellHost.cs` or new runtime scripts; `0` `UnityEngine` refs in `ProjectAegis.Delegation.UnityAdapter`.
- MB / DI / alloc: thin UITK host (`ScenarioEditorShellHost`), zero `Find*` / `Resources.Load`, cached UI elements, `OnDisable()` reset/unsubscription, `DetachAuthoringWorkflow()` on `OnDestroy()`, pre-Awake `_document == null` guards.
- Editor: `ScenarioMapAuthoringWindow` remains in `ProjectAegis.Unity.Editor` (`includePlatforms: ["Editor"]`), non-rewriting `GenericOnly` disclosure via `ScenarioExportGateState`, `OnGUI` absent.
- Tests: `dotnet test ProjectAegis.sln --no-build` (`4,938/4,938`, `0` warnings), `PlayModeSmokeHarnessTests` (`27/27`), `ReplayGolden` (`17/17`), `ScenarioEditorShellHostContractTests` (`3/3`), `ScenarioMapAuthoringPanelUiTests|ScenarioEditorActionAvailabilityTests` (`16/16`); live Unity `:8081` `ProjectAegis.Unity.Tests` (`61/61`, `Sprint4AuthoringPlayModeTests` `8/8`, `0` Console errors).
- Plugins: `netstandard2.1` Release builds (`0w/0e`) + `Assets/Plugins/ProjectAegis/` SHA-256 parity verified across all `ProjectAegis.*.dll` files.

**Waivers / N/A:** None (`H6`/`C10` owner Play Mode walk & PR submit/merge remain owner-only).

## Round Log

| Round | Date | Met | Not Met | Jev Scores | Notes |
|---|---|---|---|---|---|
| 0 | 2026-11-23 | Baseline (`d25e017d`) | Sprint 4 pending | Launch decision `gen-dec-1795431960-XxFmQstt8OQouMWMGBkD` | `two_disjoint_tracks_plus_evidence` (`0.99`), `hold_owner_go_s4` (`0.86`); branch `sprint/s4-h4-editors-g3` stacked on `sprint/s3-coord-ai-depth` |
| 1 | 2026-11-23 | **ALL 11/11 non-owner criteria `noul >= 0.80`** (`h1_c1_build_and_suite 0.86`, `h2_tracker_honesty 0.97`, `h3_c7_hard_no_surfaces 0.97`, `h4_c2_determinism 0.96`, `h5_c8_tdd_and_fences 0.87`, `h8_c3_c12_smoke_and_data 0.97`, `c4_c13_unity_playmode 0.97`, `c9_c11_live_mcp_ui 0.96`, `track_a_drg371_drg346 0.97`, `track_b_drg346_drg274 0.97`, `scored_s1_s6_pass 0.97`) | `H6`/`C10` (owner-only Play Mode walk & PR submit/merge) | `gen-dec-1795436779-EghqACMsaBRxFTk4RcP7` (`typesafe/jev-1.14-20261014`): `more_rounds = 0` (`P(0)=0.89`, `score=0.11`, `conf=0.82`), `sprint4_verdict = pass_owner_walk` (`0.97`, `conf=0.94`) | **SPRINT 4 AGENT DoD MET.** Saved to `jev-sprint4-dod-round1-request.json` and `jev-sprint4-dod-round1-decisions.json`; verified `4,938/4,938` headless, `61/61` Unity `:8081` PlayMode (`8/8` `Sprint4AuthoringPlayModeTests`, `0` Console errors), 8 inspected PNGs, Linear comments on `DRG-371`/`DRG-346`/`DRG-274` (`Backlog`), `S1`–`S6` (`12/12`), and atomic Track A (`039531a3`) + Track B (`730e2007`) commits. |
