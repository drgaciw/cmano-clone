# Sprint 2 C8 / H5 — Consolidated RED→GREEN TDD, Architecture Fence & Unity Play Mode Evidence

Branch: `sprint/s2-c2-combat-ux` (stacked on `sprint/s1-hygiene-wiring` @ `d0fde4af`, 2026-11-22)

## Scope & Deliverable Reconciliation
- **7 Active Sprint 2 Stories:** `DRG-247`, `DRG-272`, `DRG-712`, `DRG-408`, `DRG-398`, `DRG-273`, `DRG-245`
- **2 Held Stories (`hold:owner-go`):** `DRG-268` (`EngageExplain` log-row link), `DRG-269` (`PendingApprovalQueue` auto-1× / DLZ countdown) — **0 implementation, 0 leakage**
- **Milestone File Counts (Reconciled vs `git status --porcelain -uall`):**
  - **M2 (`DRG-247`, `DRG-272`):** **18 created** (incl. 2 `.meta`) / **30 modified** *(note: `s2-m2-red-evidence.md` headers stated `16`/`29`; reconciled exact count is `18`/`30`)*
  - **M3 (`DRG-712`, `DRG-245`):** **14 created** (incl. 6 `.meta`) / **9 modified** (1 overlap with M2: `DelegationOrchestrator.cs`)
  - **M4 (`DRG-408`, `DRG-398`, `DRG-273`):** **31 created** (incl. 7 `.meta`) / **17 modified** (3 overlaps: `MessageLogProjection.cs`, `DelegationBridgeHost.cs` w/ M2; `C2TopBarPanelHost.cs` w/ M3)
  - **Union (Code/Test/Asset Deliverables):** `115` files across Commits 1–3 (`46` M2 + `21` M3 + `48` M4)

---

## 1. Milestone M2 — Verbatim RED Evidence (`DRG-247`, `DRG-272`)

Pre-implementation `dotnet build` against phase-A tests (`Command exited with code 1`):

```text
=== RED: Delegation.Tests ===
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs(131,51): error CS0117: 'AgentDirectiveAction' does not contain a definition for 'PauseAgent'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Input/AgentDirectiveIssuanceTests.cs(132,52): error CS0117: 'AgentDirectiveAction' does not contain a definition for 'ResumeAgent'
Build FAILED. 0 Warning(s), 2 Error(s)

=== RED: Delegation.UnityAdapter.Tests ===
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/PlayDelegation/PlayDelegationCommandsControlTests.cs(21,31): error CS1061: 'PlayDelegationCommands' does not contain a definition for 'TryPauseAgentActions'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/PlayDelegation/PlayDelegationCommandsControlTests.cs(23,27): error CS1061: 'AgentController' does not contain a definition for 'IsActionsPaused'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/PlayDelegation/PlayDelegationCommandsControlTests.cs(52,80): error CS0117: 'PlayDelegationErrorCodes' does not contain a definition for 'ReplayAttached'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeControlTests.cs(27,43): error CS0117: 'C2PlayerCommandBridge' does not contain a definition for 'TryTakeControl'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/C2PlayerCommandBridgeControlTests.cs(48,43): error CS0117: 'C2PlayerCommandBridge' does not contain a definition for 'TryPauseAgentActions'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/RightUnitControlProjectionTests.cs(18,21): error CS0103: The name 'RightUnitControlProjection' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/ContactOverrideCommandTests.cs(35,22): error CS0103: The name 'ContactOverrideCommand' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/ContactOverrideCommandTests.cs(115,27): error CS1061: 'SliceAContactFrame' does not contain a definition for 'IsReplayReadOnly'
Build FAILED. 0 Warning(s), 64 Error(s)

=== RED: Ai.Host.Tests ===
/home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/Evidence/EvidenceProjectorContactOverrideTests.cs(15,39): error CS0246: The type or namespace name 'ContactOverrideRecord' could not be found
/home/username01/cmano-clone/src/ProjectAegis.Ai.Host.Tests/Evidence/EvidenceProjectorContactOverrideTests.cs(36,62): error CS0117: 'EvidenceEntryKind' does not contain a definition for 'ContactOverride'
Build FAILED. 0 Warning(s), 4 Error(s)
```

---

## 2. Milestone M3 — Verbatim RED Evidence (`DRG-245`, `DRG-712`)

Pre-implementation `dotnet test` against phase-A tests (`Command exited with code 1`):

```text
=== RED: SimulationModeConfiguratorTests ===
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Orchestration/SimulationModeConfiguratorTests.cs(226,34): error CS1061: 'DelegationOrchestrator' does not contain a definition for 'SimulationMode'

=== RED: LoopPolicyChromeBinderTests & MapCueOverlayProjectionTests & MapCueOverlayHostContractTests ===
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/LoopPolicyChromeBinderTests.cs(78,31): error CS1061: 'LoopPolicyChromePresentation' does not contain a definition for 'ShowDualSideTestSandboxBanner'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/LoopPolicyChromeBinderTests.cs(97,89): error CS0117: 'LoopPolicyChromeBinder' does not contain a definition for 'TestSandboxBannerText'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/MapCueOverlayProjectionTests.cs(18,21): error CS0103: The name 'MapCueOverlayProjection' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/MapCueOverlayHostContractTests.cs(40,13): error CS0103: The name 'MapCueOverlayProjection' does not exist in the current context
```

Contract refinement during GREEN: `MapCueOverlayHostContractTests.MapCueOverlayPanel_uxml_and_uss_declare_required_elements_bounds_and_ignore_picking` initially failed `Expected: True / But was: False` because `xml.Descendants()` included `<ui:UXML>`; fixed to `xml.Root!.Descendants()`.

---

## 3. Milestone M4 — Verbatim RED Evidence (`DRG-408`, `DRG-398`, `DRG-273`, Iterations 1 & 2)

### Iteration 1 RED (`Command exited with code 1`):
```text
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageLogNotificationCoordinatorTests.cs(17,31): error CS0246: The type or namespace name 'MessageLogNotificationCoordinator' could not be found
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageLogNotificationCoordinatorTests.cs(28,51): error CS0103: The name 'ClockStopAction' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageCategoryPolicyTests.cs(37,22): error CS0103: The name 'MessageCategoryPolicy' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageLogAttributionAndDeepLinkTests.cs(41,30): error CS1061: 'MessageLogLine' does not contain a definition for 'ActorId'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageLogAttributionAndDeepLinkTests.cs(114,22): error CS0103: The name 'DecisionLogDeepLinkProjection' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/TimeCompressionSafeStopTests.cs(60,35): error CS0117: 'C2ClockCommand' does not contain a definition for 'EvaluateAndApplyNotificationStop'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogDeepLinkBridgeTests.cs(35,44): error CS0117: 'MessageLogBridge' does not contain a definition for 'InspectDecisionLogLive'
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Presentation/MapBalloonOverlayProjectionTests.cs(22,21): error CS0103: The name 'MapBalloonOverlayProjection' does not exist in the current context
```

### Iteration 2 RED (Challenger Remediation — `MessageLogAlertBannerProjection` & `MessageLogRowWindow.Select` pre-filtering, `Command exited with code 1`):
```text
/home/username01/cmano-clone/src/ProjectAegis.Delegation.Tests/Projection/MessageLogNotificationCoordinatorTests.cs(246,27): error CS0103: The name 'MessageLogAlertBannerProjection' does not exist in the current context
/home/username01/cmano-clone/src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/MessageLogRowWindowTests.cs(76,41): error CS1501: No overload for method 'Select' takes 3 arguments
```

---

## 4. Milestone M5 — Unity PlayMode Compile RED→GREEN, Live `:8081` Verification & `UatHarness.cs` Sweep

### Unity Compile RED → GREEN & Polish Fixes
- **RED (`unity-mcp-cli compile` after `copy-delegation-assemblies.sh`):**
  - `Sprint2NotificationPolicyPlayModeTests.cs`, `Sprint2MessageLogDeepLinkPlayModeTests.cs`, `Sprint2TopBarSandboxPlayModeTests.cs`, and `Sprint2TimeCompressionSafeStopPlayModeTests.cs` failed Unity compile (`CS0246: The type or namespace name 'ISimWorldSnapshot'/'IOrderSink'/'EntityKey' could not be found` due to missing `using ProjectAegis.Delegation.UnityAdapter.Bridge;`), and `Sprint2NotificationPolicyPlayModeTests.cs` imported `ProjectAegis.Sim.*` which `ProjectAegis.Unity.Tests.asmdef` excludes (`overrideReferences: true`).
  - Unity Console flagged `Unknown property 'picking-mode'` USS parser warnings in `MapBalloonOverlayPanel.uss` / `MapCueOverlayPanel.uss`.
- **GREEN Fix:**
  - Added `using ProjectAegis.Delegation.UnityAdapter.Bridge;` to all four PlayMode test files and replaced `using ProjectAegis.Sim.*` in `Sprint2NotificationPolicyPlayModeTests.cs` with reflection over `Orchestrator.ScenarioPolicy` (`ProjectAegis.Unity.Tests.asmdef` keeps zero `ProjectAegis.Sim.dll` reference, preserving `pr-finish.md` §2.3).
  - Removed invalid `picking-mode: ignore;` from `MapCueOverlayPanel.uss` and `MapBalloonOverlayPanel.uss` (retaining `picking-mode="Ignore"` in UXML and `PickingMode.Ignore` in C#), and updated `MapCueOverlayHostContractTests` / `MapBalloonOverlayHostContractTests` to assert `Does.Not.Contain("picking-mode:")`.
  - Applied `MessageLogPanelHost.cs` `RefreshAlertBanner()` & `ApplyChromeCollapse()` polish so un-collapsing `MessageLog` restores `_alertBanner.style.display = DisplayStyle.Flex` when active rows exist.
  - `npx --yes unity-mcp-cli@1.13.0 compile`: `✔ Scripts compile (4 warning(s))` (only pre-existing `CS8600` in `MapSymbolPoolTests.cs`; `0` errors, `0` Sprint-2 USS warnings).

### Live Unity PlayMode Runner (`ProjectAegis.Unity.Tests`) — 43/43 PASSED (0 Failed, 0 Skipped, `3.41s`)
- **Sprint 2 PlayMode Suites (16/16 PASSED):**
  - `Sprint2UnitControlPlayModeTests` (`3/3`): `Right_unit_panel_take_control_pause_resume_and_return_to_agent_in_play_mode`, `Right_unit_panel_disables_control_buttons_with_reason_under_attach_replay_viewer`, `Right_unit_panel_clears_control_feedback_on_selection_change`
  - `ContactOverridePlayModeTests` (`3/3`): `Reclassify_button_updates_override_status_and_map_symbol_via_bridge_host`, `Drop_button_marks_contact_dropped_even_while_sim_paused`, `AttachReplayViewer_disables_override_controls_and_shows_reason_with_zero_log_writes`
  - `MapCueOverlayPlayModeTests` (`2/2`): `Companion_overlay_renders_stale_datalink_icons_and_queued_order_badge_with_ignore_picking`, `Selection_switch_clears_previous_contact_and_unit_badges_in_play_mode`
  - `Sprint2TopBarSandboxPlayModeTests` (`2/2`): `Top_bar_shows_test_sandbox_banner_iff_dual_side_policy_and_mixed_mode`, `Top_bar_hides_test_sandbox_banner_for_standard_baltic_mixed_mode`
  - `Sprint2NotificationPolicyPlayModeTests` (`3/3`): `MapBalloonOverlay_renders_balloons_with_ignore_picking_and_single_dismiss_keeps_second_pause`, `ReplayViewer_attached_blocks_policy_edits_and_suppresses_fogged_alerts`, `NonPause_MapBalloons_surface_in_alert_banner_for_dismiss_and_muted_FUEL_filters_before_windowing`
  - `Sprint2TimeCompressionSafeStopPlayModeTests` (`2/2`): `SimplePlayModeSimHost_safe_stops_at_8x_on_live_damage_event`, `TopBar_preserves_sandbox_banner_and_disables_clock_under_AttachReplayViewer`
  - `Sprint2MessageLogDeepLinkPlayModeTests` (`1/1`): `Selecting_attributed_row_populates_non_mutating_DecisionLog_deeplink_drawer`
- **Sprint 1 Regression PlayMode Suites (27/27 PASSED):** `C2DelegationSmokeTests` (2), `CombatMapViewTests` (2), `CommandReviewViewTests` (1), `CoverageMapViewTests` (5), `EscortMissionPlayModeTests` (2), `MapSymbolPoolTests` (3), `RightUnitPanelCommandFeedbackTests` (2), `SliceAContactPanelSmokeTests` (3), `SmokeTaskGroupCoveragePlayModeTests` (2), `Sprint1UiWiringPlayModeTests` (3), `UnitOrderToolbarIssuedStatusPlayModeTests` (2).

### Interactive `DelegationSmoke.unity` `UatHarness.cs` Sweep & Screenshots
- `UatHarness.SpawnMissing()` spawned `UAT_MapCueOverlayPanelHost(order=6)` and `UAT_MapBalloonOverlayPanelHost(order=7)` cleanly without modifying `DelegationSmoke.unity`.
- Verified live state transitions via `script-execute` + `UatHarness`:
  - `DRG-247`: `Take Control` (`Agent a-play (Active)` -> `Human (Direct)`), `Return to Agent` (`Human (Direct)` -> `Agent a-play (Active)`), `Pause Agent` (`Agent a-play (Active)` -> `Agent a-play (Paused)`), `Resume Agent` (`Agent a-play (Paused)` -> `Agent a-play (Active)`), `UatHarness.Sweep("RightUnitDetail")` 0 errors (`s2-drg247-unit-control.png`).
  - `DRG-272`: `contact-reclassify-dropdown=Neutral` -> `OVERRIDE: RECLASSIFIED → Neutral`, `contact-drop-button` -> `OVERRIDE: DROPPED (operator)` (`s2-drg272-contact-override.png`).
  - `DRG-712` + `DRG-245`: `test-sandbox-banner` hidden (`None`) in standard `Mixed` mode -> visible (`Flex / TEST SANDBOX`) under `allowDualSideControl=true`; `UAT_MapCueOverlayPanelHost` renders `DL` contact cue (`dl-hostile-1`) over `MapPlaceholder` (`s2-drg712-drg245-cues-sandbox.png`).
  - `DRG-408` + `DRG-398` + `DRG-273`: `8x` (`TIME: PAUSED (8x)`) safe-stopped to `TIME: PAUSED (1x)` on live `DAMAGE` event (`HasPause=True`); `message-log-policy-toggle` opened category rules drawer; `message-log-collapse-toggle` collapse/expand verified `BannerBeforeCollapse=Flex -> BannerWhileCollapsed=None -> BannerAfterExpand=Flex`; `UAT_MapBalloonOverlayPanelHost` rendered `[PAUSE] DAMAGE · u1`, `[ALERT] KILL_CONFIRMED`, `[PAUSE] CONTACT` balloons; selecting log row `SEQ #994` opened non-mutating deep-link drawer (`Source: Controller · Actor: a-play · Unit: u-agent`) (`s2-drg408-398-273-policy-safestop-deeplink.png`).
- Exited Play Mode cleanly (`isPlaying = false`); Unity Console `rql-query` post-fix: **`0` Errors, `0` Exceptions, `0` Sprint-2 USS Warnings**.

---

## 5. Post-Final-Edit `.NET 11` & Hard Invariant Outputs (Verbatim)

```text
=== dotnet format --verify-no-changes (touched M5 .cs files) ===
Exit code 0 (0 formatting violations)

=== dotnet build ProjectAegis.sln --no-restore --disable-build-servers -p:UseSharedCompilation=false ===
Build succeeded.
    0 Warning(s)
    0 Error(s)

=== dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -c Release -f netstandard2.1 ===
Build succeeded.
    0 Warning(s)
    0 Error(s)

=== dotnet test ProjectAegis.sln -v minimal --no-build --disable-build-servers ===
Passed!  - Failed:     0, Passed:    18, Skipped:     0, Total:    18 - ProjectAegis.Ai.Contracts.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:    15, Skipped:     0, Total:    15 - ProjectAegis.Delegation.Demo.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:   652, Skipped:     0, Total:   652 - ProjectAegis.Sim.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:    80, Skipped:     0, Total:    80 - ProjectAegis.Ai.Host.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:  1524, Skipped:     0, Total:  1524 - ProjectAegis.Delegation.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:  1368, Skipped:     0, Total:  1368 - ProjectAegis.Delegation.UnityAdapter.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:    24, Skipped:     0, Total:    24 - ProjectAegis.Data.Excel.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:   168, Skipped:     0, Total:   168 - ProjectAegis.MissionEditor.Cli.Tests.dll (net11.0)
Passed!  - Failed:     0, Passed:   951, Skipped:     0, Total:   951 - ProjectAegis.Data.Tests.dll (net11.0)
Total Passed: 4,800, Failed: 0, Skipped: 0

=== ReplayGolden + PlayModeSmokeHarnessTests ===
Passed!  - Failed:     0, Passed:    44, Skipped:     0, Total:    44 - ProjectAegis.Delegation.UnityAdapter.Tests.dll (17/17 ReplayGolden + 27/27 PlayModeSmokeHarnessTests)

=== Architecture Fences ===
Passed!  - Failed:     0, Passed:    61, Skipped:     0, Total:    61 - ProjectAegis.Data.Tests.dll (CsprojReferenceGraphPinTests|NamespaceSccBaselineTests|UnitySimImportForbidTests|AiHostBoundaryGateTests)

=== Meta / UI Asset Integrity ===
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4 - ProjectAegis.Delegation.Tests.dll
Passed!  - Failed:     0, Passed:     2, Skipped:     0, Total:     2 - ProjectAegis.Delegation.UnityAdapter.Tests.dll

=== Hard-No & Data Invariants ===
git diff main -- DelegationBridge.cs WriteGate/ SimulationSession.cs GlobeMapProductHost.cs assets/data/catalog/ tests/regression/ -> 0 lines
git diff d0fde4af -- DelegationBridge.cs WriteGate/ SimulationSession.cs GlobeMapProductHost.cs MapPlaceholderPanelHost.cs assets/data/catalog/ tests/regression/ DelegationSmoke.unity DelegationSmokeSceneBuilder.cs -> 0 lines
sha256sum assets/data/catalog/aegis_public_corpus.db -> 82d262129d41c6fbb78fa636a3c2923471dedf37db071f7a4e7c99bdec4760fd
sha256sum assets/data/catalog/baltic_patrol.db       -> 75ea357a22de185808c4097b3ab1ddfcd836a4da37635576c0816b514d0b88ab
grep -rn "17144800277401907079" tests/ data/ | wc -l -> 26
```
