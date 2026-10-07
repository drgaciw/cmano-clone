# S123 Play-entry Evidence Index (DRG-244)

**Sprint:** [S123 play-entry / loop honesty](../../sprints/sprint-123-play-entry.md) · **QA plan:** [qa-plan-sprint-122-127-rebaseline-2026-09-30.md](../qa-plan-sprint-122-127-rebaseline-2026-09-30.md) (S123 entry row)
**Branch:** `cursor/s123-play-entry-7fee` · **Base:** `origin/main` @ `434b3cf6cf0dc64ef285d47e6f78857779369636`
**Linear:** DRG-243 · DRG-246 · DRG-244 · feeds DRG-208 (interim walk)

> **Scope of this index.** It lists the **headless** entry-path evidence for the S123 golden path:
> Load Baltic → briefing → mode + side → Begin Execution → one tick.
> It feeds the owner's **DRG-208 interim walk (S123-10)**. It does **not** close DRG-208, does not record an
> owner walk result, and is not Editor golden-path capture. S123-10 is owner-only.

## Commits

| SHA | Content |
|-----|---------|
| `591b0244770efd09061e38e73f87f50a8d484f3f` | `PlayEntrySession` façade, presentation models, 51 headless tests (S123-01, -03, -04, -05, -06, -07, -11) |
| _commit carrying this file_ | DRG-244 evidence index (this document; see `git log -- production/qa/evidence/s123-play-entry-evidence-index.md`) |

Re-pin this table to the sprint-tip SHA when the branch merges (DoD: "DRG-244 index links entry evidence at the sprint tip SHA").

## Code under test

All new, under `src/ProjectAegis.Delegation.UnityAdapter/PlayEntry/` (no `UnityEngine`; `net8.0` + `netstandard2.1`):

| File | Role |
|------|------|
| `PlayEntrySession.cs` | Package list + load → Planning; non-mutating failed resolve; staged mode/side; gated Begin; Reset → Planning. Null or missing `metadata` returns `SCHEMA_ERROR` before `ScenarioPackage.FromDocument` (that method reads `Metadata.PolicyId`). |
| `PlayEntryContracts.cs` | `PlaySide`, `PlayEntryErrorCodes`, `PlayEntryResult`, `PlayEntryState`, `BeginExecutionGate` |
| `BriefingContentPresentation.cs` | Briefing panel model: package title/description + mission rows, or explicit missing-content state |
| `C2ModeSelectorPresentation.cs` | Top-bar mode selector: Human / Mixed / AvA → `SimulationModeKind.Human` / `Mixed` / `AgentVsAgent` |
| `PlaySidePickerPresentation.cs` | Side picker shown after mode selection |
| `BeginExecutionButtonPresentation.cs` | Begin button enabled state + blocked-reason label |

Mode application uses the existing façade `DelegationBridge.ConfigureSimulationMode` followed by `DelegationBridge.BeginExecution`, both called at Begin. Selection is staged until then because `SimulationModeConfigurator` begins execution immediately for AgentVsAgent, which would otherwise bypass the side gate. `DelegationBridge.cs` is unchanged.

## Entry-path tests

Project: `src/ProjectAegis.Delegation.UnityAdapter.Tests` · folder `PlayEntry/` · 54 tests (mode `[TestCase]` ×3, plus null/missing metadata `[TestCase]` ×2).

| Sprint row | Acceptance | Tests (`PlayEntrySessionTests` unless noted) |
|------------|------------|-------|
| S123-01 DRG-243 | Package list + load → Planning | `ListPackages_includes_available_baltic_patrol_package_in_deterministic_order`, `ListPackages_returns_empty_for_missing_directory`, `New_session_has_no_package_and_no_bridge`, `TryLoad_baltic_entry_enters_planning_with_package_bound_bridge`, `Successful_reload_replaces_bridge_and_clears_mode_and_side` |
| S123-01 DRG-243 | Failed resolve is non-mutating, explicit error | `Failed_resolve_from_empty_session_leaves_session_empty`, `Failed_resolve_missing_file_preserves_prior_planning_session` (`FILE_UNREADABLE`), `Failed_resolve_malformed_json_preserves_prior_session` (`SCHEMA_ERROR`), `Failed_resolve_null_or_missing_metadata_preserves_prior_session` (`SCHEMA_ERROR` for `"metadata": null` and for a missing metadata key; empty `metadata: {}` still loads via `Empty_metadata_object_still_resolves_default_policy`), `Failed_resolve_unknown_policy_preserves_prior_session` (`POLICY_UNRESOLVED`), `Failed_resolve_seed_outside_bridge_range_preserves_prior_session` (`SEED_UNSUPPORTED`), `Failed_resolve_unavailable_library_entry_preserves_prior_session` (`PACKAGE_UNAVAILABLE`), `Failed_resolve_during_execution_keeps_executing_session` |
| S123-03 W2-CORE-01 | Briefing content or explicit missing-content state | `PlayEntryPresentationTests`: `Briefing_without_package_reports_no_package_state`, `Briefing_for_baltic_package_shows_explicit_missing_content_and_existing_mission_rows`, `Briefing_binds_authored_title_and_description_when_present`, `Briefing_whitespace_description_is_missing_content_not_blank_text` |
| S123-04 DRG-246 MODE-01 | Human / Mixed / AvA via façade | `PlayEntryPresentationTests`: `Mode_selector_offers_human_mixed_ava_mapped_to_simulation_mode_enum`, `Mode_selector_enabled_in_planning_and_marks_selection`, `Mode_selector_locks_after_begin`; session: `Selecting_each_mode_stages_it_without_touching_the_bridge` (×3), `Mode_and_side_selection_require_a_loaded_package`, `Begin_human_mode_applies_human_friendly_agent_opposing_via_facade`, `Begin_mixed_friendly_applies_human_on_friendly_side_via_facade`, `Begin_mixed_opposing_applies_human_on_opposing_side_via_facade`, `Begin_agent_vs_agent_applies_agents_on_both_sides_via_facade` |
| S123-05 W2-MODE-01 | Side pick after mode | `OfferedSides_maps_each_simulation_mode`, `Side_pick_is_rejected_before_mode_selection`, `Side_not_offered_by_mode_is_rejected`, `Changing_mode_clears_side_the_new_mode_does_not_offer`, `Changing_mode_keeps_side_the_new_mode_still_offers`; `PlayEntryPresentationTests`: `Side_picker_hidden_until_mode_selected`, `Side_picker_after_human_mode_offers_friendly_only`, `Side_picker_after_mixed_mode_offers_both_sides_and_marks_pick`, `Side_picker_after_ava_mode_uses_observe_prompt` |
| S123-06 W3-MODE-01 | Cannot Begin without both | `Begin_gate_blocks_without_package`, `Begin_is_rejected_when_mode_and_side_are_both_missing`, `Begin_is_rejected_when_side_is_missing_after_mode_selection`, `Begin_is_rejected_for_agent_vs_agent_without_side`, `Begin_with_mode_and_side_transitions_to_executing_once`, `Selection_is_locked_after_begin`; `PlayEntryPresentationTests`: `Begin_button_disabled_with_reason_when_mode_and_side_missing`, `Begin_button_disabled_with_reason_when_side_missing`, `Begin_button_enabled_when_mode_and_side_selected`, `Begin_button_without_package_explains_load_first` |
| S123-07 W3-CORE-01 | Golden-path smoke | `PlayEntryGoldenPathSmokeTests`: `Golden_path_load_baltic_briefing_mode_side_begin_one_tick`, `Golden_path_is_deterministic_for_the_same_package` (order-log SHA-256 equal across runs), `Golden_path_leaves_baltic_replay_hash_invariant_untouched` (`BalticReplayHarness.Run(42, "baltic-patrol", 4).WorldHash == 17144800277401907079` after the play-entry path; golden file still pins it) |
| S123-11 W3-CORE-03 (Should) | Reset → Planning | `Reset_returns_executing_session_to_planning_with_fresh_bridge`, `Reset_without_package_is_rejected` |

## Commands and results (agent run, 2026-10-06, branch tip `591b0244`)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~ProjectAegis.Delegation.UnityAdapter.Tests.PlayEntry"
# Passed! Failed: 0, Passed: 54, Total: 54

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter PlayModeSmokeHarnessTests
# Passed! Failed: 0, Passed: 25, Total: 25

dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~PlayEntry|FullyQualifiedName~C2TopBar|FullyQualifiedName~Baltic|FullyQualifiedName~PlayModeSmoke|FullyQualifiedName~ReplayGolden"
# Passed! Failed: 0, Passed: 229, Total: 229
```

Full-solution gates (build, ≥1638 suite, ReplayGolden 6/6 in `ProjectAegis.Delegation.Tests`) are run serially by the orchestrator and are not claimed here.

## Known gaps (not covered by this index)

- **Unity host wiring.** No `C2TopBarPanelHost` / UXML / scenario-library host change binds these models yet; Editor golden-path capture is still required by the QA plan.
- **Briefing content.** The Baltic package (`data/scenarios/examples/baltic-patrol.scenario.json`) has no `metadata.description`, so the panel shows the explicit missing-content state. Fixture enrichment is S123-02 (Should, not done here).
- **Side labels.** The engine knows only friendly/opposing (`SimulationModeProfile.PlayerControlsFriendlySide`); picker labels are "Blue (friendly)" / "Red (opposing)" rather than scenario `sides[]` names. In AvA the pick is an observer perspective and does not change controller assignment.
- **Force registration.** `TryBeginExecution` takes friendly/opposing targets from the host (entity registration stays a host concern on `DelegationBridge.Registry`); the package ORBAT is not auto-spawned.
- **Slice A (S123-08 / DRG-183)** is not part of this branch.
