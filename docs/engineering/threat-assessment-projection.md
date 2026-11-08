# Threat-assessment projection — advisory weapon-recommendation read-model (DRG-212)

> **ADR:** this is a **read-model seam**, not a fire path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-007-c2-map-presentation.md`](../architecture/adr-007-c2-map-presentation.md),
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It consumes
> read-only **engage-preview** and **policy** facts and emits a single **advisory** `WeaponRecommendation`
> — *should this shooter/weapon be recommended against this contact, and if not, why?* — for a C2 surface
> to review. It **never** authorizes weapons release, **never** enqueues a fire order, and **never**
> auto-engages — those flags are hard-wired `false`. Combat UX **Slice C**.

A command surface needs one honest line per shooter/weapon/contact leg: *is this launch feasible right
now, and if it is being held, is that ROE (policy) or an engage-gate (range / magazine / fire-control)?*
The **threat-assessment projection** answers that. It is a **pure, static** fold
([`ThreatAssessmentProjection.Project`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentProjection.cs))
over read-only facts: it takes an engage context, an effective ROE/WRA policy, and a weapon identity,
runs the same engage preview and policy checks the sim would, and classifies the result as **feasible**
or **withheld** with a named cause, a confidence score, the range/envelope picture, and the ROE/WRA
constraints. It issues nothing; the host decides what to show.

It **calls** the sim-side engage gates it reports on —
[`EngagePreviewProjection.Project`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs)
and [`DlzEngageGate`](../../src/ProjectAegis.Sim/Engage/DlzEngageGate.cs) for the dynamic-launch-zone
state, and the [`PolicyEvaluator`](../../src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs) for the ROE/WRA
verdict — then folds those into an advisory presentation (outcome, confidence, assumptions, a
`StatusLine`) for the UI. The engage math itself lives in the [engagement pipeline](engagement-pipeline.md);
the decision-time ROE write path is [autonomy-roe-gating.md](autonomy-roe-gating.md); the withhold-reason
codes come from the [abort-reason catalog](abort-reason-catalog.md).

| Piece | File | Role |
|-------|------|------|
| [`ThreatAssessmentProjection`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentProjection.cs) | `Delegation/ThreatAssessment/` | The pure `Project` fold + `ComputeFingerprint`. |
| [`ThreatAssessmentInput`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentInput.cs) | `Delegation/ThreatAssessment/` | Read-only facts: `ContactId`, `TargetId`, `ShooterUnitId`, `WeaponId`, `WeaponLabel`, an `EngageContext`, an `EffectivePolicy`, a `ThreatAssessmentPosture` (default `Offensive`), and an optional `ThreatAssessmentTuning`. |
| [`ThreatAssessmentTuning`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentTuning.cs) | `Delegation/ThreatAssessment/` | Scenario-supplied confidence numerics (not compiled gameplay constants); `Default` mirrors the DRG-212 baseline. |
| [`ThreatPolicyConstraints`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatPolicyConstraints.cs) | `Delegation/ThreatAssessment/` | The ROE/WRA facts constraining the recommendation (`RoeLevel`, `MaxSalvo`, `AutoEngageAuthorized`, `ExpendAuthorized`, `PolicyAllowsFire`, `PolicyAbortCode`). |
| [`ThreatRangeAssessment`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRangeAssessment.cs) | `Delegation/ThreatAssessment/` | The range/envelope picture (`RangeMeters`, `EnvelopeMin/MaxMeters`, `DlzState`, `DlzLabel`, `InEnvelope`). |
| [`WeaponRecommendation`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendation.cs) | `Delegation/ThreatAssessment/` | The immutable output DTO (with the `Empty` sentinel). |
| [`WeaponRecommendationOutcome`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendationOutcome.cs) / [`ThreatRecommendationKind`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRecommendationKind.cs) | `Delegation/ThreatAssessment/` | The four outcomes and the single `AdvisoryRecommendation` kind. |

---

## Enums

| Enum | Values | Notes |
|------|--------|-------|
| `ThreatAssessmentPosture` | `Offensive` `0` / `Defensive` `1` | Strike-vs-intercept label; drives only the assumption text and the fingerprint, not the gate logic. |
| `WeaponRecommendationOutcome` | `Feasible` `0` / `WithheldByPolicy` `1` / `WithheldByEngage` `2` / `NoRecommendation` `3` | `NoRecommendation` is the empty-input sentinel. |
| `ThreatRecommendationKind` | `AdvisoryRecommendation` `0` | The only value — the output is always advisory. |
| [`DlzState`](../../src/ProjectAegis.Sim/Engage/DlzState.cs) | `Unknown` `0` / `OutOfZone` `1` / `Approaching` `2` / `InZone` `3` | Dynamic-launch-zone state from `DlzEngageGate`; shared with the engage pipeline. |

---

## The outcome ladder — `ResolveOutcome`

`Project` evaluates the ROE/WRA policy **first**, then the engage gates, taking the **first** blocking
check:

1. **`WithheldByPolicy`** — `PolicyEvaluator.Evaluate(...)` denied the synthetic `FireGuided` request
   ⇒ ROE/WRA holds release. The `PolicyAbortCode` is mapped from the `FireAbortReason`
   (e.g. `ROE_HOLD_FIRE`, `ROE_WEAPONS_TIGHT`, `WRA_RANGE`, `WRA_SALVO`, `AUTO_ENGAGE_DENIED`,
   `EXPEND_UNAUTHORIZED`, …; see [abort-reason-catalog.md](abort-reason-catalog.md)).
2. **`WithheldByEngage` / `WINCHESTER_ORDNANCE`** — `RoundsRemaining <= 0` ⇒ magazine empty.
3. **`WithheldByEngage` / `NO_AMMO`** — `RoundsRemaining < salvoSize` (`salvoSize = max(1, ctx.SalvoSize)`)
   ⇒ not enough rounds for the requested salvo.
4. **`WithheldByEngage` / `<preview code>`** — `EngagePreviewProjection.CanFire` is `false` ⇒ the engage
   preview's own abort code (`DLZ_OUT`, `NO_FIRE_CONTROL_TRACK`, `MOUNT_OFFLINE`, `EMCON_OFF`,
   `CYBER_SPOOF_TRACK`).
5. **`Feasible`** — otherwise the launch is feasible *if authorized separately*.

ROE wins over the engage gates: a weapons-tight or hold-fire unit is reported withheld-by-policy even
when the shot is geometrically feasible, and a DLZ-out or winchester unit under Weapons Free is
reported withheld-by-engage (the policy still `PolicyAllowsFire`). The `WithheldReasonCode` surfaced on
the DTO is the policy abort code for `WithheldByPolicy`, the engage abort code for `WithheldByEngage`,
and `null` when feasible.

> The policy evaluation is synthetic/advisory: it builds a `PolicyContext` (snapshot id `1`, tick `0`,
> the supplied `EffectivePolicy`, salvo `max(1, ctx.SalvoSize)`) and a `FireGuided` `ActionRequest`
> (mount `1`) purely to read the ROE verdict. Non-numeric `ShooterUnitId` / `TargetId` parse to `0`.
> It does **not** touch sim state or any order queue.

---

## Confidence — `ComputeConfidence`

Confidence is a `[0, 1]` advisory score driven entirely by the injected `ThreatAssessmentTuning`
(`Default` values shown):

- **`WithheldByPolicy`** → `WithheldByPolicyConfidence` (`0`).
- **`WithheldByEngage`** → by the engage abort code: `DLZ_OUT` → `WithheldByEngageDlzOutConfidence`
  (`0.35`), `NO_FIRE_CONTROL_TRACK` → `WithheldByEngageNoFireControlConfidence` (`0.2`), else
  `WithheldByEngageDefaultConfidence` (`0.1`).
- **`Feasible`** → a DLZ base (`InZone` `0.85` / `Approaching` `0.55` / else `0.25`), multiplied by
  `LowMagazineMultiplier` (`0.75`) when `RoundsRemaining <= max(1, ShotgunRoundsThreshold)`, and by
  `AutoEngageUnauthorizedMultiplier` (`0.9`) when auto-engage is not authorized, then clamped to
  `[0, 1]`.

Tuning is **scenario-supplied data, not compiled constants** — callers inject explicit values; `Default`
only mirrors the legacy DRG-212 baseline so an omitted bag is numerically identical.

---

## Assumptions and status line

The `Assumptions` list always opens with the two load-bearing disclaimers
`"Advisory recommendation only — not weapons release authorization."` and
`"Does not enqueue fire orders or trigger automatic engagement."`, then the posture/contact and
weapon/shooter context, then the observed engage facts (fire-control track held or not, mount online,
radar EMCON permits emissions), and finally the outcome rationale — either
`"Engage preview and ROE both permit launch if authorized separately."` (feasible),
`"ROE {level} withholds weapons release."` (withheld by policy), or
`"Engage gate blocked ({code})."` (withheld by engage).

The one-line `StatusLine` is:

- `"THREAT: RECOMMEND {weapon} (advisory — not weapons release)"` when feasible;
- `"THREAT: WITHHELD BY POLICY — {code|ROE} (recommend {weapon} if ROE permits)"` when held by policy;
- `"THREAT: WITHHELD BY ENGAGE — {code|BLOCKED} (recommend {weapon} when feasible)"` when held by an
  engage gate;
- `"THREAT: —"` for the `Empty` sentinel.

---

## The output — `WeaponRecommendation`

`WeaponRecommendation` carries the leg identity (`ContactId`, `TargetId`, `ShooterUnitId`, `WeaponId`,
`WeaponLabel`), the `Posture`, the `Outcome` + `RecommendationKind`, the three advisory flags, the
`Confidence`, the `Assumptions` and `Range` (`ThreatRangeAssessment`) and `PolicyConstraints`
(`ThreatPolicyConstraints`), the `WithheldReasonCode` (nullable), and the `StatusLine`.

- **`IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement`** are always `false`.
- **`RecommendationKind`** is always `AdvisoryRecommendation`.
- A blank/whitespace `ContactId`, `TargetId`, **or** `WeaponId` returns the
  **`WeaponRecommendation.Empty`** sentinel (empty ids, `NoRecommendation` outcome, `0` confidence,
  no assumptions, a zeroed `DlzState.Unknown` range, a `HoldFire` / `PolicyAllowsFire: false`
  constraint, `StatusLine` `"THREAT: —"`).

---

## Fingerprint — `ComputeFingerprint`

Replay-stable canonical string; same input yields the same string (invariant culture, no wall clock):

- `ta:empty` — a null recommendation **or** any `NoRecommendation` outcome (so `Empty` → `ta:empty`).
- otherwise
  `ta:<contactId>|<targetId>|<shooterId>|<weaponId>|<posture>|<outcome>|<kind>|<wra><fire><auto>|<confidence>|<range>|<envMin>|<envMax>|<dlzState>|<inEnvelope>|<roe>|<maxSalvo>|<autoEngage>|<expend>|<policyAllows>|<policyCode>|<dlzLabel>|<withheldCode>|<statusLine>|<assumptions…>`
  — folding the leg identity, the three advisory flags as a three-bit field, the confidence and the
  full range/envelope/DLZ picture, the ROE/WRA constraints, the DLZ label, the withheld code, the
  status line, and the `;`-joined assumptions (doubles formatted `0.######`, invariant culture).

It is **off the `SimWorldHash` / replay fingerprint** — this is a presentation fingerprint for
dirty-frame rebinds, not a sim-determinism input.

---

## Host consumer — `AdviceFrame` (Command Review)

The live consumer is the Command-Review
[`AdviceBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceBridge.cs). A
runtime that implements `IAdviceEvidenceSource` supplies a current `WeaponRecommendation?` on the
per-contact [`AdviceEvidenceSource`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceModels.cs).
When one is present the bridge:

- cites it as a `WeaponRecommendation` projection evidence pointer;
- emits a `weapon-policy:{RoeLevel}:{WithheldReasonCode|none}` and a
  `range:{m}:envelope=…:{DlzState}:in-envelope=…` policy line from `PolicyConstraints` + `Range`;
- uses the recommendation's `StatusLine` as the `AdviceFrame` rationale.

The `AdviceFrame` itself always reports `IsWeaponsReleaseAuthorization` / `IsFireOrder` `false` and the
`advisory-only:no-authority-or-order` hard constraint, so the recommendation never reads as a clearance.
The dashboard composition is documented in [command-review-dashboard.md](command-review-dashboard.md).
The `Range` sub-record also feeds the DLZ live surface via
[`DlzLiveSurfaceBinder.Bind`](../../src/ProjectAegis.Delegation.UnityAdapter/Presentation/DlzLiveSurfaceBinder.cs).

---

## Invariants

- **Advisory only** — `IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement` are
  always `false`; `RecommendationKind` is always `AdvisoryRecommendation`. The projection never fires,
  authorizes, or mutates sim state.
- **Never withheld-without-a-cause** — a non-feasible leg always names an outcome and (where one exists)
  a `WithheldReasonCode`; the `StatusLine` and assumptions spell it out.
- **Pure / deterministic** — a `static` fold over read-only facts; no RNG, no wall clock, no
  `DecisionLog` mutation. The fixed policy-then-engage precedence and the injected tuning pin the
  output; same input ⇒ same recommendation ⇒ same fingerprint.
- **Off the sim fingerprint** — projection-only; the Baltic v2 replay hash is untouched.
- **No `UnityEngine`** in the projection; the Unity dependency lives only in the `AdviceBridge` /
  `DlzLiveSurfaceBinder` hosts.

---

## See also

| Topic | Doc |
|-------|-----|
| Engage math / DLZ gate / preview this projection builds on | [engagement-pipeline.md](engagement-pipeline.md) |
| Decision-time ROE/WRA write path behind the policy verdict | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Withhold-reason codes (`DLZ_OUT`, `ROE_*`, `WRA_*`, `WINCHESTER_ORDNANCE`, …) | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Host dashboard (`AdviceFrame`) that renders the recommendation | [command-review-dashboard.md](command-review-dashboard.md) |
| Sibling Slice-C advisory read-models | [combat-ux-advisory-ledgers.md](combat-ux-advisory-ledgers.md) / [cde-assess-advisory-projection.md](cde-assess-advisory-projection.md) |
| General read-model / projection rules | [c2-projection-layer.md](c2-projection-layer.md) |

## Tests

[`ThreatAssessmentProjectionTests`](../../src/ProjectAegis.Delegation.Tests/ThreatAssessment/ThreatAssessmentProjectionTests.cs)
(NUnit, 10 tests) pin the feasible path (confidence/range/constraints, advisory flags `false`), the two
withheld-by-policy cases (`WeaponsTight` → `ROE_WEAPONS_TIGHT`, `HoldFire` → `ROE_HOLD_FIRE`), the three
withheld-by-engage cases (`DLZ_OUT` under Weapons Free, `WINCHESTER_ORDNANCE` on an empty magazine,
`NO_AMMO` below salvo), the tuning-changes-confidence path, the fingerprint variance (auto-engage /
expend / DLZ-label) and re-run stability, and the `ta:empty` blank-input case.
