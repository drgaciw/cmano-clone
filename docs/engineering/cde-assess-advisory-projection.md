# CDE assess (collateral advisory) projection — developer guide

This is the **collateral-damage-estimate (CDE) advisory projection** (`ProjectAegis.Delegation/CdeAssess/`,
DRG-220): a pure, deterministic read-model that folds explicit engage-assess facts, an optional
range/geometry label, collateral (CDE) withhold facts, and shooter-scoped policy denials into one
ordered *advisory* collateral-risk picture. It answers *"how much collateral risk does this
shooter/target/weapon leg carry, and what assumptions is that estimate standing on?"* — and it is
**never** an authorization verdict.

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It is a projection-layer building block: as of writing it has **no host or
bridge consumer yet** — it is exercised only by its own test fixture, awaiting a presentation surface
(the natural consumer is the [Slice A contact-detail path](c2-presentation-bridges.md), which already
composes kill-chain / provenance / sensor-to-shooter records the same way). Verified against source
and pinned by the tests at the end.

- **Types (core):**
  [`CdeAssessTypes.cs`](../../src/ProjectAegis.Delegation/CdeAssess/CdeAssessTypes.cs) —
  `CdeAssessRiskKind` (`Low` / `Elevated` / `Withheld`), the caller-supplied `CdeAssessInput`, the
  presentation-facing `CdeAssessRow`, and the ordered `CdeAssessSnapshot` (+ `Empty`).
- **Projection (core):**
  [`CdeAssessProjection.cs`](../../src/ProjectAegis.Delegation/CdeAssess/CdeAssessProjection.cs) —
  `Project(CdeAssessInput input, DecisionLog? log = null)` and the stable assumption/constraint
  string constants.
- **Fingerprint (core):**
  [`CdeAssessFingerprint.cs`](../../src/ProjectAegis.Delegation/CdeAssess/CdeAssessFingerprint.cs) —
  `Compute(CdeAssessSnapshot?)` → replay-stable canonical string.
- **Inputs it consumes:** the `EngagePreview` (`DlzLabel`, `CanFire`, `AbortPreviewCode`) from
  [`EngagePreviewProjection`](../../src/ProjectAegis.Delegation/Projection/EngagePreviewProjection.cs),
  and the `PolicyDenialRecord` rows on the [order log](order-log-runtime.md) (`DecisionLog.PolicyDenials`).
- **Related:** the engage/kill-chain resolver that produces the underlying facts is
  [engagement-pipeline.md](engagement-pipeline.md); the ROE/autonomy gate that emits the policy
  denials is [autonomy-roe-gating.md](autonomy-roe-gating.md); the abort codes that appear as
  withhold reasons are [abort-reason-catalog.md](abort-reason-catalog.md); the general read-model
  rules are [c2-projection-layer.md](c2-projection-layer.md).

---

## The pipeline at a glance

```
CdeAssessInput (shooter, target, weapon-family, tick/time, correlation,
                optional EngagePreview, optional RangeClassLabel, CDE withhold facts)
   +  DecisionLog? (order log — read for shooter-scoped Engage PolicyDenials)
        │
        ▼  CdeAssessProjection.Project(input, log)   — pure, no side effects
   risk-kind resolution (precedence below) + assumptions + geometry/range + policy-constraint text
        │
        ▼
CdeAssessSnapshot { Rows: [ CdeAssessRow ] }         — one row per leg, immutable
        │
        ▼  CdeAssessFingerprint.Compute(snapshot)
   replay-stable canonical string ("cde:empty" | "cde:r=<n>|…")
```

`Project` returns a one-row snapshot for the single shooter/target leg it is given; a caller that
assesses several legs composes them into a multi-row `CdeAssessSnapshot` (the fingerprint and the
`Empty` sentinel both handle 0..N rows).

---

## Risk-kind resolution (the precedence that matters)

`Project` picks exactly one `CdeAssessRiskKind`, in this fixed order — **fail-closed to `Withheld`**:

1. **`Withheld` — explicit collateral withhold.** `input.CollateralWithheld == true` wins over
   everything, including a clear preview *and* a policy denial. The row carries
   `WithholdReason = CollateralWithholdReason ?? "CDE_WITHHOLD"` and a `CDE/collateral withhold: <reason>`
   constraint line (`Cde_withhold_takes_precedence_over_clear_preview_and_policy`).
2. **`Withheld` — shooter-scoped policy denial.** Otherwise, if the order log has a matching `Engage`
   `PolicyDenialRecord` (see the binding contract below), the row is withheld with
   `WithholdReason = denial.Reason` (e.g. `RoeHoldFire`, `WeaponsTight`) and a
   `Policy denial: <reason> (tick <n>)` constraint line.
3. **`Elevated` — preview says can't fire.** With no withhold and no denial, an
   `EngagePreview { CanFire: false }` (out-of-range / blocked) projects to `Elevated`, `WithholdReason = null`.
4. **`Low` — feasible.** Everything else (including "no preview supplied"): `Low`, constraint text
   `No policy denial for engage attempt`.

`RiskKind` is **advisory only** — it never encodes permission. The DTOs deliberately omit any
`Authorized` / `CanFire` field (`Low_risk_output_is_advisory_never_authorizes`,
`Dto_surface_omits_ui_derived_truth_fields`).

### Policy-denial binding contract (CombatEvents parity)

`FindPolicyDenial` matches the log the **same way** `CombatEventProjection.FindPolicyDenial` does, on
purpose (DRG-211 P1 lesson — do not invent a second denial contract for one log shape):

- Only `AttemptedKind == OrderKind.Engage` denials.
- `denial.TargetId.Value` must equal `input.ShooterId` — policy denials record the **commanded unit**
  (the shooter) on `TargetId`, *not* the hostile victim (`Victim_scoped_policy_denial_does_not_withhold_cde_assess`).
- `denial.SimTick >= input.SimTick` (at or after the attempt tick), **last-wins**. A stale earlier
  denial does not bleed into a later attempt (`Stale_shooter_scoped_policy_denial_does_not_apply_to_later_attempt`).
- `PolicyDenialRecord` exposes no `CorrelationId`, so attempt-correlated binding is intentionally not
  available without an order-log schema change.

---

## What a row carries

`CdeAssessRow` is an immutable presentation-facing record (sim-clock only — no UI selection, hover,
camera, or panel state):

| Field | Meaning |
|-------|---------|
| `ShooterId` / `TargetId` / `WeaponFamilyId` | The leg being assessed. |
| `RiskKind` | `Low` / `Elevated` / `Withheld` (resolution above). |
| `Assumptions` | The explicit fact list this estimate stands on — defensively copied to a read-only array. |
| `GeometryRangeClass` | `range:<label>` + the preview `DlzLabel` + `abort:<code>` when present, `|`-joined; `geometry/range: not supplied` when empty. |
| `PolicyConstraintText` | Clear / policy-denial / CDE-withhold constraint line. |
| `CorrelationId` / `SimTime` / `SimTick` | Provenance stamps carried through from the input. |
| `WithholdReason` | Non-null only for a `Withheld` row. |

**Assumptions** are drawn from a stable constant catalog on `CdeAssessProjection` (so callers/tests
match on symbols, not free text): the CDE-withhold pair
(`AssumptionCdeWithhold` / `AssumptionNoCdeWithhold`), the preview trio
(`AssumptionPreviewInRange` / `AssumptionPreviewOutOfRange` / `AssumptionNoPreview`), and the
policy pair (`AssumptionPolicyDenial` / `AssumptionNoPolicyDenial`).

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **Advisory, never authorization** | Output is a collateral-risk *estimate*. No DTO carries `Authorized` / `CanFire`; a feasible preview is `Low`, not "cleared to fire". |
| **Fail-closed to `Withheld`** | Explicit CDE withhold and shooter-scoped policy denial both force `Withheld` and take precedence over a clear preview. |
| **Pure / read-only** | `Project` consumes an input record (+ read-only `DecisionLog`) and returns immutable records. It never enqueues orders, resolves combat, mutates the log, or reads the wall clock. |
| **CombatEvents denial parity** | Policy-denial matching mirrors `CombatEventProjection.FindPolicyDenial` (Engage-only, shooter-on-`TargetId`, `>=` attempt tick, last-wins). Do not fork a second contract. |
| **Deterministic / replay-stable** | Same inputs ⇒ identical rows and identical fingerprint (`Fingerprint_is_identical_for_identical_inputs`). Invariant culture, ordinal comparisons, no RNG, no wall clock. |
| **Immutable after construction** | `CdeAssessRow` copies its `Assumptions`; `CdeAssessSnapshot` copies the row array (`AsReadOnly` over a fresh array), so a caller cannot mutate a snapshot via a shared list or an array cast-back. |
| **Collision-safe fingerprint** | String fields are length-prefixed (`len:value`) so `,` / `|` inside ids cannot collide two different snapshots into one hash (`Fingerprint_resists_comma_and_pipe_collisions_in_ids`). |

---

## The fingerprint

`CdeAssessFingerprint.Compute` yields `cde:empty` for a null/empty snapshot, else
`cde:r=<count>` followed by one `|`-delimited segment per row. Each segment packs the risk kind,
the length-prefixed ids/text fields, the numeric stamps (`SimTime` via round-trip `"R"` format), and
the length-prefixed assumption list. It is the CDE-advisory analog of the other projection
fingerprints — a canonical string for equality/regression assertions, **not** a `SimWorldHash` input
and **not** part of the order-log replay fingerprint, so it never affects the Baltic v2 replay hash
`17144800277401907079`.

---

## Extending without breaking replay

1. **Adding a risk kind or assumption?** Add the enum value / string constant, slot it into the
   ordered precedence in `Project`, and pin the new branch with a projection test. Keep the
   fail-closed ordering (withhold → policy denial → preview → feasible).
2. **Adding a row field?** Add it to `CdeAssessRow` (immutable, defensively copied if it is a
   collection) **and** to `CdeAssessFingerprint.AppendRow` (length-prefixed for strings), then update
   the fingerprint tests. A field the fingerprint ignores is a silent replay hole.
3. **Never add an authorization field.** No `Authorized` / `CanFire` / UI-selection field — this is an
   advisory projection (`Dto_surface_omits_ui_derived_truth_fields` will fail otherwise).
4. **Denial matching stays in parity** with `CombatEventProjection.FindPolicyDenial`; change both
   together or not at all.
5. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged.

---

## Tests that pin this doc

All green as of writing (DRG-220). The fixture is NUnit in the core Delegation test assembly
(headless `dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`CdeAssessProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/CdeAssess/CdeAssessProjectionTests.cs) | Low-risk feasible row (assumptions + geometry + policy fields); output is advisory / never authorizes; withheld-by-explicit-CDE with reason; withheld-by-shooter-scoped-policy-denial with reason; CDE withhold precedence over clear preview + denial; victim-scoped denial does **not** withhold; stale denial does not apply to a later attempt; blocked preview ⇒ `Elevated`; snapshot rows + assumptions immutable after construction; distinct weapon families ⇒ distinct rows + fingerprints; identical inputs ⇒ identical fingerprint; fingerprint resists `,` / `|` id collisions; DTO surface omits UI-derived/authorization fields. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~CdeAssess"
```

---

*Verified against source at the paths above. If you change a risk-kind rule, a row field, the
denial-binding contract, or the fingerprint layout, update this doc (and, once a host consumes it,
the consuming presentation doc) together.*
