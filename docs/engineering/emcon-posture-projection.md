# EMCON posture projection — advisory emissions read-model (DRG-221)

> **ADR:** this is a **read-model seam**, not an emitter-control path
> ([`adr-010-headless-first-command-driven-ui.md`](../architecture/adr-010-headless-first-command-driven-ui.md) §2–3,
> [`adr-001-sim-assembly-boundary.md`](../architecture/adr-001-sim-assembly-boundary.md)). It consumes
> read-only **policy EMCON** and **comms** facts and emits a single **advisory** `EmissionsPosture` —
> *is this unit radiating, and if not, why?* — for a C2 status surface to review. It **never** toggles
> an emitter, **never** authorizes weapons release, and **never** enqueues a fire order — those flags
> are hard-wired `false`. Combat UX **Slice C**.

A command surface needs one honest line about each own-force unit's emissions: *is its radar up, and
if it is silent, is that policy EMCON, a passive/standby posture, or denied comms?* The **EMCON
posture projection** answers that. It is a **pure, static** fold
([`EmconPostureProjection.Project`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureProjection.cs))
over read-only facts: it takes a unit's resolved EMCON level, its comms state, and an optional
per-emitter inventory, decides whether the unit is radiating (and lists which emitters are), and when
it is not, names a single **silent cause**. It issues nothing; the host decides what to show.

It is **distinct from** the sim-side [radar-emcon-resolution.md](radar-emcon-resolution.md) seam. That
resolver answers the sim's low-level question — *is the radar emitting?* — for the detection loop and
the engage kill-chain (req-21 / ADR-006). This projection sits one layer up: it **calls** that resolver
(via `BuildInput`) to get the EMCON level, then folds in comms and an advisory presentation (silent
causes, radiating-emitter list, plain-language assumptions, a `StatusLine`) for the UI. Emissions
posture is also deliberately **separate from C2 network health** (DRG-214,
[c2-network-health-projection.md](c2-network-health-projection.md)) and from agent Skills (DRG-194) — a
unit can be fully connected yet policy-silent, or radiating yet partitioned.

| Piece | File | Role |
|-------|------|------|
| [`EmconPostureProjection`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureProjection.cs) | `Delegation/EmconPosture/` | The pure `Project` fold + `BuildInput` + `ComputeFingerprint`. |
| [`EmconPostureInput`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureInput.cs) | `Delegation/EmconPosture/` | Read-only facts: `UnitId`, `EmconLevel` (`EmconState`), `CommsState` (default `Nominal`), optional per-emitter `Emitters` list. |
| [`EmconEmitterFact`](../../src/ProjectAegis.Delegation/EmconPosture/EmconEmitterFact.cs) | `Delegation/EmconPosture/` | One emitter's `(EmitterId, State)` — the inventory the default-radar fallback stands in for. |
| [`EmissionsPosture`](../../src/ProjectAegis.Delegation/EmconPosture/EmissionsPosture.cs) | `Delegation/EmconPosture/` | The immutable output DTO (with the `Empty` sentinel). |
| [`RadiatingSensor`](../../src/ProjectAegis.Delegation/EmconPosture/RadiatingSensor.cs) | `Delegation/EmconPosture/` | One actively-radiating emitter `(EmitterId, Label)` in the snapshot. |
| [`EmconPostureKind`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureKind.cs) / [`EmconPostureSilentCause`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureSilentCause.cs) / [`EmconPostureSilentCauseLabels`](../../src/ProjectAegis.Delegation/EmconPosture/EmconPostureSilentCauseLabels.cs) | `Delegation/EmconPosture/` | The single `AdvisoryEmissionsPosture` kind, the four silent causes, and their stable display labels. |

---

## Enums

| Enum | Values | Notes |
|------|--------|-------|
| [`EmconState`](../../src/ProjectAegis.Sim/Policy/EmconState.cs) | `Off` `0` / `Passive` `1` / `Active` `2` | Per-emitter EMCON state (policy GDD MVP); shared with the sim resolver. |
| `EmconPostureKind` | `AdvisoryEmissionsPosture` `0` | The only value — the output is always advisory. |
| `EmconPostureSilentCause` | `None` `0` / `PolicyOff` `1` / `StandbyPassive` `2` / `CommsDenied` `3` | Why a unit is not fully radiating; `None` means it is. |
| [`CommsState`](../../src/ProjectAegis.Delegation/Comms/CommsState.cs) | `Nominal` `0` / `Degraded` `1` / `Denied` `2` | Only `Denied` suppresses emissions; `Degraded` is noted but still radiates. |

---

## Building the input — `BuildInput`

`BuildInput(unitId, policy?, commsState = Nominal, catalog?)` resolves the EMCON level the same way
the sim does — it delegates to
[`ScenarioEmconResolver.ResolveRadar`](../../src/ProjectAegis.Sim/Scenario/ScenarioEmconResolver.cs)
(scenario `unitRadarEmcon` override → catalog posture fallback → default `Active`, see
[radar-emcon-resolution.md](radar-emcon-resolution.md)) — and wraps it in an `EmconPostureInput`. A
caller that already has an EMCON level and a per-emitter inventory can also construct the input
directly.

The emitter inventory has a **three-way** meaning:

- **`null`** (not supplied) → falls back to a single default emitter
  `radar-1` ([`CatalogRadarEmconResolver.DefaultEmitterId`](../../src/ProjectAegis.Sim/Catalog/CatalogRadarEmconResolver.cs))
  at the unit's `EmconLevel`.
- **a populated list** → used as-is, ordered by `EmitterId` (ordinal) for determinism.
- **an explicit empty list** → **authoritative**: the unit has *no* emitters, so even an `Active`
  posture radiates nothing (no default-radar fallback).

---

## The silent-cause ladder — `ResolveSilentCause`

Before any emitter is counted, the projection decides whether the unit is radiating at all by taking
the **first** matching cause:

1. **`CommsDenied`** — `CommsState == Denied` ⇒ emissions suppressed despite policy; code
   [`COMMS_DENIED`](../../src/ProjectAegis.Sim/Glossary/AbortReasonCatalog.Generated.cs).
2. **`PolicyOff`** — `EmconLevel == Off` ⇒ emitters held silent by policy; code `EMCON_OFF`.
3. **`StandbyPassive`** — `EmconLevel == Passive` ⇒ listen-only / standby; **no** code (an expected
   posture, not an abort reason).
4. **`None`** — otherwise the unit is radiating per policy.

Comms `Denied` wins over policy: a unit told to radiate but cut off from C2 is reported silent. (A
`Degraded` comms state does **not** suppress emissions — it is noted in the assumptions but the unit
still radiates.)

---

## Radiating sensors — `BuildRadiatingSensors`

- If the silent cause is anything other than `None`, the radiating list is **empty** — a silent unit
  radiates nothing regardless of its emitter inventory.
- Otherwise each emitter whose `State == Active` becomes a `RadiatingSensor` (`Passive`/`Off` emitters
  are skipped), with a display `Label` that is the `EmitterId` with hyphens turned into spaces
  (`radar-1` → `radar 1`).

So a multi-emitter unit at policy `Active` lists only the emitters that are themselves `Active`; an
explicit-empty inventory at `Active` is radiating-but-with-no-emitters (`SilentCause.None`, empty list).

---

## Assumptions and status line

The `Assumptions` list always opens with the load-bearing disclaimer
`"Advisory emissions posture only — not emitter toggle authorization."`, then
`"Policy EMCON level is {ACTIVE|PASSIVE|OFF}."`, then
`"Emissions posture is distinct from C2 network health and agent Skills."`. A non-`Nominal` comms state
adds `"Comms state is {degraded|denied}."`. Finally it names the outcome — either the radiating
emitters (`"Radiating emitters: …"` / `"No emitters are actively radiating."`) or
`"Not fully radiating — {label}."` for a silent cause.

The one-line `StatusLine` is `"EMCON: {level} | radiating: {emitters|none} (advisory — not emitter
toggle)"` when radiating, or `"EMCON: {level} | silent: {code|label} (advisory — not emitter toggle)"`
when silent (the abort-reason code when one exists, else the human label).

---

## The output — `EmissionsPosture`

`EmissionsPosture` carries `UnitId`, `EmconLevel`, `PostureKind`, the four advisory flags, the
`RadiatingSensors` and `Assumptions` lists, the `SilentCause` + its `SilentCauseCode` (nullable) +
`SilentCauseLabel`, and the `StatusLine`.

- **`IsAdvisoryOnly`** is always `true`; **`IsEmitterToggleAuthorization` / `IsWeaponsReleaseAuthorization` /
  `IsFireOrder`** are always `false`.
- **`PostureKind`** is always `AdvisoryEmissionsPosture`.
- A blank/whitespace `UnitId` returns the **`EmissionsPosture.Empty`** sentinel (empty `UnitId`,
  `EmconState.Off`, no radiating sensors, no assumptions, `StatusLine` `"EMCON: —"`).

---

## Fingerprint — `ComputeFingerprint`

Replay-stable canonical string; same input yields the same string (invariant culture, no wall clock):

- `emcon:empty` — a null posture or a blank `UnitId`.
- otherwise
  `emcon:<unitId>|<emconLevel>|<postureKind>|<advisory><toggle><wra><fire>|<silentCause>|<silentCauseCode>|s=<n>|<emitterId>,<label>…|a=<m>|<assumption>…`
  — folding the posture, the four advisory flags as a four-bit field, the silent cause + code, then the
  radiating-emitter list and the assumptions.

It is **not length-prefixed** and is **off the `SimWorldHash` / replay fingerprint** — this is a
presentation fingerprint for dirty-frame rebinds, not a sim-determinism input.

---

## Host consumer — `StatusFrame` (Command Review)

The live consumer is the Command-Review
[`StatusFrame`](../../src/ProjectAegis.Delegation.UnityAdapter/CommandReview/StatusFrame.cs) builder.
For each unit it prefers **live** emissions telemetry: when the snapshot is an `IStatusSensorSource`,
the row lists the real active emitter ids and EMCON level. When no live sensor source is available it
**falls back** to this projection — `EmconPostureProjection.BuildInput(id, orchestrator.ScenarioPolicy,
CurrentCommsState(…))` then `Project(…)` — and emits an explicit `EMISSIONS: UNKNOWN; policy
{posture.StatusLine}` row with `StatusKnowledge.Unknown`, so the operator sees the *policy* posture
without it being mistaken for a confirmed live picture. The dashboard composition is documented in
[command-review-dashboard.md](command-review-dashboard.md).

(Note the separate ASSET-012 `PolicyEmconHudPanelBinder` in `Delegation/Projection/` is a different,
free-text policy/EMCON HUD line — it does not consume `EmissionsPosture`.)

---

## Invariants

- **Advisory only** — `IsEmitterToggleAuthorization` / `IsWeaponsReleaseAuthorization` / `IsFireOrder`
  are always `false`; `PostureKind` is always `AdvisoryEmissionsPosture`. The projection never toggles
  an emitter or mutates sim state.
- **Never silent-without-a-cause** — a non-radiating unit always names a `SilentCause` (and a code
  when one exists); the `StatusLine` and assumptions spell it out.
- **Pure / deterministic** — a `static` fold over read-only facts; no RNG, no wall clock, no
  `DecisionLog` mutation. Ordinal emitter order and the fixed cause precedence pin the output; same
  input ⇒ same posture ⇒ same fingerprint.
- **Off the sim fingerprint** — projection-only; the Baltic v2 replay hash is untouched.
- **No `UnityEngine`** in the projection; the Unity dependency lives only in the `StatusFrame` host.

---

## See also

| Topic | Doc |
|-------|-----|
| Sim-side radar-EMCON resolver this projection builds on | [radar-emcon-resolution.md](radar-emcon-resolution.md) |
| Comms clock whose `Denied` state suppresses emissions | [comms-degradation-runtime.md](comms-degradation-runtime.md) |
| Host dashboard (`StatusFrame`) that renders the posture | [command-review-dashboard.md](command-review-dashboard.md) |
| Sibling Slice-C read-model deliberately kept separate | [c2-network-health-projection.md](c2-network-health-projection.md) |
| General read-model / projection rules | [c2-projection-layer.md](c2-projection-layer.md) |

## Tests

[`EmconPostureProjectionTests`](../../src/ProjectAegis.Delegation.Tests/EmconPosture/EmconPostureProjectionTests.cs)
(NUnit, 8 tests) pin the `Active` radiating path (default `radar-1`, advisory flags `false`), the three
silent causes (`Off` → `EMCON_OFF`, `Passive` → `passive / standby`, `Denied` → `COMMS_DENIED`, each
with no radiating sensors), the stable re-run fingerprint, the `emcon:empty` blank-unit case, the
multi-emitter "only `Active` radiators" fold, and the explicit-empty-inventory "stays empty, no
default-radar fallback" case.
