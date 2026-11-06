# DRG-165 — Combat event projection contract: gap audit

**Date:** 2026-10-06
**Epic:** DRG-184 (Combat UX Slice B)
**Branch:** `cursor/drg-165-combat-event-contract-7fee`
**Baseline:** DRG-211 headless contract (`src/ProjectAegis.Delegation/CombatEvents/`), DRG-215 explain contract
(`EngageExplainContract/`), DRG-183 Slice A targetability harness (7eaad8c2), DRG-169 contact combat card (9cd872c7).
**Scope:** headless only. No Unity Editor evidence and no owner acceptance are claimed here.

## Method

I audited each acceptance criterion against the code and tests on `origin/main`, then implemented only the gaps.
Status values: **MET (pre-existing)**, **MET (this change)**, **N/A** (with justification).

## Criterion-by-criterion audit

### 1. Emits intent accepted, authorization/refusal, firing/launch, sustained or in-flight state where applicable, and terminal outcome

| Sub-criterion | Evidence | Status |
|---|---|---|
| Intent accepted | `CombatEventPhase.IntentAccepted`; `CombatEventProjectionTests.Permitted_path_emits_intent_authorized_firing_and_terminal_outcome`; `CombatEventLogProjectionTests.Build_uses_enriched_log_facts_for_complete_terminal_leg` | MET (pre-existing) |
| Authorization | `CombatEventPhase.Authorized`, emitted only on affirmative preview or a launched log row; `Null_preview_without_log_evidence_omits_authorized` | MET (pre-existing) |
| Refusal (never silent) | `CombatEventPhase.AuthorizationRefused` from policy denial, preview abort, resolver abort; `Refused_path_emits_explicit_*`, `Build_refusal_uses_row_sequence_as_zero_engagement_correlation` | MET (pre-existing) |
| Refusal from Slice A targetability | New: a withheld Slice A row, or a supplied Slice A snapshot with no row for the target, refuses with the named cause (`targetability:<cause>`). `CombatEventContractTests.Assess_withheld_slice_a_row_emits_named_targetability_refusal_even_when_preview_can_fire`, `Assess_supplied_slice_a_without_target_row_fails_closed_as_missing_provenance`, `Assess_policy_denial_outranks_slice_a_withheld_cause` | MET (this change) |
| Firing / launch | `CombatEventPhase.Firing` with outcome `Launch` | MET (pre-existing) |
| In-flight | `CombatEventPhase.InFlight`: assess path when launched without outcome, log path only for `Missile` family; `Launch_without_outcome_emits_in_flight`, `Build_does_not_attach_future_or_prior_outcome_and_only_missile_infers_in_flight` | MET (pre-existing) |
| Sustained | The simulation has no sustained-fire concept (`rg Sustain src` returns nothing). The gun resolver records launch and outcome rows; it emits no persistent fire state. "Where applicable" is satisfied by `InFlight`. Adding a phase with no sim source would invent a fact. | N/A |
| Terminal outcome | `CombatEventPhase.TerminalOutcome` with `outcome:<code>` explanation ref | MET (pre-existing) |

### 2. Includes shooter, target, weapon family, outcome, correlation ID, simulation time, and explanation reference

| Field | Evidence | Status |
|---|---|---|
| Shooter, target, weapon family, outcome, correlation ID, sim time (+ sim tick), explanation reference | `CombatEvent` record (`CombatEventTypes.cs`); asserted field-by-field in `Permitted_path_emits_intent_authorized_firing_and_terminal_outcome`; legacy rows get explicit `unknown-target` / `Unknown` in `Build_projects_explicit_unknowns_for_legacy_rows` | MET (pre-existing) |
| Execution facts per correlation (firing solution at execution, salvo) | New `CombatExecutionFact` on `CombatEventSnapshot.Execution`; `Assess_launched_leg_carries_execution_fact_for_correlation`, `Log_build_projects_execution_facts_keyed_by_log_correlation_in_sequence_order` | MET (this change) |

### 3. Remains replay-stable and is the only combat-fact source for presentation

| Sub-criterion | Evidence | Status |
|---|---|---|
| Replay-stable ordering + fingerprint | Ordinal / sequence ordering; `CombatEventFingerprint` (invariant culture, no wall clock). `Fingerprint_is_identical_for_identical_inputs` (pre-existing); new `CombatEventContractTests.Replay_same_inputs_produce_identical_ordered_events_and_fingerprint` builds the log and Slice A snapshot twice from scratch and checks that events, facts, and fingerprint are identical; `SliceBCombatScenarioTests` (same seed gives the same fingerprint) | MET |
| Fingerprint covers new facts without breaking event-only shape | The `|ta=` / `|ex=` segments are appended only when present. `Fingerprint_covers_targetability_and_execution_facts` | MET (this change) |
| Baltic v2 replay hash `17144800277401907079` | No sim / tick-path change; ReplayGolden + PlayModeSmoke filter 42/42 green | MET |
| Only combat-fact source for presentation | **Gap found:** `CombatPresentationFrameBridge.Build` built `CombatPresentationFrame.Explanations` (fire-control track at execution, salvo) straight from `DecisionLog.Engagements`. That made it a second combat-fact path beside `CombatEventLogProjection`. **Fix:** explanations now map 1:1 from `CombatEventSnapshot.Execution`. `CombatPresentationFrameBridgeTests.Explanations_are_sourced_from_combat_event_execution_facts` + source fence `Frame_bridge_reads_combat_facts_only_through_the_combat_event_contract` (no `.Engagements` / `.EngagementOutcomes` reads in the bridge) | MET (this change) |

### 4. Consumes the authoritative Slice A targetability state and does not rebuild sensor, weapon-envelope, or datalink overlays

| Sub-criterion | Evidence | Status |
|---|---|---|
| Consumes Slice A state | **Gap found:** neither combat-event projection read `TargetabilityAcceptSnapshot`. **Fix:** `CombatEventProjection.Project(input, log, targetability)` and `CombatEventLogProjection.Build(log, simTime, targetability)` accept the authoritative DRG-219/DRG-183 snapshot. They copy each row through `CombatTargetabilityFact.FromRow` (disposition, cause, contact confidence, sensor-to-shooter completeness, ROE engage allowance, targeting disposition/reason). `Assess_permitted_slice_a_row_authorizes_and_attaches_targetability_fact` uses the real `TargetabilityAcceptProjection`. | MET (this change) |
| Assess path gates on Slice A | Withheld or missing Slice A row refuses authorization. Refusal precedence: sim policy denial, then Slice A, then preview abort. | MET (this change) |
| Log path does not rewrite history | The Slice A facts are attached for event targets only, and the phases stay identical: `Log_build_with_slice_a_attaches_facts_for_event_targets_without_altering_phases` | MET (this change) |
| No overlay rebuild | Source fence `Combat_event_sources_do_not_rebuild_sensor_envelope_or_datalink_overlays`: `CombatEvents/*.cs` must not reference `SensorToShooterProjection.`, `ContactProvenanceProjection.`, `TargetabilityAcceptProjection.`, `KillChainContactStateProjection.`, `C2AuthorityProjector.`, envelope resolvers, datalink projections/mergers, or tactical overlays | MET (this change) |

## Blast radius (grep-based; GitNexus index unavailable)

| Symbol | Direct callers (non-test) | Change | Risk |
|---|---|---|---|
| `CombatEventProjection.Project` | none (tests only) | optional trailing `targetability` parameter; legacy behaviour unchanged when null | LOW |
| `CombatEventLogProjection.Build` | `CombatPresentationFrameBridge.Build`, `SliceBCombatScenario.Run` | optional trailing parameter; now also fills `Execution` | LOW |
| `CombatEventSnapshot` | `CombatPresentationFrame`, `CombatDetailPresenter`, `CombatMapPresentation`, `CommandReviewTimeline`, `SliceBCombatScenario` | additive init-only `Targetability` / `Execution` lists, default empty | LOW |
| `CombatEventFingerprint.Compute` | `SliceBCombatScenario.Run` | appends fact segments only when present. Slice B fingerprint text gains `|ex=`, but no golden pins the literal (same-seed equality test only) | LOW |
| `CombatPresentationFrameBridge.Build` | `unity/.../DelegationBridgeHost.cs` (runtime), tests | `Explanations` now sourced from `Events.Execution`; same shape and order (sequence order, ≤ sim time) | LOW–MEDIUM (Unity runtime consumer; shape-preserving) |

Not touched: `DelegationBridge.cs`, Baltic v2 goldens, `CatalogWriteGate`, `ContactCombatCard*`, `EngagePreviewProjection`.

## Verification (targeted; full-solution gates run by orchestrator)

- `dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj -m:1 --filter "FullyQualifiedName~ProjectAegis.Delegation.Tests.CombatEvents|FullyQualifiedName~EngageExplainContract|FullyQualifiedName~AfterAction|FullyQualifiedName~TargetabilityAccept"` gives 63/63
- `dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 --filter "...CombatPresentationFrameBridgeTests|...Combat|...SliceB|...BattleGraphic|...CommandReview|...MapCanvasTransientEffects|...ContactCombatCard"` gives 162/162
- `... UnityAdapter.Tests ... --filter "FullyQualifiedName~ReplayGolden|FullyQualifiedName~PlayModeSmokeHarnessTests"` gives 42/42
