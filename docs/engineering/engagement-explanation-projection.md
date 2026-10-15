# Engagement-explanation projection — the full why-this-engagement surface (DRG-168)

> **ADR:** this is the **presentation / read-model seam**
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3
> — a projection is a **client**, not sim authority) over
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md) (the adapter boundary).
> Do **not** cite Git ADR-018 (that is sensor side-picture / datalink). Combat UX; the combat-event + Slice A
> facts it reads are DRG-165 / DRG-211, the explanation contract it folds is DRG-215, this surface is DRG-168.

The **engagement-explanation projection** turns the ordered **combat-event facts** for one engage-assess leg —
events plus the DRG-165 Slice A targetability and execution facts — into one
[`EngagementExplanationSurface`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationTypes.cs):
the *"why is this engagement available / selected / refused, and what can the commander do about it?"* card a C2
client shows for a **selected** engagement. It is strictly a **read-model** derived from combat-event facts — it
issues no orders, rebuilds no projections, reads no world snapshot, and is **not** part of the Baltic v2
`SimWorldHash`. Missing facts stay `UNKNOWN`; it never fabricates authority or a kill.

Three engine-agnostic pieces under
[`ProjectAegis.Delegation/EngagementExplanation/`](../../src/ProjectAegis.Delegation/EngagementExplanation/):

1. [`EngagementExplanationProjection`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationProjection.cs)
   — the single pure `Build(events, shooterId, targetId, correlationId)` entry point.
2. [`EngagementExplanationTypes`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationTypes.cs)
   — the `EngagementExplanationSurface` record, the `EngagementConstraint` row, and the three status/constraint enums.
3. [`EngagementExplanationFingerprint`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationFingerprint.cs)
   — the replay-stable canonical fingerprint.

It differs from its sibling [engage-explain-contract-projection.md](engage-explain-contract-projection.md): the
engage-explain contract is the one-line *why permitted / why withheld* reason (DRG-215), and this projection
**consumes** it (`EngageExplainContractProjection.ProjectFromSnapshot`) while adding the full hard-vs-doctrine
constraint breakdown, the firing solution, the contact confidence, and an **actionable remedy**.

---

## Inputs — a `CombatEventSnapshot` + the leg keys

`Build` reads only a [`CombatEventSnapshot`](../../src/ProjectAegis.Delegation/CombatEvents/CombatEventTypes.cs)
(from [combat-event-log-projection.md](combat-event-log-projection.md)) and three leg keys — the public-API
surface is fenced to exactly `CombatEventSnapshot`, `string`, and `ulong`:

| Input | Role |
|-------|------|
| `CombatEventSnapshot? events` | `null` → `Empty`. Otherwise its three lists: `Events` (the lifecycle facts), `Targetability` (DRG-165 Slice A facts), `Execution` (firing-solution / salvo facts). |
| `string shooterId`, `string targetId`, `ulong correlationId` | The exact leg to explain. The `Events` list is filtered to **this** `(ShooterId, TargetId, CorrelationId)` tuple; a non-matching leg → `Empty`. |

The relevant input fields:

- **`CombatEvent`** — `Phase` (the `CombatEventPhase` `1–6` lifecycle), `ShooterId`, `TargetId`, `WeaponFamilyId`,
  `Outcome`, `CorrelationId`, `SimTime`, `SimTick`, `ExplanationRef`.
- **`CombatTargetabilityFact`** — the Slice A fact copied from a `TargetabilityAcceptContactRow`: `Disposition`
  (`Permitted`/`Withheld`), `WithheldCauseCode`, `Confidence`, `SensorToShooterComplete` (nullable — unknown when
  no chain fact), `RoeAllowsEngage`, `TargetingDisposition`, `TargetingReasonCode`, and a nullable `ShooterId`
  (null = the chain found **no** eligible shooter).
- **`CombatExecutionFact`** — `HasFireControlTrack` (nullable — null predates the enriched log), `SalvoSize`,
  matched by `(CorrelationId, ShooterId, TargetId)`.

---

## The fold — `Build`

```csharp
EngagementExplanationSurface Build(
    CombatEventSnapshot? events, string shooterId, string targetId, ulong correlationId)
```

### 1. Leg + status

Filter `events.Events` to the exact tuple. On an empty leg return
[`EngagementExplanationSurface.Empty`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationTypes.cs).
Otherwise pick the **decisive** event by priority and derive the status:

| Status | Decisive event (first present) | Meaning |
|--------|--------------------------------|---------|
| `Refused` | last `AuthorizationRefused` | an explicit authorization refusal — **wins over everything** |
| `Selected` | last `Firing` / `InFlight` / `TerminalOutcome` | selected and executed |
| `Available` | last `Authorized` | authorized, not yet fired |
| `Unknown` | the last leg event | intent accepted; no authorization/refusal/execution reported yet — **never silently `Available`** |

`decisive = refused ?? executed ?? authorized ?? leg[^1]`.

### 2. Shooter-scoped Slice A facts — `FindTargetability` (load-bearing)

`FindTargetability(facts, shooterId, targetId)` walks the targetability facts for the target and resolves, in
order:

1. a `Permitted` fact **for this shooter** → return it;
2. else the first `Withheld` fact **for this shooter** (`first`);
3. else a **contact-wide track refusal** (a fact with a `null` shooter whose withhold is a track cause,
   `IsContactWideRefusal`) as the fallback.

The keystone is **shooter scoping** (`fix(combat): scope Slice A facts to the nominated shooter`): a fact whose
`ShooterId` is another unit is skipped for the per-shooter legs, so **another shooter's permission never grants
this shooter**, and a withheld authority for this shooter is never replaced by a different unit's permit. The
resolved fact only populates the **doctrine** (ROE / targeting-authority) rows when
`targetability.ShooterId == shooterId` — a contact-wide refusal still surfaces the **Track** hard constraint but
leaves ROE / targeting authority `Unknown` (`fix(combat): keep contact-wide track refusals without shooter
authority`).

### 3. Constraint rows

From the decisive event, the refusal code, the resolved targetability fact, and the execution fact, `Build`
emits two fixed-order constraint lists. Every row carries a `Kind`, a stable `Name`
([`EngagementConstraintNames`](../../src/ProjectAegis.Delegation/EngagementExplanation/EngagementExplanationTypes.cs)),
a `State` (`Satisfied`/`Violated`/`Unknown`), an optional stable reason `Code`, and human-readable `Text`.

**Hard constraints** (physical / track / weapon facts no order can override):

| Row | `Satisfied` | `Violated` | `Unknown` |
|-----|-------------|------------|-----------|
| `Track` | fact `Permitted` (via `{ContactId}`), or a doctrine-withheld fact whose `SensorToShooterComplete` | non-doctrine withhold cause, or `SensorToShooterComplete == false` | no Slice A fact, or no sensor-to-shooter fact |
| `FiringSolution` | execution `HasFireControlTrack == true`, else `SensorToShooterComplete == true` | `== false` on either | nullable on either |
| `Engagement` | — | a non-targetability, non-doctrine refusal code (`EngageExplainProjection.ExplainCode`) | no refusal, a doctrine refusal, or a targetability refusal (*"see Track"*) |

**Doctrine constraints** (ROE / weapons release / targeting authority / policy):

| Row | `Satisfied` | `Violated` | `Unknown` |
|-----|-------------|------------|-----------|
| `Roe` | `RoeAllowsEngage` | `!RoeAllowsEngage` (code = `TargetingReasonCode`) | no shooter-scoped authority fact |
| `TargetingAuthority` | `TargetingDisposition == Permitted` | `ApprovalRequired` or `Withheld` | no fact |
| `Policy` | — | a doctrine refusal code (targetability → `DescribeTargetabilityCause`, else `ExplainCode`) | no doctrine refusal |

A **refusal is a doctrine constraint** when its code is in the `DoctrineCodes` set (ROE/WRA/EMCON/expend/TL,
plus the three C2-authority targetability causes `SharedTrackNoRelease` / `WeaponsReleaseRequired` /
`ApprovalRequired`); otherwise a targetability refusal (`ExplanationRef` starts `targetability:`) routes to the
Track/Firing-Solution rows, and any other hard refusal fills the `Engagement` row.

### 4. Headline, reason text, and the remedy

- **`Headline`** is one line keyed on status: `Refused — <cause> [<code>]`, `Selected — <weapon> engaged
  <target>: <phase> / <outcome>`, `Available — authorized …`, or the intent-only `Unknown` sentence.
- **`ActionableReason`** (`ResolveActionableReason`) is the **"what can I do"** remedy for a refusal: it first asks
  [`EngageNextActionProjection`](../../src/ProjectAegis.Delegation/EngageNextAction/) for a `ReloadRearm` /
  `Approval` next action, then falls back to a code→sentence map (e.g. `NO_FIRE_CONTROL_TRACK` → *"Acquire or
  designate a fire-control track"*, `DLZ_OUT` / `WraRange` → *"Close to within the weapon launch zone"*, stale /
  lost / degraded → *"Re-acquire the contact …"*, WRA/weapons-tight/approval → *"Request weapons-release
  approval"*). It is **`null`** when nothing the commander can do would lift the refusal (e.g.
  `TARGET_DESTROYED`).
- **`DescribeTargetabilityCause`** turns a `TargetabilityAcceptCauseCodes` value into plain language
  (*"contact track is stale"*, *"no fire-control track on the contact"*, *"shared track is not released for
  engagement"*, …).
- **`ExplanationRef`** is resolved from the folded `EngageExplainContractDto` — `WhyWithheld` for `Refused`,
  `WhyPermitted` for `Available`, else the decisive event's own `ExplanationRef`.

---

## The surface — `EngagementExplanationSurface`

A `sealed record` with the leg keys (`ShooterId`, `TargetId`, `CorrelationId`, `SimTime`), the `Status`, the
`Headline`, the two `IReadOnlyList<EngagementConstraint>` lists (`HardConstraints` / `DoctrineConstraints`),
`ContactConfidence` (the fact's `Confidence`, or `UNKNOWN`), `Weapon` (`Weapon family: … | Salvo: n`),
`FiringSolution`, the nullable `ActionableReason`, and `ExplanationRef`. `Empty` is the
*"Select an engagement to inspect."* zero-leg singleton (all lists empty, every string `UNKNOWN`/empty).

---

## Fingerprint — `EngagementExplanationFingerprint.Compute`

`null` **or** the `Empty` singleton → `"eex:empty"`. Otherwise a canonical string with the status, leg keys
(`SimTime` round-trippable `"R"`, `InvariantCulture`), headline, each constraint list (count-prefixed, in order,
each row as `kind,name,state,code`), contact confidence, weapon, firing solution, actionable reason, and
explanation ref:

```
eex:s=<status>|<shooter>,<target>,<cid>,<simTime:R>|h=<headline>|hc=<n>;…|dc=<m>;…|cc=…|w=…|fs=…|a=…|x=…
```

No wall clock, no RNG, fixed row order — the same inputs always yield the same string.

---

## Consumer — `CombatVerticalSliceHarness`

The DRG-166 vertical-slice harness
[`CombatVerticalSliceHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/CombatVerticalSliceHarness.cs)
builds one surface per commanded engagement leg
(`EngagementExplanationProjection.Build(events, leg.ShooterId, leg.TargetId, correlationId)`) and folds
`EngagementExplanationFingerprint.Compute(leg.Explanation)` into its own replay fingerprint, so the explanation
is correlated across the event log, map, and replay for a commanded missile/gun/laser slice. There is no
dedicated Unity panel host yet; the natural surface is the Slice A contact-detail / engage path behind
[c2-presentation-bridges.md](c2-presentation-bridges.md).

---

## Determinism & invariants

- **Read-model, off the fingerprint.** Derived from combat-event facts (fingerprinted upstream via the DRG-211
  log); never writes the log, not part of the `SimWorldHash` or order-log fingerprint. The Baltic v2 hash
  `17144800277401907079` is untouched — changing a reason mapping cannot move a replay golden.
- **Shooter-scoped authority.** `FindTargetability` only honours a fact for the nominated shooter for the
  ROE / targeting-authority rows; another shooter's permission never leaks in, and a contact-wide track refusal
  surfaces the Track row without granting authority (`Other_or_unknown_shooters_…` /
  `Matching_shooters_withheld_authority_is_not_replaced_…`).
- **No silent authorization.** A refusal always wins over an authorization in the same leg; an intent-only leg
  stays `Unknown`, never silently `Available`.
- **Missing evidence stays `Unknown`.** Every constraint row has an explicit `Unknown` state when its fact is
  absent — a missing fact is never rendered as satisfied.
- **No sim queries / no rebuilt projections.** The sources are fenced against `DecisionLog`, `ISimWorldSnapshot`,
  `ObservedState`, `SimulationSession`, `DelegationOrchestrator`, `DelegationBridge`, `IOrderSink`,
  `SliceAContactFrame`, the upstream `*Projection.` rebuilders, and `ICatalogReader` — it reads **only** the
  `CombatEventSnapshot` handed in.
- **No UI-derived truth.** The surface / constraint records are asserted free of
  `Selection` / `Hover` / `Camera` / `Panel` / `Visible` / `Chrome` / `IsSelected` members.
- **Pure.** `Build` and `Compute` read only their arguments — no RNG, no wall clock, no `UnityEngine`.

---

## Tests (pins)

All in
[`EngagementExplanationProjectionTests`](../../src/ProjectAegis.Delegation.Tests/EngagementExplanation/EngagementExplanationProjectionTests.cs)
(NUnit, `ProjectAegis.Delegation.Tests/EngagementExplanation`, **16** tests). Highlights:

| Test | Covers |
|------|--------|
| `Available_leg_explains_why_permitted_with_satisfied_constraints` | `Available` status, all five Track/FiringSolution/ROE/TargetingAuthority rows `Satisfied`, confidence + weapon + `null` remedy. |
| `Selected_leg_explains_weapon_salvo_and_firing_solution_at_execution` | `Selected` from an execution leg: weapon + `Salvo: 2`, fire-control-track firing-solution text. |
| `Refused_targetability_hard_constraint_states_actionable_reason` | targetability refusal → violated hard row carries the cause, broken firing solution, fire-control remedy, `targetability:` ref. |
| `Refused_policy_denial_is_a_doctrine_constraint_with_approval_action` | ROE-hold-fire denial → doctrine row, approval remedy, no violated hard row. |
| `Refused_wra_range_policy_denial_recommends_closing_range` | WRA-range → *close the range* remedy (not approval). |
| `Refused_domain_abort_is_hard_constraint_with_weapon_selection_action` | surface-aspect-block → hard `Engagement` row, weapon-selection remedy, Track `Unknown`. |
| `Refused_without_remedy_reports_no_actionable_reason` | `TARGET_DESTROYED` → `null` remedy. |
| `Other_or_unknown_shooters_facts_leave_requested_shooters_constraints_unknown` (×2) | another/unknown shooter's facts leave Track / ROE / targeting authority `Unknown`. |
| `Matching_shooters_withheld_authority_is_not_replaced_by_another_shooters_permission` | this shooter's withhold survives a peer's permit. |
| `Unmatched_leg_returns_empty_surface` / `Intent_only_leg_is_unknown_not_silently_available` | `Empty` on no leg / `null` events; intent-only → `Unknown`. |
| `Replay_same_inputs_produce_identical_surface_and_fingerprint` | fingerprint stability + `eex:empty`. |
| `Fence_public_api_consumes_only_combat_event_snapshot_and_leg_keys` / `Fence_sources_issue_no_sim_queries_or_rebuilt_projections` / `Dto_surface_omits_ui_derived_truth_fields` | the API, source, and UI-derived-truth fences above. |

Part of the ≥1638-test baseline.

---

## See also

- [engage-explain-contract-projection.md](engage-explain-contract-projection.md) — the one-line *why permitted /
  why withheld* contract this surface folds in.
- [combat-event-log-projection.md](combat-event-log-projection.md) — the upstream `CombatEventSnapshot` producer
  (events + Slice A targetability + execution facts).
- [targetability-accept.md](targetability-accept.md) / [sensor-to-shooter-chain.md](sensor-to-shooter-chain.md) —
  the Slice A composition behind the `CombatTargetabilityFact` rows.
- [autonomy-roe-gating.md](autonomy-roe-gating.md) — the decision-time gate that emits the `policy:` / refusal
  reasons; [engagement-pipeline.md](engagement-pipeline.md) — the engage/kill-chain abort codes; reason codes are
  [abort-reason-catalog.md](abort-reason-catalog.md).
- [c2-projection-layer.md](c2-projection-layer.md) — general read-model / presentation-boundary rules.
