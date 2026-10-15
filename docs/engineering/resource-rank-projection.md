# Resource-rank projection — advisory shooter/weapon ranking under scarcity (DRG-217)

> **ADR:** this is a **read-model seam**, not a fire path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It consumes
> engage-preview and policy facts and emits a single **advisory** `ResourceRankSnapshot` — an ordered
> list of eligible shooter/weapon pairs — for a C2 panel to review. It **never** authorizes weapons
> release, **never** enqueues a fire order, and **never** triggers automatic engagement — those three
> flags are hard-wired `false`. Combat UX **Slice C**.

When several shooters can each service the same contact but magazines are finite, the panel wants one
honest line: *of the pairs that can legally and physically fire right now, which should the operator
spend first, and why not the others?* The **resource-rank projection** answers that. It is a **pure,
static** fold ([`ResourceRankProjection.Project`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankProjection.cs))
over read-only candidate facts: for each shooter/weapon it runs the same engage-preview and ROE/WRA
policy checks the real kill-chain would, drops the ones that cannot fire with a **named** reason,
scores the survivors on a weighted scarcity model, and returns a rank-ordered
[`ResourceRankSnapshot`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankSnapshot.cs). It
issues nothing; the host decides what to do with the ranking.

It is the sibling advisory of [threat-assessment-projection.md](threat-assessment-projection.md)
(DRG-212): both are folded into the same read-only `AdviceFrame` by the Command-Review
[`AdviceBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceBridge.cs) — threat
assessment answers *should I shoot this weapon?*, resource rank answers *which of several weapons
first?*

| Piece | File | Role |
|-------|------|------|
| [`ResourceRankProjection`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankProjection.cs) | `Delegation/ResourceRank/` | The pure `Project` fold + `ComputeFingerprint`. |
| [`ResourceRankCandidateInput`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankCandidateInput.cs) | `Delegation/ResourceRank/` | Read-only facts for one candidate: contact/target/shooter/weapon ids, weapon label, `EngageContext`, `EffectivePolicy`, optional `ResourceRankAvailabilityFacts`, `ResourceRankPosture`. Contract shape is aligned with `ThreatAssessmentInput` for future composition. |
| [`ResourceRankAvailabilityFacts`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankAvailabilityFacts.cs) | `Delegation/ResourceRank/` | `RoundsCommittedElsewhere`, `MountAvailable`, `TimeToEffectSeconds` — the scarcity inputs the engage preview does not carry (`None` = nothing committed, mount up). |
| [`ResourceRankSnapshot`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankSnapshot.cs) | `Delegation/ResourceRank/` | The immutable output DTO (with the `Empty` sentinel). |
| [`ResourceRankRankedCandidate`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankRankedCandidate.cs) / [`ResourceRankDisposition`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankDisposition.cs) / [`ResourceRankKind`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankKind.cs) | `Delegation/ResourceRank/` | One `Preferred`/`Alternative`/`Excluded` row + the single `AdvisoryRanking` kind. |
| [`ResourceRankScores`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankScores.cs) / [`ResourceRankReasonCode`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankReasonCode.cs) | `Delegation/ResourceRank/` | The five `[0,1]` per-dimension scores + `Total`, and the named exclusion / not-preferred reason strings. |

---

## The exclusion ladder — `EvaluateCandidate`

After the empty-input guard (`Empty` when the candidate list is null/empty, or the **first** candidate's
`ContactId` / `TargetId` is blank), each candidate is folded through `EvaluateCandidate`, which drops it
with the **first** failing gate — **scarcity first, then policy, then engage**:

1. **`EXCLUDED_BY_AVAILABILITY`** — the mount is not available (`!MountAvailable`).
2. **`EXCLUDED_BY_COMMITMENT`** — rounds are committed to another engagement and
   `RoundsCommittedElsewhere + SalvoSize > RoundsRemaining`.
3. **`EXCLUDED_BY_AVAILABILITY`** — `roundsAvailable < SalvoSize`, where
   `roundsAvailable = max(0, RoundsRemaining − RoundsCommittedElsewhere)`.
4. **`EXCLUDED_BY_POLICY`** — the ROE/WRA `PolicyEvaluator` denies a `FireGuided` request; the mapped
   `PolicyAbortCode` (e.g. `ROE_HOLD_FIRE`, `ROE_WEAPONS_TIGHT`, `WRA_SALVO`) is named in the reason.
5. **`EXCLUDED_BY_ENGAGE`** — policy permits, but the engage preview (`EngagePreviewProjection.Project`)
   reports `!CanFire`; its `AbortPreviewCode` (e.g. `DLZ_OUT`) is surfaced.

> **Scarcity gates run before the shared policy/engage checks.** This is the inverse of the
> [threat-assessment](threat-assessment-projection.md) ladder (policy-first): resource rank is comparing
> *multiple* candidates for *spend order*, so a pair with no magazine or a busy mount is dropped before
> the (more expensive) `PolicyEvaluator` + `EngagePreviewProjection` run. Both projections re-use the
> real engage logic rather than re-implementing it, so advice stays consistent with a real launch
> attempt.

Every drop carries a **named** `ResourceRankReasonCode` and a plain-language sentence — there is **no
silent exclusion**.

---

## Ranking — order and dispositions

The survivors are ordered **descending by `Scores.Total`**, with `ShooterUnitId` then `WeaponId`
(ordinal) as the deterministic tie-breakers:

- **Rank 1 → `Preferred`** — `"Preferred shooter/weapon under current scarcity constraints."`, no reason
  code.
- **Rank 2+ → `Alternative`** — `ExplainNotPreferred` compares the candidate against the preferred row
  and names *why it lost* (a `0.05` dead-band avoids noise): `NOT_PREFERRED_LOWER_EFFECT` (lower
  `ExpectedEffect`) → `NOT_PREFERRED_TIME` (slower time-to-effect) → `NOT_PREFERRED_CONSERVE_MAGAZINE`
  (worse conservation), falling back to `NOT_PREFERRED_LOWER_EFFECT` ("lower composite scarcity score").

Excluded candidates are appended after the ranked survivors (sorted `ShooterUnitId` then `WeaponId`),
each with `Rank == 0` and its exclusion reason.

The snapshot `StatusLine` summarizes the fold: `RANK: NO ELIGIBLE SHOOTERS (advisory — not weapons
release)` when nothing is `Preferred`, else `RANK: PREFER <weapon> (advisory — not weapons release)` or
`RANK: PREFER <weapon> +<n> ALT (…)`. Each row also gets its own line (`RANK #1: PREFER …`,
`RANK #2: ALT … — <code>`, `RANK: EXCLUDED … — <code>`).

---

## Scoring — `ComputeScores`

`Total` is a weighted sum of five `[0,1]` dimensions (the weights sum to `1.0`):

| Dimension | Weight | How it is scored |
|-----------|--------|------------------|
| `ExpectedEffect` | `0.35` | `clamp(PkBase × (inEnvelope ? 1.0 : 0.55))` — base Pk, discounted off-envelope. |
| `Time` | `0.20` | DLZ base (`InZone` `1.0` / `Approaching` `0.6` / `OutOfZone` `0.2` / else `0.1`); if `TimeToEffectSeconds > 0`, blended `0.7 × dlz + 0.3 × 1/(1 + t/120)`. |
| `Availability` | `0.20` | `clamp(roundsAvailable / (SalvoSize × 2))` — magazine headroom relative to a salvo. |
| `Commitment` | `0.15` | `1.0` when nothing is committed elsewhere, else `clamp(1 − RoundsCommittedElsewhere / RoundsRemaining)`. |
| `Conservation` | `0.10` | `1.0` above `ShotgunRoundsThreshold`; below it, penalized by scarcity (`× 0.5`) and a weapon-TRL penalty (`clamp(WeaponTechnologyLevel × 0.08, 0, 0.5)`) so a scarce, exotic weapon is conserved. |

Scoring is **pure arithmetic** over the candidate facts — no RNG, no wall clock. `ResourceRankPosture`
(`Offensive`/`Defensive`) is carried through onto each row for labeling but does not currently alter the
scores.

---

## The output — `ResourceRankSnapshot`

Besides `ContactId`, `TargetId`, `Kind`, and `StatusLine`, the snapshot carries the ordered
`RankedCandidates` list. Each [`ResourceRankRankedCandidate`](../../src/ProjectAegis.Delegation/ResourceRank/ResourceRankRankedCandidate.cs)
holds the shooter/weapon ids + `WeaponLabel`, `Posture`, `Disposition`, `Rank`, the full
`ResourceRankScores`, the `ReasonCode` (null only for the preferred row), the plain-language
`ReasonPlain`, and its own `StatusLine`.

- **`IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement`** — all **always `false`**.
- **`Kind`** is always `AdvisoryRanking`.

---

## Fingerprint — `ComputeFingerprint`

Replay-stable canonical string; same input yields the same string (invariant culture, no wall clock):

- `rr:empty` — a null snapshot or an empty candidate list.
- otherwise `rr:<contact>|<target>|<kind>|<wra><fire><auto>|<statusLine>|` then, per candidate,
  `<shooter>:<weapon>:<disposition>:<rank>:<total>:<reasonCode>;` — folding the ranked order, scores
  (`Total` via the `0.######` invariant format), and reason codes.

It is **not length-prefixed** and is **off the `SimWorldHash` / replay fingerprint** — this is a
presentation fingerprint for dirty-frame rebinds, not a sim-determinism input.

---

## Host consumer — `AdviceBridge`

The live consumer is
[`AdviceBridge`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/AdviceBridge.cs) in the
Command-Review surface. It takes a `ResourceRankSnapshot` (supplied as the frame's `ResourceRanking`
evidence) and, after a **fail-closed** identity check (a snapshot — or any ranked row — keyed to a
different contact/target ⇒ `EvidenceUnavailable`, "discard mismatched resource evidence and refresh"),
folds each ranked row into the read-only `AdviceFrame`: the per-candidate `ReasonPlain` + scores become
`Alternatives`, excluded rows become `ResourceCommitments`, and a `ResourceRankSnapshot` evidence
pointer is cited. It sits alongside the [threat-assessment](threat-assessment-projection.md)
`WeaponRecommendation` in the same frame, which keeps `IsFireOrder` / `IsWeaponsReleaseAuthorization`
`false` throughout. The dashboard composition is documented in
[command-review-dashboard.md](command-review-dashboard.md).

---

## Invariants

- **Advisory only** — `IsWeaponsReleaseAuthorization` / `IsFireOrder` / `IsAutomaticEngagement` are
  always `false`; `Kind` is always `AdvisoryRanking`.
- **Pure / deterministic** — a `static` fold over read-only facts; no RNG, no wall clock, no
  `DecisionLog` mutation. Same input ⇒ same ordered snapshot ⇒ same fingerprint (ordinal tie-breakers
  pin the order).
- **Never silent** — every excluded or not-preferred row names a `ResourceRankReasonCode` and spells it
  out in `ReasonPlain` and the row `StatusLine`.
- **Off the sim fingerprint** — projection-only; the Baltic v2 replay hash is untouched.
- **No `UnityEngine`** in the projection; the Unity dependency lives only in the `AdviceBridge` host.

---

## See also

| Topic | Doc |
|-------|-----|
| Sibling advisory folded into the same `AdviceFrame` (`WeaponRecommendation`, DRG-212) | [threat-assessment-projection.md](threat-assessment-projection.md) |
| Engage-time preview / abort codes the exclusion mirrors | [engagement-pipeline.md](engagement-pipeline.md) |
| ROE / WRA gate the policy exclusions come from | [autonomy-roe-gating.md](autonomy-roe-gating.md) |
| Host dashboard that renders the ranking | [command-review-dashboard.md](command-review-dashboard.md) |
| General read-model / projection rules | [c2-projection-layer.md](c2-projection-layer.md) |

## Tests

[`ResourceRankProjectionTests`](../../src/ProjectAegis.Delegation.Tests/ResourceRank/ResourceRankProjectionTests.cs)
(NUnit, 3 tests) pin the two-candidate stable preferred order (higher-Pk `SM-2` beats `ESSM`, with a
named not-preferred reason and matching re-run fingerprint), the `EXCLUDED_BY_COMMITMENT` named-reason
path, and the empty-input `rr:empty` case.
