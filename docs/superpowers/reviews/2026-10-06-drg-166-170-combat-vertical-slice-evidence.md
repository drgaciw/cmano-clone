# DRG-166 / DRG-170 — Combat vertical slice: acceptance evidence

**Date:** 2026-10-06
**Epic:** DRG-184 (Combat UX Slice B)
**Branch:** `cursor/drg-166-combat-vertical-slice-7fee` (stacked on DRG-168 → DRG-165)
**Scope:** headless only. Unity Editor visual verification is **pending** (see the last section). No owner acceptance is claimed.

## What was built

| Piece | File | Role |
|---|---|---|
| Commanded scenario mode | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/SliceBCombatScenario.cs` (`RunCommanded`) | Each fixture leg's shooter is a **Manual** agent. Its engage order is queued for approval, approved through `DelegationOrchestrator.TryApprovePendingOrder` (the player command), and executed on the next tick by the real `MvpEngagementResolver`. A synthetic Detected → Classified → Identified contact feed precedes each command. The harness throws if an approved command does not execute. |
| Vertical-slice harness | `src/ProjectAegis.Delegation.UnityAdapter/Baltic/CombatVerticalSliceHarness.cs` | Reads the outcome only through real projections: `TargetabilityAcceptProjection` (Slice A, evaluated on the log bounded to each command's sim time), `CombatEventLogProjection` (DRG-165 contract), `CombatPresentationFrameBridge`, `CombatMapPresenter` at Tactical / Operational / Theater zoom, `EngagementExplanationProjection` (DRG-168), and `MapLodApplyState` / `MapSymbolLodClusterer`. It emits a replay fingerprint over events, Slice A rows, commands, per-leg views, maps, event lines, and LOD clusters. |
| Fixture | `data/scenarios/slice-b-combat-acceptance.json` (existing; unchanged) | Six synthetic legs: permitted and refused Missile, Gun, and Laser. `synthetic: true`. Not a Baltic v2 golden. |

The harness source is fenced: it never reads `DecisionLog.Engagements` / `.EngagementOutcomes` directly (combat facts come only through `CombatEventSnapshot`), and it references neither the v2 hash nor `tests/regression`.

### Weapon-family modeling (laser)

Laser is **not** a blocking gap. `WeaponFamilyId` is a sim-authored string on `EngagementRecord` set from `SimulationSession.CombatWeaponFamilyId`. Scenario policies `slice-b-laser.policy.json` / `slice-b-gun.policy.json` / `slice-b-missile.policy.json` declare the family (`ScenarioWeaponFamilyPolicyTests`), and presentation maps `Laser` / `Energy` / `directed-energy` to the energy cue (`BattleGraphicCueClasses.ForFamily`). The test `Permitted_engagement_runs_from_player_command_through_terminal_outcome("Laser")` asserts the family on the resolver's engagement row, not on a presentation label.

**Limitation (documented, not faked):** every family resolves through the same `MvpEngagementResolver` probability-of-kill model. There is no directed-energy physics (dwell time, thermal or atmospheric effects). The DRG-165 contract emits `InFlight` only for `Missile`, and there is no sustained-fire phase (DRG-165 audit, criterion 1). The "Beam" / "Pulse" / "Track" motion labels describe the displayed event semantics. They are not a simulated trajectory.

## DRG-166 — Combat vertical-slice scenario and acceptance harness

Tests: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/CombatVerticalSliceHarnessTests.cs` (16 tests).

| Acceptance criterion | Evidence (test) | Status |
|---|---|---|
| One deterministic scenario: one missile, one gun, one laser engagement **from command through outcome** | `Permitted_engagement_runs_from_player_command_through_terminal_outcome` ×3 (Missile/Gun/Laser). Command order id matches the leg, the command time precedes execution, and phases are IntentAccepted → Authorized → Firing → TerminalOutcome, with the family on the sim engagement row | MET |
| Covers **permitted and refused** engagement states | Permitted: same as the row above. Refused: `Refused_engagement_is_commanded_and_explicitly_refused_without_firing` ×3. The command is approved, then IntentAccepted → AuthorizationRefused with no Firing; the explanation is `Refused` with the code in the headline; every zoom shows `Refused` / `⊘`. Refusal causes are real resolver aborts: `NO_FIRE_CONTROL_TRACK` (missile, laser) and `OUT_OF_ENVELOPE` (gun) | MET |
| Correlates **map rendering, event log, and replay** per engagement (same correlation id) | `Map_event_log_explanation_and_replay_share_one_correlation_id_per_engagement`: for all six legs, the order-log `SequenceId` equals the combat-event correlation id, the event-history lines, the map effect at each of the three zooms, the DRG-168 explanation, the presentation-frame execution fact, and a second same-seed run's id and key. `Presentation_frame_and_harness_read_the_same_combat_event_facts` shows the frame and the harness read identical events and execution facts | MET |
| Checks **tactical and operational zoom** behavior (headless LOD/declutter) | `Tactical_zoom_draws_each_fired_engagement_with_its_own_motion_cue`, `Operational_zoom_keeps_per_engagement_identity_and_theater_zoom_flattens_motion`, `Symbol_lod_is_identity_at_close_band_and_accounts_for_every_symbol_at_coarse_bands` (`MapLodApplyState` over Close/Tactical/Theater/Overview) | MET |
| **Non-color-only recognition** (each state has a glyph/label/pattern discriminator) | `Every_engagement_state_has_a_non_color_discriminator`: the three fired families have distinct glyph (➤ ✦ ═), line pattern (dash / dot / double-solid), declutter token, and motion label. All six leg states have distinct signatures, and every label carries glyph, family, affiliation, clearance, and outcome text | MET |
| Uses **real projections only** | Harness composition (above). `Explanation_cites_slice_a_targetability_and_execution_time_firing_solution` shows Slice A at command time (Permitted with a track; `NoFireControl` withheld without one; the out-of-envelope gun is targetable but refused at execution) and the execution-time firing solution from `CombatExecutionFact`. `Harness_reads_combat_facts_only_through_the_combat_event_contract` is the source fence | MET |
| Fixtures isolated (not v2 goldens) | `Fixture_is_synthetic_and_isolated_from_baltic_v2_goldens`. Unchanged files: `tests/regression/*`; the hash `17144800277401907079` is still present there | MET |
| Deterministic | `Run_is_replay_stable_for_the_same_seed` (harness fingerprint, events, order-log fingerprint) | MET |

## DRG-170 — Validate combat declutter, accessibility, and replay

Tests: `src/ProjectAegis.Delegation.UnityAdapter.Tests/Baltic/CombatVerticalSliceDeclutterReplayTests.cs` (13 tests).
The clutter scenario is the DRG-166 fixture replicated 12 times with distinct unit ids and jittered positions. Each replica pair is engaged twice, giving 144 commanded legs, 72 fired legs, and 144 distinct map symbols. The default effect budget is 64.

| Acceptance criterion | Evidence (test) | Status |
|---|---|---|
| **Tactical**-scale clutter behavior | `Tactical_band_draws_each_fired_engagement_individually_within_the_effect_budget`: 72 fired legs exceed the budget, so exactly 64 individual effects are drawn and the full event history (no declutter) is kept | MET |
| **Operational**-scale clutter behavior | `Operational_band_folds_repeat_engagements_of_one_pair_and_keeps_motion_cues`: repeat engagements of one shooter/target pair fold into `Operational summary … ×N`. Aggregates never merge different pairs, keep line pattern and motion, and counts sum to the fired-leg total | MET |
| **Theater**-scale clutter behavior | `Theater_band_collapses_to_family_clearance_and_outcome_summaries`: at most 6 summaries; members share family and outcome; motion is `Static` and pattern `none`; no cross-location trail; counts sum to the total. `Symbol_lod_reduces_theater_scale_symbol_clutter_and_accounts_for_every_symbol`: 144 symbols stay identity at Close, are reduced at Theater/Overview, and every symbol is accounted for. `Selected_engagement_survives_declutter_at_every_band` covers all 144 legs at all three zooms | MET |
| Non-color recognition: **family** | `Weapon_family_is_recognisable_without_color_at_every_band`: one distinct glyph + declutter token per family at each zoom, and the label carries glyph and family text | MET |
| Non-color recognition: **affiliation** | `Affiliation_is_recognisable_without_color`: friendly ■ vs hostile ◆ shape glyph, the APP-6 frame glyph from the LOD clusterer differs by affiliation, and the affiliation text appears in labels | MET |
| Non-color recognition: **clearance** | `Clearance_is_recognisable_without_color`: refused is `⊘` + pattern `none` + "Refused"; cleared is the family glyph + family pattern + "Cleared" | MET |
| Non-color recognition: **outcome** | `Outcome_is_recognisable_without_color` (outcome text on the effect label and the event-history line). `Distinct_engagement_states_never_share_a_text_and_glyph_signature`: one signature per (family, affiliation, clearance, outcome) state, distinct across states | MET (see note) |
| **Replay-stable event order and presentation state** (same seed run twice gives identical fingerprints) | `Same_seed_replays_identical_event_order_and_presentation_state` (144-leg clutter run twice: fingerprint, ordered events with non-decreasing sim time, effect labels and event lines at every zoom, explanation fingerprints). `Default_vertical_slice_fingerprint_is_stable_across_runs` (three runs). `Replay_seek_matches_the_run_that_stopped_at_the_same_sim_time` (seeking the full log to the cutoff gives the same events, history, and effects as a run that ended there) | MET |

**Accessibility note (observation, not a failure):** a *refused* leg's map effect uses the generic `⊘` glyph, pattern `none`, and cue class / token `Unknown` for every family. On refused legs, the weapon family is recognizable through the label text (`| Missile |`, `| Laser |`), not through a glyph or cue class. That is still a non-color discriminator. If design wants a per-family glyph on refusals, the change belongs in `CombatMapPresenter.ToEffect` (DRG-167 surface) and is out of scope here.

## Blast radius (grep-based; GitNexus unavailable)

| Symbol | Direct callers | Change | Risk |
|---|---|---|---|
| `SliceBCombatScenario.Run(int)` / `Run(int, Definition)` | `SliceBCombatScenarioTests`, `CombatMapIntegrationTests` (tests only) | Body moved into a private `Execute(…, commanded: false)`; autonomous behavior is unchanged (existing tests 9/9 green) | LOW |
| `SliceBCombatScenario.Result` | Same tests | Additive init-only `Commands` (default empty) | LOW |
| `SliceBCombatScenario.LoadDefaultDefinition` | `SliceBCombatScenario` itself | `private` → `internal` for harness reuse | LOW |
| `SliceBCombatScenario.WireEngageAgent` (private) | `Execute` only | Autonomy parameter | LOW |
| New: `RunCommanded`, `ContactIdFor`, `Command`, `CombatVerticalSliceHarness` + evidence records | New tests only | Additive | LOW |

Not touched: `DelegationBridge.cs`, Baltic v2 goldens (`tests/regression`), `CatalogWriteGate`, `ContactCombatCard*`, `AttackOptionsPreviewBinder`, `EngagePreviewProjection`, `PlayEntry/`, symbology files. There are no sim/tick-path changes, so the replay hash `17144800277401907079` is unaffected.

## Verification (targeted; full-solution gates run by orchestrator)

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~CombatVerticalSlice|FullyQualifiedName~SliceBCombatScenarioTests|FullyQualifiedName~CombatMapIntegrationTests"
# 38/38 (16 DRG-166 + 13 DRG-170 + 9 pre-existing Slice B / map integration)
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~ReplayGolden|FullyQualifiedName~PlayModeSmokeHarnessTests|FullyQualifiedName~CombatVerticalSlice|FullyQualifiedName~SliceB|FullyQualifiedName~Combat|FullyQualifiedName~BattleGraphic|FullyQualifiedName~CommandReview|FullyQualifiedName~MapCanvasTransientEffects"
# 233/233
dotnet test src/ProjectAegis.Delegation.UnityAdapter.Tests/ProjectAegis.Delegation.UnityAdapter.Tests.csproj -m:1 \
  --filter "FullyQualifiedName~ReplayGolden|FullyQualifiedName~PlayModeSmokeHarnessTests"
# 42/42 (PlayModeSmokeHarnessTests alone: 25/25)
dotnet build src/ProjectAegis.Delegation.UnityAdapter/ProjectAegis.Delegation.UnityAdapter.csproj -m:1 -f netstandard2.1
# 0 warnings, 0 errors (Unity plugin target)
```

## Pending Unity Editor evidence (not claimed)

Headless proof covers the projections a host binds. These items still need Editor / Play Mode evidence (Unity-MCP `:8080` was not available in this cloud VM):

1. Load each `slice-b-*` policy in the smoke map and run the commanded legs; confirm the map, event history, and explanation panel show the same correlation for each engagement.
2. Switch Tactical / Operational / Theater density with the clutter set; confirm the 64-slot effect pool, aggregate labels, and that the selected engagement stays visible.
3. Grayscale / color-vision-deficiency screenshot pass: family glyph and line pattern, affiliation shape, clearance `⊘`, and outcome text must read without color.
4. Scrub replay to a mid-run time and back; confirm presentation matches `Replay_seek_matches_the_run_that_stopped_at_the_same_sim_time`.
5. Run `CombatMapViewTests` in the Editor, inspect Console logs, and capture Game View screenshots.
