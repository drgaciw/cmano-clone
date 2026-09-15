# Combat-event facts projection — order-log rows → replay-stable combat-event picture

> **ADR:** the **presentation / read-model seam** is
> [`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> (a projection is a **client**, not sim authority) and
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 here (that is sensor side-picture / datalink). Combat UX **Slice B**,
> DRG-211; the downstream engagement-explanation row that mirrors these facts is DRG-215
> ([engage-explain-contract-projection.md](engage-explain-contract-projection.md)).

The **combat-event facts projection** turns the authoritative engagement rows in the order log
([`DecisionLog`](../../src/ProjectAegis.Delegation/Decision/)) into an ordered, replay-stable
[`CombatEventSnapshot`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventTypes.cs) — the
per-engage-assess-leg lifecycle (*intent → authorized/refused → firing → in-flight → terminal
outcome*) that every combat-presentation surface reads. It is a **read-model** derived from the
order log; it is **not** authoritative sim state, is **not** part of the Baltic v2 `SimWorldHash`,
and issues no orders. This is the **upstream producer** of the facts the DRG-215
[engage-explain contract](engage-explain-contract-projection.md) row consumes.

Four engine-agnostic pieces under
[`ProjectAegis.Delegation/CombatEvents/`](../../src/ProjectAegis.Delegation/CombatEvents/):

1. [`CombatEventTypes`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventTypes.cs)
   — the phase enum + input record + fact row + snapshot.
2. [`CombatEventProjection`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventProjection.cs)
   — the single-leg `Project(input, log)` fold (caller-supplied intent/preview + log evidence).
3. [`CombatEventLogProjection`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventLogProjection.cs)
   — the **production** `Build(log, simTime)` fold (all legs from the order log, no caller input).
4. [`CombatEventFingerprint`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventFingerprint.cs)
   — the replay-stable canonical fingerprint.

---

## Types — the combat-event vocabulary

### `CombatEventPhase` — the on-disk lifecycle contract

The phase enum is numbered `1–6`; the numbers are an on-disk contract (DRG-215's
`EngageExplainCombatEventPhase` mirrors it field-for-field and casts at the seam):

| Value | Phase | Meaning |
|-------|-------|---------|
| `1` | `IntentAccepted` | caller/log expressed engage intent for the leg |
| `2` | `Authorized` | affirmative authority (preview `CanFire` or a launched engagement) |
| `3` | `AuthorizationRefused` | policy denial / preview abort / non-launched engagement |
| `4` | `Firing` | the shot was launched |
| `5` | `InFlight` | launched, no terminal outcome yet |
| `6` | `TerminalOutcome` | resolved (`Hit` / `Kill` / …) |

### The records

- **`CombatEngageAssessInput`** (`sealed record`) — the explicit caller-supplied intent/authority/
  preview facts for the single-leg `Project` path: `ShooterId`, `TargetId`, `WeaponFamilyId`,
  `IntentAccepted` (bool), `SimTick`, `SimTime`, `CorrelationId`, optional
  [`EngagePreview`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs)
  (`DlzLabel`, `CanFire`, `AbortPreviewCode`). It **does not** enqueue orders or resolve combat.
- **`CombatEvent`** (`sealed record`) — one presentation-facing fact row: `Phase`, `ShooterId`,
  `TargetId`, `WeaponFamilyId`, `Outcome`, `CorrelationId`, `SimTime`, `SimTick`, `ExplanationRef`.
  Sim-clock only — **no** UI selection / hover / camera / panel state.
- **`CombatEventSnapshot`** (`sealed record`) — the ordered facts for one or more legs. It **owns an
  immutable copy** (`Array.AsReadOnly(events.ToArray())`) on construction, so a caller mutating its
  own list afterward cannot leak into a captured snapshot. `Empty` is the zero-event snapshot.

---

## `CombatEventProjection.Project` — one leg from caller input + log

`Project(CombatEngageAssessInput input, DecisionLog? log = null)` folds explicit engage-assess
intent/authority/preview with matching order-log rows into one leg's lifecycle. It is pure — **no**
`Tick` hook, order enqueue, or combat resolve — and it **never emits a silent authorization deny**.

The fold, in order:

1. `!IntentAccepted` → `Empty` (no intent, no facts).
2. Always emit **`IntentAccepted`** (`Outcome = "IntentAccepted"`, `ExplanationRef =
   "engage-assess:intent-accepted"`).
3. **Refusal check** (`ResolveAuthorizationRefusal`) — emit **`AuthorizationRefused`** and **return
   early** if either:
   - a **shooter-scoped** `Engage` `PolicyDenialRecord` exists in the log (matched on
     `denial.TargetId == input.ShooterId` — policy denials record the *commanded unit*, not the
     hostile victim — with `denial.SimTick >= input.SimTick`, last-wins): `Outcome = reason`,
     `ExplanationRef = "policy:{reason}"`; or
   - `input.Preview is { CanFire: false }`: `Outcome = AbortPreviewCode ?? "ENGAGE_BLOCKED"`,
     `ExplanationRef = "abort:{code}"`.
4. Else, if **affirmative authorization** (`Preview.CanFire == true` **or** a matching launched
   engagement) → emit **`Authorized`** (`Outcome = "Authorized"`, `ExplanationRef = "ENGAGE: CLEAR"`
   — the shared `EngageExplainProjection.CanFireLabel`).
5. **Engagement match** (`FindEngagement`) — a launched `EngagementRecord` whose `ShooterTargetId ==
   input.ShooterId` **and** `EngagementId == input.CorrelationId` (a `CorrelationId` of `0` never
   matches, so a stale same-shooter engagement can't attach). No match → return what's emitted so far.
6. `!engagement.Launched` → **`AuthorizationRefused`** (`Outcome = AbortReasonCode ?? "ENGAGE_ABORT"`,
   `ExplanationRef = "abort:{code}"`), return.
7. Else emit **`Firing`** (`Outcome = "Launch"`, `ExplanationRef = "engage-assess:launch"`).
8. **Outcome match** (`FindOutcome`, on `EngagementId` + shooter): none → **`InFlight`** (`Outcome =
   "InFlight"`, `ExplanationRef = "engage-assess:in-flight"`); else → **`TerminalOutcome`** (`Outcome =
   OutcomeCode`, `ExplanationRef = "outcome:{code}"`).

> **Call site:** `Project` is currently **test-only** — there is no production caller today. The
> shipped host path is the log-driven `CombatEventLogProjection.Build` (below). `Project` is the
> narrower single-leg contract kept green by [its tests](#tests-pins) and mirrored for parity by
> [`CdeAssessProjection`](../../src/ProjectAegis.Delegation/CdeAssess/CdeAssessProjection.cs)
> (same shooter-scoped, `>=`-tick, last-wins policy-denial binding).

---

## `CombatEventLogProjection.Build` — all legs from the order log

`Build(DecisionLog? log, double simTime)` is the **production** path. It builds the combat-event
picture **strictly from authoritative, enriched order-log rows** through `simTime` — it takes **no**
caller `CombatEngageAssessInput`. `null` log → `Empty`.

- **Correlation id is the order-log `SequenceId`** (not the resolver `EngagementId`) — see the method
  remark. Legs are grouped and ordered by `SimTick` → `SequenceId`.
- **Per launched engagement:** `IntentAccepted` → `Authorized` → `Firing`, then a bounded outcome
  match. The outcome window is `SequenceId`-bounded by the **next** attempt sharing the same
  `(shooter, EngagementId)` (`nextAttemptBySequence`), so a **reused engagement id** cannot attach a
  later attempt's outcome to an earlier leg. No outcome and `WeaponFamilyId == "Missile"` (ordinal,
  case-insensitive) → `InFlight`; **only missiles infer in-flight**.
- **Per non-launched engagement:** `IntentAccepted` → `AuthorizationRefused` (`Outcome =
  AbortReasonCode ?? "ENGAGE_ABORT"`; a `WeaponsTight` code surfaces as `ExplanationRef =
  "policy:WeaponsTight"`, else `"abort:{code}"`).
- **Legacy / un-enriched rows** stay visible with **explicit unknowns**: a null victim →
  `"unknown-target"` (`UnknownTargetId`), a blank weapon family → `"Unknown"`
  (`UnknownWeaponFamilyId`).
- **Standalone policy denials** (`Engage` kind, `SimTime <= simTime`) surface as their own
  `IntentAccepted` + `AuthorizationRefused` pair with unknown leg facts — **except** a `WeaponsTight`
  denial that exactly precedes a surfaced weapons-tight engagement refusal at the same
  `(target, SimTime)` is de-duplicated (the earlier denial is dropped) so the pair isn't shown twice.
- **Final sort:** `SimTime` → `SimTick` → `Sequence` → `Phase`. Empty result → `CombatEventSnapshot.Empty`.

### `ExplanationRef` / `Outcome` vocabulary

| `ExplanationRef` | Emitted for |
|------------------|-------------|
| `engage-assess:intent-accepted` | every `IntentAccepted` |
| `ENGAGE: CLEAR` | `Authorized` (the `CanFireLabel`) |
| `engage-assess:launch` | `Firing` |
| `engage-assess:in-flight` | `InFlight` |
| `policy:{FireAbortReason}` | policy-denial / weapons-tight refusal |
| `abort:{code}` | preview-abort / non-launched-engagement refusal |
| `outcome:{OutcomeCode}` | `TerminalOutcome` |

These strings flow downstream unchanged: the DRG-215 [engage-explain row](engage-explain-contract-projection.md)
resolves its reason from exactly this `ExplanationRef` (`abort:…` / `policy:…` first).

---

## Fingerprint — `CombatEventFingerprint.Compute`

`null` **or** a zero-event snapshot → `"ce:empty"`. Otherwise:

```
ce:e=<count>|<phaseInt>,<ShooterId>,<TargetId>,<WeaponFamilyId>,<Outcome>,<CorrelationId>,<SimTime:R>,<SimTick>,<ExplanationRef>|…
```

`InvariantCulture`, ordinal ordering, `SimTime` formatted round-trippable (`"R"`), **no wall clock** —
the same inputs always yield the same string. This is a **presentation-stability** hash (used by the
Slice B combat scenario harness); it is **not** the order-log replay fingerprint or the `SimWorldHash`.

---

## Consumers — producer / consumer map

**Producer of the facts:** the order log itself (`DecisionLog.Engagements` /
`EngagementOutcomes` / `PolicyDenials`), written by `DelegationOrchestrator` / `SimulationSession`
during the sim tick. `CombatEvents` only *reads* it (see [order-log-runtime.md](order-log-runtime.md)).

| Consumer | Uses |
|----------|------|
| [`CombatPresentationFrameBridge.Build`](../../src/ProjectAegis.Delegation.UnityAdapter/Bridge/CombatPresentationFrame.cs) | The shipped host path — bounds the log to `simTime` (so a replay seek never sees future rows), calls `CombatEventLogProjection.Build`, and folds the result into a `CombatPresentationFrame` (shared by map, history, and detail surfaces). Never writes the log. |
| [`SliceBCombatScenario`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/SliceBCombatScenario.cs) | `Build(aggregate, double.MaxValue)` + `CombatEventFingerprint.Compute` for the Slice B combat scenario harness result. |
| [`EngageExplainContractProjection`](../../src/ProjectAegis.Delegation/EngageExplainContract/EngageExplainContractProjection.cs) (DRG-215) | Downstream *why permitted / why withheld* row — mirrors `CombatEventSnapshot` into its own `EngageExplainCombatEventSnapshot` (casting `CombatEventPhase` at the seam). See [engage-explain-contract-projection.md](engage-explain-contract-projection.md). |
| [`AfterActionLedgerProjection`](../../src/ProjectAegis.Delegation/AfterAction/AfterActionLedgerProjection.cs) | Consumes via a **consume-only** mirror (`CombatEventSnapshotConsume`). |
| `CombatDetailPresentation` / `CombatMapPresentation` / `CommandReviewTimeline` (UnityAdapter) | Render the snapshot into detail-card, map, and review-timeline surfaces. |

---

## Determinism & invariants

- **Read-model, off the fingerprint.** The snapshot is derived *from* the order log (itself
  fingerprinted upstream via the order-log replay fingerprint); it never writes the log and is **not**
  part of the `SimWorldHash` or order-log fingerprint (grep confirms no `ProjectAegis.Sim` reference to
  `CombatEvent`). The Baltic v2 hash `17144800277401907079` is untouched — changing event mapping
  cannot move a replay golden.
- **No silent authorization deny.** An explicit refusal (policy denial, preview abort, or a
  non-launched engagement) always surfaces an `AuthorizationRefused` fact, and `Project` **returns**
  after a refusal rather than emitting a later `Authorized` — the leg is never a silent permit.
- **Reuse-safe outcome attachment.** `Build` bounds each leg's outcome match by the next attempt's
  `SequenceId`, so a reused `EngagementId` never mis-attaches a later attempt's terminal outcome to an
  earlier leg.
- **Immutable snapshot.** `CombatEventSnapshot` copies its event list on construction; caller mutation
  cannot mutate a captured leg.
- **No UI-derived truth.** The input / event / snapshot surfaces are asserted free of
  `Selection` / `Hover` / `Camera` / `Panel` / `Visible` / `Chrome` / `IsSelected` members.
- **Pure & off the hotpath.** `Project`, `Build`, and `Compute` read only their arguments — no RNG,
  no wall clock — and run as a presentation read-model, **not** on the `DelegationBridge` `Tick` hotpath.

---

## Tests (pins)

Two NUnit fixtures under
[`ProjectAegis.Delegation.Tests/CombatEvents/`](../../src/ProjectAegis.Delegation.Tests/CombatEvents/)
— **21** tests total, part of the ≥1638-test baseline:

`CombatEventProjectionTests` (**11** — the single-leg `Project` path):

| Test | Covers |
|------|--------|
| `Permitted_path_emits_intent_authorized_firing_and_terminal_outcome` | Full permit leg; `outcome:Kill` terminal carries weapon/`CorrelationId`/`SimTime`. |
| `Refused_path_emits_explicit_authorization_refusal_from_preview_abort` | `Preview.CanFire == false` → `abort:{DLZ_OUT}`. |
| `Refused_path_emits_explicit_policy_denial_reason_for_shooter_scoped_log_row` | Shooter-scoped `RoeHoldFire` denial → `policy:RoeHoldFire`. |
| `Victim_scoped_policy_denial_does_not_suppress_authorization` | A denial keyed to the hostile (not the shooter) does not refuse the leg. |
| `Stale_shooter_scoped_policy_denial_does_not_apply_to_later_attempt` | Denial older than the attempt tick is ignored. |
| `Launch_without_outcome_emits_in_flight` | Launched, no outcome → `InFlight`. |
| `Zero_correlation_does_not_attach_prior_same_shooter_engagement` | `CorrelationId == 0` never binds a prior engagement. |
| `Null_preview_without_log_evidence_omits_authorized` | No preview + no log evidence → `IntentAccepted` only. |
| `Snapshot_event_list_is_immutable_after_construction` | Defensive copy. |
| `Fingerprint_is_identical_for_identical_inputs` | `Compute` stability. |
| `Dto_surface_omits_ui_derived_truth_fields` | Reflection guard against UI-derived fields. |

`CombatEventLogProjectionTests` (**10** — the order-log `Build` path):

| Test | Covers |
|------|--------|
| `Build_uses_enriched_log_facts_for_complete_terminal_leg` | Enriched rows drive victim / weapon / `SequenceId` correlation. |
| `Build_refusal_uses_row_sequence_as_zero_engagement_correlation` | Non-launched refusal uses the row `SequenceId` as correlation. |
| `Build_projects_explicit_unknowns_for_legacy_rows` | `unknown-target` / `Unknown` for un-enriched rows. |
| `Build_does_not_attach_future_or_prior_outcome_and_only_missile_infers_in_flight` | Outcome-window bounds; only missiles infer `InFlight`. |
| `Build_surfaces_unsynthesized_policy_denial_with_unknown_leg_facts` | Standalone `Engage` denial → intent + refusal with unknown facts. |
| `Build_keeps_unrelated_refusals_for_same_shooter_and_tick` | Distinct refusals at the same shooter/tick are all kept. |
| `Build_does_not_deduplicate_against_future_fractional_time_engagement` | Fractional-time dedup boundary. |
| `Build_deduplicates_exact_preceding_weapons_tight_surface_pair` | The one weapons-tight dedup rule. |
| `Build_reused_engagement_id_does_not_attach_later_attempt_outcome_to_earlier_leg` | Reused-id leg isolation. |
| `Build_large_log_preserves_all_grouped_terminal_legs` | 1 200-leg grouping / correlation integrity. |

---

## See also

- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the DRG-215
  downstream *why permitted / why withheld* row that mirrors and consumes these facts.
- [order-log-runtime.md](order-log-runtime.md) — the `DecisionLog` timeline (`EngagementRecord` /
  `EngagementOutcomeRecord` / `PolicyDenialRecord`) these projections read.
- [engagement-pipeline.md](engagement-pipeline.md) — the engage/kill-chain resolver whose abort codes
  (`abort:…`) and outcomes flow into these facts.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the decision-time gate that produces the
  `policy:` refusal reasons.
