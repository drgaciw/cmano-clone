# Sensor-to-shooter chain — developer guide

This is the **sensor → shooter chain** read-model (`ProjectAegis.Delegation/SensorToShooter/`, DRG-207):
a pure, deterministic projection that folds the kill-chain contact state plus catalog engage facts
into one inspectable, four-link chain per contact — *"can this contact actually be shot right now, and
if not, **which link is broken**?"*. Its load-bearing promise is the same fail-loud contract the
[track-custody ledger](track-custody-drop-reason-ledger.md) makes: a broken chain **never** shows a
silent/empty cause — every unlinked stage carries a named break (lost sensor / stale track / degraded
track / no FC / no eligible shooter).

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It is a Combat-UX **Slice A** projection-layer building block: as of writing
it has **no host or bridge consumer yet** — it is exercised only by its own test fixture, awaiting a
presentation surface (the natural consumer is a C2 engage/contact-detail panel behind the read-only
presentation seam in [c2-presentation-bridges.md](c2-presentation-bridges.md)). Verified against source
and pinned by the tests at the end.

- **Types (core):**
  [`SensorToShooterTypes.cs`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterTypes.cs) —
  `SensorToShooterLinkKind` (`Sensor` / `Track` / `Targetability` / `EligibleShooter`),
  `SensorToShooterBreakCause` (`None` / `LostSensor` / `StaleTrack` / `NoFireControl` /
  `NoEligibleShooter` / `DegradedTrack`), the stable `SensorToShooterBreakCauseLabels` plain-language
  map, the per-stage `SensorToShooterChainLink`, the per-contact `SensorToShooterChain`, the ordered
  `SensorToShooterSnapshot` (+ `Empty`), and the `ISensorToShooterShooterSource` /
  `SensorToShooterShooterCandidate` sim-authored shooter-candidacy seam.
- **Projection + fingerprint (core):**
  [`SensorToShooterProjection.cs`](../../src/ProjectAegis.Delegation/SensorToShooter/SensorToShooterProjection.cs) —
  the two `Project(…)` overloads (from a `DecisionLog` or a pre-built `KillChainContactSnapshot`), the
  per-link build/break rules, and `ComputeFingerprint(SensorToShooterSnapshot?)` → replay-stable
  canonical string.
- **Inputs it consumes:** the kill-chain contact state from `KillChainContactStateProjection` (DRG-179,
  read off the [order log](order-log-runtime.md)); the catalog engage envelope via
  `CatalogEngageEnvelope.Apply` + `EngagePreviewProjection` (the same engage-preview the C2 attack menu
  uses); and the sim-authored `ISensorToShooterShooterSource` shooter candidates
  (`ScenarioEngageDefaults` + rounds remaining) — **never** UI selection or chrome (ADR-010).
- **Related:** the contact lifecycle that produces `Stale` / `Lost` / `Degraded` overlays is
  [detection-pipeline.md](detection-pipeline.md) and
  [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md); the sibling drop-reason
  read-model over the same kill-chain is [track-custody-drop-reason-ledger.md](track-custody-drop-reason-ledger.md);
  the general read-model rules are [c2-projection-layer.md](c2-projection-layer.md); the engage gate the
  eligible-shooter link previews is [engagement-pipeline.md](engagement-pipeline.md).

---

## The pipeline at a glance

```
DecisionLog (order log)  +  currentSimTick  (+ optional fireControl / shooters /
                                              catalog / weaponId / stale+drop thresholds)
        │
        ▼  SensorToShooterProjection.Project(log, tick, …)   — pure, no side effects
   KillChainContactStateProjection  →  contacts (phase + loss overlay)
        │
        ▼  per contact, in ContactId ordinal order — build four links in sequence:
   [1] Sensor        (observer detecting?)
   [2] Track         (continuous, not stale/lost?)
   [3] Targetability (fire-control track, not degraded?)
   [4] EligibleShooter (a candidate that clears ammo + engage preview?)
        │
        ▼  IsComplete = all four linked ; PrimaryBreakCause = first broken link's cause
   Chains  (one per kill-chain contact, sorted by ContactId ordinal)
        │
        ▼  SensorToShooterProjection.ComputeFingerprint(snapshot)
   replay-stable canonical string ("sts:empty" | "sts:c=<n>|…")
```

A `null` log — or a log with no kill-chain contacts — returns `SensorToShooterSnapshot.Empty`. Otherwise
the snapshot carries **one chain per contact**, each with exactly **four links** in fixed order
(`Sensor → Track → Targetability → EligibleShooter`).

---

## Link resolution (the precedence that matters)

Each link is either **linked** (`IsLinked: true`, `BreakCause.None`) or **broken** with a single named
cause. The chain is walked in order; the **first** broken link sets `PrimaryBreakCause` and
`IsComplete` is `false`. Downstream links inherit the break so the whole chain names the earliest
failure, not a later symptom.

**[1] Sensor** (`BuildSensorLink`):
1. `Loss == Lost` ⇒ `LostSensor`.
2. `!DetectionCaptured` ⇒ `LostSensor` (detail `"sensor not detecting"`).
3. otherwise **linked** (`sensor:<observerId>`).

**[2] Track** (`BuildTrackLink`):
1. `Loss == Lost` ⇒ `LostSensor`.
2. `Loss == Stale` ⇒ `StaleTrack`.
3. `!TrackContinuous` ⇒ `StaleTrack`.
4. otherwise **linked** (`track:<contactId>`).

**[3] Targetability** (`BuildTargetabilityLink`):
1. `Loss == Lost` ⇒ `LostSensor`.
2. `Loss == Stale` ⇒ `StaleTrack`.
3. `Loss == DegradedL1 | DegradedL2` ⇒ `DegradedTrack` (a BDA hit degrades targetability while the
   track itself stays continuous — see the test below).
4. `!Targetable` ⇒ `NoFireControl` **iff** the track is continuous *and* `Loss == None` (a clean track
   with no fire-control solution), else `StaleTrack`.
5. otherwise **linked** (`targetable`).

**[4] EligibleShooter** (`BuildEligibleShooterLink`):
1. If targetability is broken, inherit its break cause (and detail) — no shooter is evaluated.
2. No candidates from `ISensorToShooterShooterSource` ⇒ `NoEligibleShooter`.
3. Walk candidates in `ShooterUnitId` ordinal order; a candidate is eligible only if it clears **both**
   the ammo check (`HasSufficientAmmo`: `RoundsRemaining > 0` **and** `>= max(1, SalvoSize)`, else the
   link detail is `"NO_AMMO"`) **and** the `EngagePreviewProjection` `CanFire` gate (else the detail is
   the preview's `AbortPreviewCode`, or `"engage blocked"`). The first eligible candidate links
   (`shooter:<unitId>`); if none clear, ⇒ `NoEligibleShooter`.

`SensorToShooterBreakCauseLabels.Format` maps each cause to a stable plain-language string
(`"lost sensor"` / `"stale track"` / `"no FC"` / `"no eligible shooter"` / `"degraded track"`); `None`
maps to the empty string. `SensorToShooterChainLink.CauseLabel` / `SensorToShooterChain.PrimaryCauseLabel`
surface it so callers and tests match on the label, not on free text.

---

## What a chain and a link carry

`SensorToShooterChain` is an immutable presentation-facing record — the current shoot-ability of one
contact (sim/order-log truth only; no UI selection, hover, camera, or panel state):

| Field | Meaning |
|-------|---------|
| `ContactId` / `TargetId` / `ObserverId` | The track, the hostile it points at, and the friendly holding it. |
| `IsComplete` | `true` only when all four links are linked. |
| `PrimaryBreakCause` / `PrimaryCauseLabel` (derived) | The first broken link's cause; `None` / empty for a complete chain. |
| `Links` | Exactly four `SensorToShooterChainLink`s in `Sensor → Track → Targetability → EligibleShooter` order. |

`SensorToShooterChainLink` carries `Kind`, `IsLinked`, `BreakCause` (+ derived `CauseLabel`), the
`UnitId` the link resolves to (observer for the sensor link, contact for track/targetability, the chosen
shooter for the eligible-shooter link; `null` when unresolved), `ContactId` / `TargetId`, and a free-text
`Detail` (e.g. `sensor:u1`, `track:c1`, `NO_AMMO`, or an engage abort code) for diagnostics.

Chains are sorted by `ContactId` (`StringComparison.Ordinal`) for a stable, replay-safe order.

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **No silent break** | A broken link never carries `BreakCause.None` / an empty `CauseLabel`. Operators always see *which* link failed and *why*. |
| **Earliest-break wins** | `PrimaryBreakCause` is the first broken link in `Sensor → Track → Targetability → EligibleShooter` order; downstream links inherit it rather than inventing a later symptom. |
| **Pure / read-only** | `Project` consumes a read-only `DecisionLog` (or a pre-built kill-chain snapshot) plus optional readers and returns immutable records. It never enqueues orders, resolves combat, mutates the log, or reads the wall clock. |
| **Sim-authored candidacy** | Shooter candidates come from the `ISensorToShooterShooterSource` seam (sim/order-log facts), never UI selection (ADR-010). |
| **Deterministic / replay-stable** | Same inputs ⇒ identical chains, links, and fingerprint. Ordinal ordering, invariant culture, no RNG, no wall clock. |
| **Immutable after construction** | Chains/links are `sealed record`s; the snapshot exposes `IReadOnlyList`s built from fresh collections. |
| **Off the fingerprint** | The chain fingerprint is a projection artifact for equality/regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

---

## The fingerprint

`SensorToShooterProjection.ComputeFingerprint` yields `sts:empty` for a null/empty snapshot, else
`sts:c=<chainCount>` followed by one `|`-delimited segment per chain. Each chain segment packs
`ContactId`, `TargetId`, `ObserverId`, `IsComplete` (`1`/`0`), the integer `PrimaryBreakCause`, and the
link count, then one `;`-delimited sub-segment per link (`Kind` int, `IsLinked` `1`/`0`, `BreakCause`
int, `UnitId` or empty, `Detail` or empty).

> **Note (accuracy):** like the [custody fingerprint](track-custody-drop-reason-ledger.md#the-fingerprint),
> this joins raw id/detail strings with `,` / `;` / `|` and does **not** length-prefix them, so it is not
> hardened against a separator embedded in an id or detail colliding two distinct snapshots. It is a
> canonical string for equality/regression assertions on the sim-clean ids the projection produces —
> treat it as such, not as a collision-resistant hash. If chain ids or details ever admit user/free
> text, length-prefix the segments and update the fingerprint tests.

---

## Extending without breaking replay

1. **Adding a break cause?** Add the `SensorToShooterBreakCause` enum value **and** its
   `SensorToShooterBreakCauseLabels` string, slot it into the ordered `Build*Link` precedence, and pin
   the new branch with a projection test. Keep the "no broken link is `None`" invariant.
2. **Adding a link stage?** Extend the fixed link sequence in `BuildChain` (and the earliest-break
   walk), add it to `ComputeFingerprint`'s per-link sub-segment, and update the fingerprint tests. A
   field or link the fingerprint ignores is a silent replay hole.
3. **Changing the eligible-shooter gate?** The link previews the real engage path via
   `CatalogEngageEnvelope.Apply` + `EngagePreviewProjection`; keep it a **preview** (no orders, no
   mutation) and add a test for the new ammo/abort branch.
4. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else).

---

## Tests that pin this doc

All green as of writing (DRG-207) — 9 NUnit cases in the core Delegation test assembly (headless
`dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`SensorToShooterProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/SensorToShooter/SensorToShooterProjectionTests.cs) | Complete chain links all four stages (sensor→track→targetability→eligible-shooter) with the right `UnitId`s; a stale track names `StaleTrack` and breaks every downstream link; a missing fire-control track names `NoFireControl` on the targetability link; a `Lost` contact names `LostSensor` and breaks at the sensor link; a targetable contact whose engage preview is blocked names `NoEligibleShooter`; zero rounds and rounds-below-salvo-size both reject the shooter with `NO_AMMO` detail (even when the preview would fire); a BDA `Hit` degradation keeps the track link **linked** but breaks targetability with `DegradedTrack` (distinct from `StaleTrack`); and the fingerprint is replay-stable for identical inputs with chains sorted by `ContactId` ordinal. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~SensorToShooter"
```

---

*Verified against source at the paths above. If you change a link build/break rule, the shooter
eligibility gate, a chain/link field, or the fingerprint layout, update this doc (and, once a host
consumes it, the consuming presentation doc) together.*
