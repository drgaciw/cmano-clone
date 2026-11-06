# DRG-345 — mission role capability disclosure (H4)

**Issue:** [DRG-345](https://linear.app/drgamtd-workspace/issue/DRG-345), parent DRG-327. **Date:** 2026-10-06. **Gates:** [ADR-017](../../architecture/adr-017-editor-topology-client-vs-scenario-lab.md) Accepted and [H4 scope boundary](../../../production/scenario-editor-phase2-gui-scope-boundary-2026-10-01.md) (commit `434b3cf6`). **Requirement:** proposed AME-6.11 in [requirements reconciliation](../specs/2026-09-30-requirements-planning-reconciliation.md). It stays labeled as proposed. This note does not claim owner acceptance or Editor visual evidence.

## Runtime truth

Authored missions are not consumed by any runtime directly. The only Export/Play backend is `baltic-replay-harness`, meaning `BalticReplayHarness` with `SimulationSession`. Its agents choose among the `PatrolCandidateEngagePolicy.Candidates` intents: `Hold`, `Move` and `Engage`. `MissionRoleCapabilityManifest` (`src/ProjectAegis.Data/Scenario/Authoring/`) lists the order kinds each role needs:

| Role | Required order kinds | Verdict |
| --- | --- | --- |
| Patrol | Hold, Move | Executed |
| Strike | Engage | Executed |
| Ferry | ReturnToBase | Not executed (`MISSION_ROLE_NOT_EXECUTED`) |
| Support / Tanker | Refuel (no such order exists) | Not executed |
| Support / AEW | SetSensors | Not executed |
| Support / EW | SetEwPosture | Not executed |
| Other type, Support with no or unknown role | — | `MISSION_ROLE_UNKNOWN` |
| Unknown backend id | — | `MISSION_EXECUTION_BACKEND_UNKNOWN` |

All capability findings have Error severity and are ordered by mission id. Each message says Export and Play are blocked, says Save remains available, and lists the supported roles. A supported mission emits no finding, so the existing golden report hashes do not change. This change does not implement refueling, AEW or EW.

## Acceptance → tests

| Acceptance clause | Evidence |
| --- | --- |
| Manifest evaluates authored role and execution backend | `MissionRoleCapabilityManifestTests.Assess_classifies_authored_role_against_default_backend` (10-row table), `Assess_role_matching_is_case_and_whitespace_insensitive`, `Unknown_backend_marks_every_mission_unknown_backend`, `Manifest_entries_are_stable_and_ordered` |
| Runtime-support truth is derived from what the runtime executes, not from the UI | `MissionRoleCapabilityProjectionTests.Manifest_execution_claims_match_the_orders_the_replay_harness_policy_generates` (re-derives each verdict from `PatrolCandidateEngagePolicy.Candidates`) |
| Supported roles proceed | `Supported_fixture_has_no_capability_findings_and_exports`, `ScenarioRoleCapabilityCliTests.scenario_validate_supported_fixture_has_no_capability_findings`, `SampleCompletePipelineTests` (Strike+Patrol completes after the unsupported missions are removed), `ValidationGoldenTests` (hashes unchanged) |
| Tanker/AEW/EW (and any unexecuted role) produce stable, actionable findings | `Findings_are_error_severity_with_stable_codes_and_actionable_messages` (exact Tanker message, codes, data keys), `AssessDocument_discloses_supported_and_unsupported_missions_in_mission_id_order` |
| Deterministic ordering | `Findings_are_deterministic_regardless_of_authored_mission_order` |
| Blocked before Export/Play | `Validation_engine_export_gate_and_prepare_all_block_unsupported_roles`, `ScenarioRoleCapabilityCliTests.scenario_simulate_sample_rejects_unsupported_role_before_running`, `MissionRoleCapabilityProjectionTests.TryEnterPlay_force_confirm_cannot_bypass_unsupported_role` |
| Force-confirm policy is unchanged for other errors | `TryEnterPlay_force_confirm_still_allows_non_capability_errors` |
| Saving an incomplete draft is not blocked | `Draft_with_unsupported_role_saves_and_reloads_while_export_stays_blocked`, `Save_succeeds_for_unsupported_role_while_export_gate_discloses_unsupported_execution` |
| Same findings in CLI and GUI | `ScenarioRoleCapabilityCliTests.scenario_validate_reports_manifest_capability_findings_and_exits_1` and `Gui_findings_match_cli_export_gate_findings_code_severity_and_message`. Both compare severity, code, mission id and message with `MissionRoleCapabilityManifest.EvaluateFindings` |
| UI projection | `Project_rows_mirror_manifest_assessments_one_to_one`, `Template_rows_disclose_that_tanker_template_is_not_executed` |
| Deterministic role-policy fixtures | `assets/data/scenarios/validation/role-capability-supported.json`, `role-capability-unsupported.json` |

## Surfaces

- **Headless:** `ScenarioValidationEngine` appends the manifest findings. The same findings therefore reach `scenario_validate`, `scenario_export`, `scenario_publish`, `scenario_simulate_sample`, `ScenarioExportCommand.Prepare`, `ScenarioDocumentEditor.LiveValidate` and `ScenarioEditCommandBus.RefreshFindings`.
- **GUI projection:** `ScenarioExportGateState` adds `UnsupportedExecutionFindings`, `HasUnsupportedExecution` and `PlayBlockingReason`; its constructor is unchanged. `EditModeController` adds `LastPlayBlockReason`. `MissionRoleCapabilityProjection` builds one row per mission and one per template. Binding these to `ScenarioMapAuthoringWindow` UXML is left to DRG-346 Editor work.
- **Behaviour change:** two CLI sample tests (`SampleCompletePipelineTests`, `ScenarioSimulateSampleCliTests` AC-5 ferry) used to assert that a Tanker + Ferry scenario completes simulate. They now assert rejection, because the backend never executed those roles.

## Not touched

`DelegationBridge`, `SimulationSession`, `CatalogWriteGate`, the Baltic v2/v3 replay goldens (hash `17144800277401907079` preserved), the authoring hosts and `MissionTemplateCatalog` are unchanged.
