# Targetability-accept composition — developer guide

This is the **targetability-accept** read-model (`ProjectAegis.Delegation/TargetabilityAccept/`,
DRG-219): a pure, deterministic projection that **composes** three wave-1 Combat-UX projectors —
contact provenance, the [sensor-to-shooter chain](sensor-to-shooter-chain.md), and C2 authority — into
one acceptance disposition per contact: *"is this contact cleared to be targeted right now, and if
**not**, which single named cause withholds it?"*. It is the **Slice A headless exit gate** that sits
on top of the read-model family the [kill-chain contact state](kill-chain-contact-state.md) (DRG-179)
founds and the [sensor-to-shooter chain](sensor-to-shooter-chain.md) (DRG-207) continues. Its
load-bearing promise is **fail-closed**: a row is `Permitted` only when provenance, the chain, and
authority *all* clear; any gap yields `Withheld` with a single, never-empty named cause.

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It is **headless only** — as of writing it has **no host or bridge consumer
yet** and performs no Unity or tick-path mutation; it is exercised solely by its own test fixture as the
Combat-UX Slice A composition harness. Verified against source and pinned by the tests at the end.

- **Types (core):**
  [`TargetabilityAcceptTypes.cs`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptTypes.cs) —
  the `TargetabilityAcceptDisposition` (`Permitted` = 0 / `Withheld` = 1 — there is **no** third
  `ApprovalRequired` disposition at this layer; an authority approval requirement folds into `Withheld`
  with the authority's reason code), the stable `TargetabilityAcceptCauseCodes` (`None`, the provenance
  causes `Stale` / `CatalogMiss` / `SilentComms` / `MissingProvenance`, the sensor-to-shooter causes
  `LostSensor` / `StaleTrack` / `NoFireControl` / `NoEligibleShooter` / `DegradedTrack`, and the
  authority causes re-exported from `C2AuthorityProjector` — `WeaponsTight` / `RoeHoldFire` /
  `NoFireControlAuthority` / `SharedTrackNoRelease` / `WeaponsReleaseRequired` / `ApprovalRequired`),
  the per-contact `TargetabilityAcceptContactRow` (id, target, disposition, withheld cause, and the
  three child records it carried), the ordered `TargetabilityAcceptSnapshot` (+ `Empty`), and the
  reserved `TargetabilityAcceptChildFingerprints` record (declared for a per-row child-fingerprint
  triple; the shipped fingerprint composes child fingerprints inline instead — see the note below).
- **Projection (core):**
  [`TargetabilityAcceptProjection.cs`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptProjection.cs) —
  the two `Project(…)` overloads (from a `DecisionLog` — which projects the three children for you — or
  from three **pre-built** child snapshots), the fail-closed `ResolveDisposition` precedence, and the
  union-of-contacts fold.
- **Fingerprint (core):**
  [`TargetabilityAcceptFingerprint.cs`](../../src/ProjectAegis.Delegation/TargetabilityAccept/TargetabilityAcceptFingerprint.cs) —
  `Compute(TargetabilityAcceptSnapshot?)` → replay-stable canonical string that folds each child's own
  fingerprint per row.
- **Inputs it composes:** the contact provenance rows from `ContactProvenanceProjection`
  (DRG-206, sim-clock freshness + information-quality flags, read off the [order log](order-log-runtime.md));
  the [sensor-to-shooter chain](sensor-to-shooter-chain.md) from `SensorToShooterProjection` (DRG-207);
  and the C2 authority disposition from `C2AuthorityProjector` (DRG-209, ROE + skill lane + required
  approval + track source). All three are pure order-log/sim projections — **never** UI selection,
  hover, camera, or panel state (ADR-010).
- **Related:** the shared foundation the chain folds is [kill-chain-contact-state.md](kill-chain-contact-state.md);
  the contact lifecycle behind the `Stale` / `Lost` / degraded overlays is
  [detection-pipeline.md](detection-pipeline.md) and [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md);
  the sibling drop-reason read-model over the same kill-chain is
  [track-custody-drop-reason-ledger.md](track-custody-drop-reason-ledger.md); the general read-model
  rules are [c2-projection-layer.md](c2-projection-layer.md); the read-only presentation seam a future
  host would bind through is [c2-presentation-bridges.md](c2-presentation-bridges.md).

---

## The pipeline at a glance

```
DecisionLog (order log)  +  currentSimTick  +  C2AuthorityProjectionContext
   (+ optional fireControl / shooters / catalog / weaponId / staleThreshold /
      orbatUnits / commsDisplay)
        │
        ▼  TargetabilityAcceptProjection.Project(log, tick, authorityCtx, …)   — pure, no side effects
   ContactProvenanceProjection   →  provenance rows (freshness + quality flags)
   SensorToShooterProjection     →  four-link chains (phase + break cause)
   C2AuthorityProjector          →  ROE / targeting / action dispositions
        │
        ▼  contact id set = union(provenance ids, chain ids), sorted ContactId ordinal
        ▼  per contact: ResolveDisposition(provenance, chain, authority)   — fail closed
   Permitted   iff provenance clears AND chain.IsComplete AND authority permits targeting
   Withheld    otherwise, with the first-matching named cause (precedence below)
        │
        ▼  TargetabilityAcceptFingerprint.Compute(snapshot)
   replay-stable canonical string ("tac:empty" | "tac:c=<n>|…")
```

A `null` log returns `TargetabilityAcceptSnapshot.Empty`; so does a `null` authority projection or an
empty union of contacts. The full `Project` overload projects all three children off the same
`DecisionLog` + `currentSimTick`; the second overload takes pre-built `ContactProvenanceSnapshot` /
`SensorToShooterSnapshot` / `C2AuthorityProjection` so a caller that already computed them can compose
**without re-projecting**.

---

## Disposition resolution (the precedence that matters)

`ResolveDisposition(provenance, chain, authority)` walks a fixed, **fail-closed** order and returns on
the first match. The whole point is that a withheld row always names *one* cause, and the earliest gap
in the pipeline wins:

1. **`provenance is null`** ⇒ `Withheld` / `MissingProvenance`. A chain-only row (no provenance) must
   **never** become `Permitted`, even if the chain is complete and authority permits — this is the
   fail-closed guard.
2. **provenance quality `CatalogMiss`** ⇒ `Withheld` / `CatalogMiss`.
3. **provenance `Freshness == Stale`** ⇒ `Withheld` / `Stale`.
4. **provenance quality `SilentComms` *and* `OutOfCommsUnknown`** ⇒ `Withheld` / `SilentComms`.
5. **`chain is null` or `!chain.IsComplete`** ⇒ `Withheld`, cause = `FormatSensorToShooterCause` of the
   chain's `PrimaryBreakCause` (`LostSensor` / `StaleTrack` / `NoFireControl` / `NoEligibleShooter` /
   `DegradedTrack`; a `null` chain defaults to `StaleTrack`).
6. **authority targeting `Withheld`** ⇒ `Withheld`, cause = the authority `ReasonCode` (e.g.
   `WeaponsTight`, `RoeHoldFire`, `SharedTrackNoRelease`) or `ApprovalRequired` if the code is absent.
7. **authority targeting `ApprovalRequired`** ⇒ `Withheld`, cause = the authority `ReasonCode` (e.g.
   `WeaponsReleaseRequired`) or `ApprovalRequired`.
8. otherwise ⇒ **`Permitted`** / `None`.

Note the ordering consequence: **provenance gaps outrank chain gaps outrank authority gaps.** A stale
contact reads `Stale` even if its ROE would also withhold it, because provenance is checked first. The
`TargetId` on each row is resolved as `provenance.Source.TargetId` → `chain.TargetId` → empty string.

---

## What a row carries

`TargetabilityAcceptContactRow` is an immutable presentation-facing record — one contact's acceptance
disposition plus the three child projections it was composed from (sim/order-log truth only):

| Field | Meaning |
|-------|---------|
| `ContactId` / `TargetId` | The track and the hostile it points at (target resolved provenance-first, then chain). |
| `Disposition` | `Permitted` or `Withheld` — the fail-closed composition verdict. |
| `WithheldCauseCode` | The single named cause when `Withheld` (`None` when `Permitted`); never empty on a withheld row. |
| `Provenance` | The `ContactProvenanceState` child row, or `null` (a `null` here forces `Withheld` / `MissingProvenance`). |
| `SensorToShooter` | The `SensorToShooterChain` child, or `null`. |
| `Authority` | The `C2AuthorityProjection` child (ROE + targeting + per-action dispositions). |

Rows are emitted in `ContactId` (`StringComparison.Ordinal`) order over the **union** of provenance and
chain contact ids, for a stable, replay-safe snapshot.

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **Fail-closed composition** | `Permitted` requires provenance present + clear, `chain.IsComplete`, and authority permitting targeting. Any missing/failed child ⇒ `Withheld`. A `null` authority or empty union ⇒ `Empty`. |
| **No silent withheld cause** | A `Withheld` row never carries an empty `WithheldCauseCode` and never `None`. Operators always see *why* a contact is not targetable. |
| **Earliest-gap wins** | The precedence is provenance → chain → authority; the first gap sets the cause, so the row names the root gap, not a later symptom. |
| **Pure / read-only** | `Project` consumes a read-only `DecisionLog` (or pre-built child snapshots) plus optional readers and returns immutable records. It never enqueues orders, resolves combat, mutates the log, touches `DelegationBridge` / `SimulationSession`, or reads the wall clock. |
| **Sim-authored inputs** | Provenance, chain, and authority all derive from sim / order-log facts and the sim-authored fire-control / shooter seams — never UI selection (ADR-010). |
| **Deterministic / replay-stable** | Same inputs ⇒ identical rows and fingerprint. Ordinal ordering, invariant culture, no RNG, no wall clock. |
| **Off the fingerprint** | The acceptance fingerprint is a projection artifact for equality/regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

Freshness uses `ContactProvenanceProjection.DefaultStaleThresholdTicks` (30) unless overridden; a
scenario `commsDisplay` `degradedStaleThresholdDivisor` can tighten staleness under degraded comms, so a
contact that is fresh under the default divisor can still be withheld `Stale` (see the divisor test
below). This projection owns no separate freshness clock — it inherits provenance's.

---

## The fingerprint

`TargetabilityAcceptFingerprint.Compute` yields `tac:empty` for a null/empty snapshot, else
`tac:c=<contactCount>` followed by one `|`-delimited segment per contact. Each contact segment packs
`ContactId`, `TargetId`, the integer `Disposition`, `WithheldCauseCode`, then the child fingerprints:
the provenance row via `ContactProvenanceFingerprint.Compute` (over a single-row snapshot, or the
`ContactProvenanceSnapshot.Empty` fingerprint when `null`), the chain via
`SensorToShooterProjection.ComputeFingerprint` (or the literal `sts:empty` when `null`), and an
`auth:`-prefixed authority string (ROE level + targeting disposition/reason + engage-allowed flag +
targeting disposition/reason + pending approval, then one `;`-delimited sub-segment per action sorted by
action int). Composing each child's own fingerprint means a change in any child projection surfaces here
too.

> **Note (accuracy):** like the [chain fingerprint](sensor-to-shooter-chain.md#the-fingerprint) and the
> [custody fingerprint](track-custody-drop-reason-ledger.md#the-fingerprint), this joins raw id/cause
> strings with `,` / `;` / `|` and does **not** length-prefix them, so it is not hardened against a
> separator embedded in an id or reason code colliding two distinct snapshots. It is a canonical string
> for equality/regression assertions on the sim-clean ids the projection produces — treat it as such,
> not as a collision-resistant hash. If ids or cause codes ever admit user/free text, length-prefix the
> segments and update the fingerprint tests.

The `TargetabilityAcceptChildFingerprints` record in the types (a `Provenance` / `SensorToShooter` /
`Authority` triple) is declared but **not** consumed by the shipped `Compute`, which composes the child
fingerprints inline per row; treat that record as reserved surface rather than the live layout.

---

## Extending without breaking replay

1. **Adding a withheld cause?** Add the `TargetabilityAcceptCauseCodes` constant, slot it into the
   `ResolveDisposition` precedence at the right rung (provenance → chain → authority), and pin the new
   branch with a projection test. Keep the "no withheld row is empty / `None`" invariant. If the cause
   comes from a child projector, prefer re-exporting that child's stable reason code (as the
   sensor-to-shooter and authority causes already are) so the label stays single-sourced.
2. **Adding a child projector to the composition?** Extend both `Project` overloads (project it in the
   log overload; accept its snapshot in the pre-built overload), fold it into `ResolveDisposition` in
   precedence order, **and** add it to `TargetabilityAcceptFingerprint` — a child the fingerprint
   ignores is a silent replay hole. Update the fingerprint tests.
3. **Changing the union / ordering?** Keep the contact set the union of every child's ids, sorted by
   `ContactId` ordinal; the snapshot's replay-stable order depends on it.
4. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else).

---

## Tests that pin this doc

All green as of writing (DRG-219) — 11 NUnit cases in the core Delegation test assembly (headless
`dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`TargetabilityAcceptProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/TargetabilityAccept/TargetabilityAcceptProjectionTests.cs) | The `Permitted` path (fresh provenance + complete chain + authority permits targeting); each `Withheld` cause named from the right child — `Stale` from provenance freshness, `CatalogMiss` from a provenance quality flag, `NoFireControl` from the sensor-to-shooter chain's `PrimaryBreakCause`, and `WeaponsTight` from the authority reason code under `WeaponsTight` ROE; the "no withheld row uses an empty cause code" invariant across HoldFire / WeaponsTight / stale rows; the degraded-comms `degradedStaleThresholdDivisor` tipping a would-be-fresh contact to `Withheld` / `Stale`; the pre-built child-snapshot overload composing without re-projecting; the fail-closed guard where a `null` provenance with a complete chain and permitting authority still withholds `MissingProvenance`; an empty log yielding `Empty` + `tac:empty`; and the replay-stable fingerprint for identical inputs. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~TargetabilityAccept"
```

---

*Verified against source at the paths above. If you change the disposition precedence, a cause code, the
composed child set, or the fingerprint layout, update this doc (and, once a host consumes it, the
consuming presentation doc) together.*
