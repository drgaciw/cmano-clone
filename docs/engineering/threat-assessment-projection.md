# Threat-assessment / weapon-recommendation projection — advisory read-model (DRG-212)

> **ADR:** this is a **read-model seam**, not a fire path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It consumes
> engage-preview and policy facts and emits a single **advisory** `WeaponRecommendation` for a C2 panel
> to review. It **never** authorizes weapons release, **never** enqueues a fire order, and **never**
> triggers automatic engagement — those three flags are hard-wired `false`. Combat UX **Slice C**.

When a watch officer is deciding whether to commit a weapon to a contact, the panel wants one honest
line: *given this shooter, this weapon, and the current ROE, is a launch feasible — and if not, why?*
The **threat-assessment projection** answers exactly that. It is a **pure, static** fold
([`ThreatAssessmentProjection.Project`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentProjection.cs))
over read-only facts: it runs the same engage-preview and ROE/WRA policy checks the real kill-chain
would, then packages the verdict, a confidence number, the range picture, the policy constraints, and
a plain-language assumption list into one [`WeaponRecommendation`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendation.cs).
It issues nothing; the host decides what to do with the advice.

| Piece | File | Role |
|-------|------|------|
| [`ThreatAssessmentProjection`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentProjection.cs) | `Delegation/ThreatAssessment/` | The pure `Project` fold + `ComputeFingerprint`. |
| [`ThreatAssessmentInput`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentInput.cs) | `Delegation/ThreatAssessment/` | Read-only facts: contact/target/shooter/weapon ids, `EngageContext`, `EffectivePolicy`, `ThreatAssessmentPosture`, optional `ThreatAssessmentTuning`. |
| [`WeaponRecommendation`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendation.cs) | `Delegation/ThreatAssessment/` | The immutable output DTO (with the `Empty` sentinel). |
| [`WeaponRecommendationOutcome`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendationOutcome.cs) / [`ThreatRecommendationKind`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRecommendationKind.cs) | `Delegation/ThreatAssessment/` | The `Feasible`/`WithheldBy*`/`NoRecommendation` verdict + the single `AdvisoryRecommendation` kind. |
| [`ThreatRangeAssessment`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRangeAssessment.cs) / [`ThreatPolicyConstraints`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatPolicyConstraints.cs) | `Delegation/ThreatAssessment/` | The DLZ range picture and the ROE/WRA constraints attached to the recommendation. |
| [`ThreatAssessmentTuning`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentTuning.cs) | `Delegation/ThreatAssessment/` | Scenario-injected confidence numerics (`Default` mirrors the DRG-212 baseline). |

---

## The outcome ladder — `ResolveOutcome`

After the empty-input guard (`Empty` when any of `ContactId` / `TargetId` / `WeaponId` is blank),
`Project` resolves exactly one [`WeaponRecommendationOutcome`](../../src/ProjectAegis.Delegation/ThreatAssessment/WeaponRecommendationOutcome.cs)
with **policy first, then engage gates** — the first failing gate wins:

1. **`WithheldByPolicy`** — the ROE/WRA `PolicyEvaluator` denies a `FireGuided` request. The named
   `PolicyAbortCode` (e.g. `ROE_HOLD_FIRE`, `ROE_WEAPONS_TIGHT`) becomes the `WithheldReasonCode`.
2. **`WithheldByEngage` / `WINCHESTER_ORDNANCE`** — policy permits, but `RoundsRemaining <= 0`.
3. **`WithheldByEngage` / `NO_AMMO`** — `RoundsRemaining < SalvoSize` (at least one round, but not a full salvo).
4. **`WithheldByEngage`** — the engage preview (`EngagePreviewProjection.Project`) reports `!CanFire`;
   its `AbortPreviewCode` (e.g. `DLZ_OUT`, `NO_FIRE_CONTROL_TRACK`) is surfaced verbatim.
5. **`Feasible`** — all gates pass. The status line reads `THREAT: RECOMMEND <weapon> (advisory — not weapons release)`.

`NoRecommendation` is reserved for the `Empty` sentinel only. The resolution is **never silent**:
a withheld recommendation always carries a non-null `WithheldReasonCode`, and the `StatusLine`
spells out `WITHHELD BY POLICY` / `WITHHELD BY ENGAGE` with that code.

> **Policy and engage run the real checks.** `Project` evaluates a genuine `PolicyContext` +
> `ActionRequest(FireGuided)` through the shared `PolicyEvaluator`, and the DLZ picture comes from
> `DlzEngageGate.EvaluateState`. The projection re-uses the engage logic rather than re-implementing
> it, so advice stays consistent with what a real launch attempt would decide.

---

## Confidence — `ComputeConfidence` + `ThreatAssessmentTuning`

Confidence is a `[0, 1]` number driven by the outcome and the DLZ state, with all numerics supplied
by [`ThreatAssessmentTuning`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatAssessmentTuning.cs)
(the `Default` bag below; scenarios inject their own):

- **Withheld by policy** → `WithheldByPolicyConfidence` (`0` in the default bag).
- **Withheld by engage** → `WithheldByEngageDlzOutConfidence` (`DLZ_OUT`), `WithheldByEngageNoFireControlConfidence`
  (`NO_FIRE_CONTROL_TRACK`), else `WithheldByEngageDefaultConfidence`.
- **Feasible** → a DLZ base (`InZone` `0.85` / `Approaching` `0.55` / else `0.25`), then multiplied by
  `LowMagazineMultiplier` (`0.75`) when `RoundsRemaining <= ShotgunRoundsThreshold`, and by
  `AutoEngageUnauthorizedMultiplier` (`0.9`) when auto-engage is not authorized, finally clamped to `[0, 1]`.

Because the numerics live on the injected tuning bag, confidence is **data-driven** — a scenario can
raise or lower advisory confidence without a code change (see the `Different_tuning_changes_confidence…` test).

---

## The output — `WeaponRecommendation`

Besides `Outcome`, `Confidence`, and `WithheldReasonCode`, the DTO carries:

- **`Range`** — a [`ThreatRangeAssessment`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatRangeAssessment.cs)
  of `RangeMeters`, the envelope min/max, the `DlzState` (`Unknown`/`OutOfZone`/`Approaching`/`InZone`),
  the `DlzLabel`, and `InEnvelope`.
- **`PolicyConstraints`** — a [`ThreatPolicyConstraints`](../../src/ProjectAegis.Delegation/ThreatAssessment/ThreatPolicyConstraints.cs)
  snapshot of `RoeLevel`, `MaxSalvo`, `AutoEngageAuthorized`, `ExpendAuthorized`, `PolicyAllowsFire`,
  and the `PolicyAbortCode`.
- **`Assumptions`** — a plain-language list that always opens with
  *"Advisory recommendation only — not weapons release authorization."* and
  *"Does not enqueue fire orders or trigger automatic engagement."*, then states the posture, weapon,
  fire-control-track / mount / radar-EMCON assumptions, and why the gate passed or blocked.
- **`StatusLine`** — the one-line panel string.
- **`IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement`** — all **always `false`**,
  and **`RecommendationKind`** is always `AdvisoryRecommendation`.

---

## Fingerprint — `ComputeFingerprint`

Replay-stable canonical string; same input yields the same string (invariant culture, no wall clock):

- `ta:empty` — a null recommendation or `Outcome == NoRecommendation`.
- otherwise `ta:<contact>|<target>|<shooter>|<weapon>|<posture>|<outcome>|<kind>|<wra><fire><auto>|<confidence>|…`
  folding the range (meters / envelope min / max / dlz-state / in-envelope), the policy constraints
  (roe / max-salvo / auto-engage / expend / allows-fire / abort-code), the `DlzLabel`, the
  `WithheldReasonCode`, the `StatusLine`, and the `;`-joined assumptions.

It is **not length-prefixed** and is **off the `SimWorldHash` / replay fingerprint** — this is a
presentation fingerprint for dirty-frame rebinds, not a sim-determinism input. The
`Fingerprint_changes_when_auto_engage_expend_or_dlz_label_differ` and `Same_inputs_yield_same_fingerprint`
tests pin both directions.

---

## Host consumer — `AdviceBridge`

The one live consumer is
[`AdviceBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceBridge.cs) in the
Command-Review surface. It takes a `WeaponRecommendation` (supplied as the frame's `ThreatAssessment`
evidence) and, after a **fail-closed** freshness/identity check (mismatched contact/target ⇒
`EvidenceUnavailable`, stale evidence ⇒ `Stale`), folds its assumptions, `Confidence`, `StatusLine`,
`PolicyConstraints`, and `Range` into the read-only `AdviceFrame` alongside the
[`ResourceRankSnapshot`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankProjection.cs)
sibling ranking. The frame keeps `IsFireOrder` / `IsWeaponsReleaseAuthorization` `false` throughout —
the dashboard is advisory, the fire path is elsewhere. The dashboard composition is documented in
[command-review-dashboard.md](command-review-dashboard.md).

---

## Invariants

- **Advisory only** — `IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement` are
  always `false`; `RecommendationKind` is always `AdvisoryRecommendation`.
- **Pure / deterministic** — a `static` fold over read-only facts; no RNG, no wall clock, no
  `DecisionLog` mutation. Same input ⇒ same output ⇒ same fingerprint.
- **Never silent** — a withheld outcome always names a `WithheldReasonCode` and spells it out in the
  `StatusLine` and `Assumptions`.
- **Off the sim fingerprint** — projection-only; the Baltic v2 replay hash is untouched.
- **No `UnityEngine`** in the projection; the Unity dependency lives only in the `AdviceBridge` host.

---

## See also

| Topic | Doc |
|-------|-----|
| Engage-time preview / abort codes this advice mirrors | [engagement-pipeline.md](engagement-pipeline.md) |
| ROE / WRA gate the policy constraints come from | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Sibling advisory ranking folded into the same `AdviceFrame` (`ResourceRankProjection`, DRG-217) | [`src/ProjectAegis.Delegation/ResourceRank/`](../../src/ProjectAegis.Delegation/ResourceRank/) |
| Host dashboard that renders the advice | [command-review-dashboard.md](command-review-dashboard.md) |
| General read-model / projection rules | [c2-projection-layer.md](c2-projection-layer.md) |

## Tests

[`ThreatAssessmentProjectionTests`](../../src/ProjectAegis.Delegation.Tests/ThreatAssessment/ThreatAssessmentProjectionTests.cs)
(NUnit, 10 tests) pin the feasible path (confidence + range + policy constraints), the two policy
withholds (`WeaponsTight`, `HoldFire`), the three engage withholds (`DLZ_OUT`, `WINCHESTER_ORDNANCE`,
`NO_AMMO`), tuning-driven confidence, the two fingerprint directions, and the `ta:empty` empty-input case.
