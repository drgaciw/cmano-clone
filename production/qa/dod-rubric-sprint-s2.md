# Unified Sprint 2 Definition of Done Rubric (Jev-Gated)

**Sprint:** Sprint 2 — Core C2 Combat UX & Player Agency (`sprint/s2-c2-combat-ux`)
**Base Commit:** `d0fde4af` (`sprint/s1-hygiene-wiring` — Sprint 1 Agent DoD MET: 4,706/0 headless, 27/27 Unity PlayMode, Baltic v2 hash `17144800277401907079` intact)
**Sprint 2 Atomic Code Commits:** `960007df` (M2: `DRG-247`, `DRG-272`), `da173102` (M3: `DRG-712`, `DRG-245`), `738c6c1c` (M4: `DRG-408`, `DRG-398`, `DRG-273`)
**Kickoff Jev Decision:** `gen-dec-1795370075-cYxFt5CkjW7ba5VJ5x9I` (2026-11-22, Round 0)
**Unified Standard:** Combines live Notion **Project Aegis Rubrics (DRAFT v1)** (`H1`–`H8` + `S1`–`S6 ≥ 9/12`), Play Mode UAT (`C1`–`C13`), and Sprint 2 Jev gates (`C14`–`C17`).
**Pass Rule:** Every non-owner mechanical Hard Gate passes, every non-owner Jev `noul ≥ 0.80`, Scored `S1`–`S6 ≥ 9/12`, and `more_rounds = 0`. **Script-first:** Jev never overrides a failed mechanical gate.

## Scope (7 Non-Held Linear Issues across 3 Milestones; `DRG-268` & `DRG-269` Held)

*(Note: Round 0 kickoff draft referenced `DRG-255`; Sprint 2 execution scope per `ORIGINAL_REQUEST.md` / `PROJECT.md` comprises the 7 active issues below, replacing `DRG-255` with `DRG-398` + `DRG-245` while keeping `DRG-268` and `DRG-269` under `hold:owner-go`.)*

| Milestone / Track | Issues | Surface |
|---|---|---|
| M2 — Controls & Contact Overrides (`960007df`) | `DRG-247`, `DRG-272` | `Delegation/Controllers/**`, `Delegation/Orchestration/DelegationOrchestrator.cs`, `Delegation/Input/InputCommandInventory.cs`, `Delegation/Decision/ContactOverrideRecord.cs`, `Ai.Contracts`/`Ai.Host` `EvidenceProjector`, `Delegation.UnityAdapter/Bridge/C2PlayerCommandBridge*` / `ContactOverrideCommand*`, `RightUnitPanelHost`, `ContactDetailPanelHost` + `*Tests` |
| M3 — Map Cues & Sandbox Banner (`da173102`) | `DRG-712`, `DRG-245` | `Delegation/Orchestration/SimulationModeConfigurator.cs`, `Delegation.UnityAdapter/Presentation/MapCueOverlayProjection*` / `LoopPolicyChromeBinder*`, companion `MapCueOverlayPanelHost` (`UIDocument` `sortingOrder=6`), `C2TopBarPanel.uxml`/`.uss` + `*Tests` |
| M4 — Notification Policy, Safe-Stop & Deep-Link (`738c6c1c`) | `DRG-408`, `DRG-398`, `DRG-273` | `Delegation/Projection/MessageCategoryPolicy*` / `MessageLogNotificationCoordinator*` / `DecisionLogDeepLinkProjection*`, `data/scenarios/message-category-policy.default.json`, `Delegation.UnityAdapter/Bridge/C2ClockCommand.cs` / `MessageLogBridge.cs`, `Gameplay/FixedStepFrameRunner.cs`, companion `MapBalloonOverlayPanelHost` (`sortingOrder=7`), `MessageLogPanelHost`, `C2TopBarPanelHost`, `DelegationBridgeHost`, `SimplePlayModeSimHost` + `*Tests` |

## Held / Off-Limits Items (`hold:owner-go` — 0 Lines Implemented)
- **`DRG-268` (`hold:owner-go`):** Message-log click → map/OOB select + open `DRG-168` `EngageExplain` surface (0 wiring).
- **`DRG-269` (`hold:owner-go`):** `PendingApprovalQueue` auto-1× interrupt + DLZ countdown (0 wiring).

## Hard Gates (All Must Be YES)

| ID | Gate | Mechanical Check | Owner-Only |
|---|---|---|---|
| H1 / C1 | Zero-warning build & full headless suite green | `dotnet build ProjectAegis.sln` (0 warnings, 0 errors); `Release/netstandard2.1` (0 warnings, 0 errors); `dotnet test ProjectAegis.sln` (`4,800/4,800` passed, 0 failures) | no |
| H2 | Tracker honesty | Linear evidence comments posted (`save_comment`) on all 7 active issues (`e0ffd6ad`, `7eda1128`, `af5074e2`, `b790c762`, `57cff66d`, `0d5079ff`, `102b8a68`); all statuses remain `Backlog` (never `Done` before PR merge) | no |
| H3 / C7 | Hard-no surfaces untouched | 0 diff on `DelegationBridge.cs` hotpath, `WriteGate/`, `SimulationSession.cs`, `GlobeMapProductHost.cs`, `MapPlaceholderPanelHost.cs`, `DelegationSmoke.unity`, `DelegationSmokeSceneBuilder.cs`, `tests/regression/**` | no |
| H4 / C2 | Determinism preserved | `ReplayGolden` `17/17` pass; Baltic v2 hash `17144800277401907079` unchanged (`26` occurrences) | no |
| H5 / C8 | Strict TDD RED→GREEN + source-fence tests | Pre-fix RED failure outputs + GREEN + arch fences (`61/61`) + PlayMode results recorded in `production/qa/evidence/sprint-2/s2-c8-red-evidence.md` | no |
| H6 / C10 | Owner Play Mode walk preserved | Agent supplies fresh Play Mode MCP screenshots + evidence; owner sign-off & PR submit/merge remain owner-only | **yes** |
| H8 / C3 / C12 | Smoke floor & shipped data integrity | `PlayModeSmokeHarnessTests` `27/27`; `sha256sum assets/data/catalog/*.db` byte-identical (`82d26212...`, `75ea357a...`) | no |
| C4 / C13 | Unity Console & PlayMode Runner | `0` Unity Console `Error`/`Exception` entries & `0` Sprint-2 USS parser warnings; `ProjectAegis.Unity.Tests` PlayMode **43/43 PASSED** (`0` failures) | no |
| C9 / C11 | Live Unity MCP interactive verification | All 7 Sprint 2 features exercised live in `DelegationSmoke.unity` (`:8081`) via `UatHarness.cs` + `script-execute` with 4 screenshots in `production/qa/evidence/sprint-2/` | no |
| C14 | Controller handoff invariants (`DRG-247`) | Single active controller on take/return, agent pause distinct from sim pause & direct control, task retention on pause/resume, group detach/rejoin, `DRG-348` `InputCommandInventory` + `adr-029` registration, 0 `DelegationBridge`/`SimulationSession` edits | no |
| C15 | Replay mutation refusal (`DRG-247/272/408/398/273`) | `AttachReplayViewer` / replay mode refuses handoff, contact-override, message-policy edit, notification pause/1x, and time-compression mutations with zero order-log writes; deep-link inspection stays strictly read-only | no |
| C16 | Fog & selection hygiene (`DRG-712/408`) | Selection switches clear stale contact/queued-order badges (`DRG-712`); notification alerts source strictly from `MessageLogBridge.ProjectLive` with zero fog-of-war leaks (`DRG-408`) | no |
| C17 | Safe-stop determinism & Sandbox banner (`DRG-398/245`) | `DRG-398` consumes `DRG-408` shared coordinator (no second policy system) with deterministic `1x`/`2x`/`4x`/`8x` safe-stop parity (`0` diff on `SimulationSession.cs`); `TEST SANDBOX` banner visible iff `AllowDualSideControl && SimulationModeKind.Mixed` (`DRG-245`) | no |

## Scored Criteria (0 = missing, 1 = partial, 2 = complete; Pass ≥ 9 / 12)

- **S1**: Clean diff (`960007df`, `da173102`, `738c6c1c`), 0 compiler/review warnings, `unity-csharp-architect` `pr-finish.md` **PASS** verdict.
- **S2**: Held/deferred items explicitly ticketed (`DRG-268`, `DRG-269` `hold:owner-go` untouched; owner PR submit & Play Mode walk `H6`).
- **S3**: ADR / inventory / doc status updated (`adr-029-h6-save-resume.md`, `ADR-010`, `ADR-007`, `ADR-001`, `ADR-030`, `DRG-348` `InputCommandInventory`).
- **S4**: All 7 Linear evidence comments include branch, commit SHAs, test counts, replay hash, RED evidence, and Play Mode evidence.
- **S5**: Scope honesty — no claim exceeds verified execution (`4,800/4,800` headless, `43/43` Unity PlayMode, `0` Console errors).
- **S6**: Test quality — exercises production composition & edge cases (replay refusal, fog filtering, `8x` safe-stop, selection leak, collapse/expand alert banner).

## unity-csharp-architect — PR finish (UCA-M4)

**Checklist:** `production/agentic/skills/unity-csharp-architect/checklists/pr-finish.md`
**Skill:** unity-csharp-architect
**ADRs:** ADR-010, ADR-007, ADR-001 (plus ADR-029, ADR-030 — never ADR-018 for presentation)

**Verdict:** PASS

**Evidence:**
- Presentation reads: snapshot / `*Projection` / `*Binder` / `*Bridge` / `IReadOnly*` only (`RightUnitControlProjection`, `ContactOverrideProjection`, `MapCueOverlayProjection`, `LoopPolicyChromeBinder`, `MapBalloonOverlayProjection`, `MessageCategoryPolicyBinder`, `DecisionLogDeepLinkBinder`); companion overlays (`MapCueOverlayPanelHost` `order=6`, `MapBalloonOverlayPanelHost` `order=7`) use `PickingMode.Ignore` and keep `MapPlaceholderPanelHost.cs` / `GlobeMapProductHost.cs` at `0` diff.
- Command path: `C2PlayerCommandBridge`, `PlayDelegationCommands`, `ContactOverrideCommand`, `C2ClockCommand`, `DelegationBridgeHost` façades only; zero `IOrderSink.ApplyOrder` or direct `DecisionLog` writes from MonoBehaviours; `DecisionLogDeepLinkProjection.InspectLive/Replay` is strictly read-only.
- Assemblies: zero new `.csproj` / `<ProjectReference>` (`CsprojReferenceGraphPinTests`, `NamespaceSccBaselineTests`, `UnitySimImportForbidTests` (frozen at 2 hosts), `AiHostBoundaryGateTests`: `61/61` PASS); `ProjectAegis.Unity.Tests.asmdef` keeps zero `ProjectAegis.Sim.dll` reference; `DelegationBridge.cs` hotpath & `SimulationSession.cs` `0` diff vs `main`.
- MB / DI / alloc: thin hosts (`RightUnitPanelHost`, `ContactDetailPanelHost`, `C2TopBarPanelHost`, `MessageLogPanelHost`, `MapCueOverlayPanelHost`, `MapBalloonOverlayPanelHost`); zero `Find*`/`Resources.Load`; pooled map cue & balloon elements; `RefreshAlertBanner()`/`ApplyChromeCollapse()` signature-gated.
- Editor: N/A for runtime hosts (no new EditorWindow; `DelegationSmoke.unity` & `DelegationSmokeSceneBuilder.cs` `0` diff).
- Tests: `dotnet test ProjectAegis.sln -v minimal` (`4,800/4,800` passed, `0` warnings), `ReplayGolden` (`17/17`), `PlayModeSmokeHarnessTests` (`27/27`), `ProjectAegis.Unity.Tests` live PlayMode (`43/43` passed via `:8081`).
- Plugins: `Release / netstandard2.1` built (`0` warnings, `0` errors) and `15` DLLs refreshed into `unity/ProjectAegis/Assets/Plugins/ProjectAegis/` via `bash tools/copy-delegation-assemblies.sh` (gitignored, zero `net8.0` DLLs).

**Waivers / N/A:** None (all new UI hosts strictly follow ADR-010/007/001; `DRG-268` and `DRG-269` remain `hold:owner-go`).

## Round Log

| Round | Date | Met | Not Met | Jev Scores | Notes |
|---|---|---|---|---|---|
| 0 | 2026-11-22 | Baseline (`d0fde4af`) | Sprint 2 pending | `gen-dec-1795370075-cYxFt5CkjW7ba5VJ5x9I` (`typesafe/jev-1.14-20261014`) | Kickoff draft (`DRG-255` later replaced by `DRG-398` + `DRG-245` per `ORIGINAL_REQUEST.md`). |
| 1 | 2026-11-22 | 9/12 `noul >= 0.80` (`sprint2_verdict pass_owner_walk 0.97`, `scored_s1_s6_pass 0.96`) | `h1_c1 0.74`, `c15 0.59`, `c16 0.70`, `more_rounds 0.74` | `gen-dec-1795376894-pxH7SSftfPg5oay0cR6N` (`typesafe/jev-1.14-20261014`) | Archived to `jev-sprint2-dod-round1-*.json`; lacked 9-assembly breakdown and itemized `C15`/`C16` test citations. |
| 2 | 2026-11-22 | **ALL 12/12 non-owner criteria `noul >= 0.80`** (`h1_c1 0.88`, `h3_h4 0.98`, `h5_c8 0.91`, `c4_c13 0.97`, `c9_c11 0.93`, `c12 0.97`, `c14 0.97`, `c15 0.93`, `c16 0.90`, `c17 0.98`, `h2_h6 0.93`, `scored_s1_s6_pass 0.89`) | H6/C10 (owner-only walk & PR submit/merge) | `gen-dec-1795376953-TUpzu0ykqSwplX4LrGEQ` (`typesafe/jev-1.14-20261014`): `more_rounds 0 (P(0)=0.81, score=0.21)`, `sprint2_verdict pass_owner_walk (0.98)` | **SPRINT 2 AGENT DoD MET.** Combined itemized 9-project `4,800/4,800` breakdown, verbatim RED->GREEN (`s2-c8-red-evidence.md`), `pr-finish.md` PASS, `C14`–`C17` test proof, 7 Linear comments (`H2`), `43/43` Unity PlayMode + `4` screenshots, `DRG-268`/`DRG-269` held. |
