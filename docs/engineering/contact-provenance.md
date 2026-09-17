# Contact provenance projection — developer guide

This is the **contact-provenance** read-model (`ProjectAegis.Delegation/Projection/`, DRG-206): a
pure, deterministic projection that folds the order-log **contact picture** plus **comms state** into
one provenance row per active track answering *"where did this contact come from, how confident are we,
how fresh is it, and which named information-quality gaps apply right now?"*. It is the **provenance
leg** of the Combat-UX Slice A read-model family — the earliest, highest-precedence input the
[targetability-accept](targetability-accept.md) composition (DRG-219) fails closed on, and one of the
inputs the [track-custody drop-reason ledger](track-custody-drop-reason-ledger.md) (DRG-222) folds. Its
load-bearing promise is **never-implicit quality**: catalog-miss, staleness, and silent-comms are each
surfaced as a *named* flag, never left empty for an operator to infer.

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It is **headless projection-only** — it performs no Unity or tick-path
mutation and has no host or bridge consumer of its own yet; it is exercised by its own test fixture and
composed by the two downstream read-models above. Verified against source and pinned by the tests at the
end.

- **Types (core):**
  [`ContactProvenance.cs`](../../src/ProjectAegis.Delegation/Projection/ContactProvenance.cs) —
  the `ContactProvenanceFreshness` (`Fresh` = 0 / `Stale` = 1, CMD-29.6), the `ContactProvenanceConfidence`
  (`Unknown` = 0 / `Low` = 1 / `Medium` = 2 / `High` = 3), the combinable `[Flags]`
  `ContactProvenanceQualityState` (`None` = 0 / `CatalogMiss` = 1 / `Stale` = 2 / `SilentComms` = 4), the
  `ContactProvenanceSource` (observer + resolved target + `SourceRef`), the `ContactProvenanceLastKnown`
  (lifecycle + target + last sim tick/time), the per-contact `ContactProvenanceState` row, and the
  ordered `ContactProvenanceSnapshot` (+ `Empty`).
- **Projection (core):**
  [`ContactProvenanceProjection.cs`](../../src/ProjectAegis.Delegation/Projection/ContactProvenanceProjection.cs) —
  the two `Project(…)` overloads (from a `DecisionLog` — which projects the contact picture and comms
  state for you — or from a pre-built `IReadOnlyList<ContactPictureEntry>` plus an explicit `CommsState`),
  the per-contact fold, and the `ContactId`-ordinal sort.
- **Fingerprint (core):**
  [`ContactProvenanceFingerprint.cs`](../../src/ProjectAegis.Delegation/Projection/ContactProvenanceFingerprint.cs) —
  `Compute(ContactProvenanceSnapshot?)` → replay-stable canonical string over every row.
- **Inputs it folds:** the contact picture from `ContactPictureProjection` (contact/observer/target ids,
  lifecycle state, last sim tick/time, read off the [order log](order-log-runtime.md)); the comms clock
  from `CommsStateProjection` (`Nominal`/`Degraded`/`Denied`, see
  [comms-degradation-runtime.md](comms-degradation-runtime.md)); an optional `ICatalogReader` for the
  catalog-miss check; an optional scenario `ScenarioCommsDisplaySettings` for the degraded-staleness
  divisor; and an optional orbat unit list (`ScenarioOrbatUnitDto`) for instance-id → platform-id
  resolution. All are pure order-log/sim/catalog facts — **never** UI selection, hover, camera, or panel
  state (ADR-010).
- **Related:** the composition that fails closed on this row is [targetability-accept.md](targetability-accept.md);
  the custody ledger that folds it is [track-custody-drop-reason-ledger.md](track-custody-drop-reason-ledger.md);
  the phase/loss foundation of the family is [kill-chain-contact-state.md](kill-chain-contact-state.md);
  the raw contact lifecycle behind `Stale` is [detection-pipeline.md](detection-pipeline.md) /
  [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md); the general read-model rules are
  [c2-projection-layer.md](c2-projection-layer.md).

---

## The pipeline at a glance

```
DecisionLog (order log)  +  currentSimTick
    (+ optional catalog / commsDisplay / staleThresholdTicks / orbatUnits)
        │
        ▼  ContactProvenanceProjection.Project(log, tick, …)   — pure, no side effects
   CommsStateProjection.Project(log)      →  current CommsState
   ContactPictureProjection.Project(log)  →  active ContactPictureEntry rows
        │
        ▼  effectiveStale = max(1, staleThreshold / commsDivisor)   — divisor 1 unless Degraded/Denied
        ▼  per contact: ProjectContact(entry, tick, commsState, effectiveStale, catalog, unitPlatformMap)
   age        = tick ≥ lastTick ? tick − lastTick : 0
   Freshness  = age > effectiveStale ? Stale : Fresh
   Confidence = lifecycle → High/Medium/Low/Unknown
   Quality    = CatalogMiss? | Stale? | SilentComms?   (flags; never left implicit)
        │
        ▼  Array.Sort by ContactId (StringComparison.Ordinal)
        ▼  ContactProvenanceFingerprint.Compute(snapshot)   →  "cp:empty" | "cp:c=<n>|…"
```

A `null` log, or a log with no active contacts, returns `ContactProvenanceSnapshot.Empty`. The
pre-built overload short-circuits `Empty` on a `null`/empty contact list.

---

## What a row carries

`ContactProvenanceState` is an immutable, presentation-facing record — one active track's provenance,
derived only from sim / order-log / catalog truth:

| Field | Meaning |
|-------|---------|
| `ContactId` | The track id (rows are emitted in `ContactId` ordinal order). |
| `Source` | `ContactProvenanceSource(ObserverId, TargetId, SourceRef)` — who saw it, what it points at, and the canonical `observer:<id>\|target:<id>` reference string. |
| `Confidence` | `High` (`Identified`) / `Medium` (`Classified`, `DegradedL1`, `DegradedL2`) / `Low` (`Detected`) / `Unknown` (anything else) — from sensor lifecycle, **not** UI. |
| `Freshness` | `Fresh` or `Stale`, decided by `AgeTicks` vs the effective stale threshold. |
| `AgeTicks` | `currentSimTick − LastSimTick`, clamped to `0` when the tick is behind the last update. |
| `LastKnown` | `ContactProvenanceLastKnown(LifecycleState, TargetId, LastSimTick, LastSimTime)` — last confirmed facts for the ghost/stale hint. |
| `OutOfCommsUnknown` | `true` only under `CommsState.Denied` — the track may have moved unseen. |
| `QualityState` | The `[Flags]` set of named gaps (`CatalogMiss` / `Stale` / `SilentComms`), combinable and never left implicit. |

---

## Quality resolution (the named gaps)

`ProjectContact` sets three independent, combinable quality flags; unlike the downstream fail-closed
compositions there is **no single-cause precedence here** — a row can carry several flags at once:

1. **`CatalogMiss`** ⇒ the target id resolves to no catalog platform. Skipped when there is no
   `ICatalogReader` or the target id is empty. A hit is `TryGetPlatformDomain` **or**
   `TryGetPlatformPosition` on the target id; if that misses, an orbat `unitId → platformId` map (from
   `orbatUnits`) is tried so a **cloned instance** resolves via its platform; only if both miss is the
   flag set.
2. **`Stale`** ⇒ `AgeTicks > effectiveStaleThresholdTicks`. Also sets `Freshness = Stale`.
3. **`SilentComms`** ⇒ comms are `Degraded` **or** `Denied`. `OutOfCommsUnknown` is set **only** for
   `Denied` (the stricter "we may have lost the track entirely" state).

### Comms-tightened staleness

The effective stale threshold is `max(1, staleThresholdTicks / divisor)` where the divisor comes from
`CommsTrackStaleness.StaleThresholdDivisor`: `1` under `Nominal`, and the scenario
`ScenarioCommsDisplaySettings.DegradedStaleThresholdDivisor` (clamped `[1, 8]`) under `Degraded` or
`Denied`. So a contact that is fresh under nominal comms can read `Stale` under degraded comms **and**
carry `SilentComms` at the same time (see the degraded-comms test below). The default base threshold is
`DefaultStaleThresholdTicks` (30), matching the kill-chain / sensor GDD default; pass
`staleThresholdTicks` to override.

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **Named quality, never implicit** | Catalog-miss, staleness, and silent-comms are always surfaced as explicit `ContactProvenanceQualityState` flags — never left `None` when the underlying condition holds. |
| **Sim-authored inputs** | Confidence comes from sensor lifecycle, freshness from the sim clock, catalog-miss from the catalog reader, silent-comms from the comms clock — **never** UI selection / hover / camera / panel visibility (ADR-010; a reflection test forbids those property names). |
| **Pure / read-only** | `Project` consumes a read-only `DecisionLog` (or pre-built entries) plus optional readers and returns immutable records. It never enqueues orders, resolves combat, mutates the log, touches `DelegationBridge` / `SimulationSession`, or reads the wall clock. |
| **Deterministic / replay-stable** | Same inputs ⇒ identical rows and fingerprint. `ContactId` ordinal ordering, invariant culture, no RNG, no wall clock. |
| **Off the fingerprint** | The provenance fingerprint is a projection artifact for equality/regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

---

## The fingerprint

`ContactProvenanceFingerprint.Compute` yields `cp:empty` for a null/empty snapshot, else `cp:c=<n>`
(the contact count) followed by one `|`-delimited segment per contact. Each contact segment packs, in
order, `ContactId`, `ObserverId`, `TargetId`, `SourceRef`, the integer `Confidence`, the integer
`Freshness`, `AgeTicks`, the last-known `LifecycleState` / `TargetId` / `LastSimTick`, the `LastSimTime`
formatted `"R"` under invariant culture, `OutOfCommsUnknown` as `1`/`0`, and the integer `QualityState`
flag set.

> **Note (accuracy):** like the [kill-chain](kill-chain-contact-state.md#the-fingerprint),
> [chain](sensor-to-shooter-chain.md#the-fingerprint), and [custody](track-custody-drop-reason-ledger.md#the-fingerprint)
> fingerprints, this joins raw id strings with `,` / `|` and does **not** length-prefix them, so it is
> not hardened against a separator embedded in an id colliding two distinct snapshots. It is a canonical
> string for equality/regression assertions on the sim-clean ids the projection produces — treat it as
> such, not as a collision-resistant hash. If ids ever admit user/free text, length-prefix the segments
> and update the fingerprint tests.

---

## Extending without breaking replay

1. **Adding a quality flag?** Add the `ContactProvenanceQualityState` power-of-two constant, set it in
   `ProjectContact` from a sim/order-log/catalog fact (never UI), and pin the new branch with a
   projection test. Keep the "named, never implicit" invariant.
2. **Adding a confidence rung or lifecycle mapping?** Update `ResolveConfidence` and pin it; keep the
   ordinal-string comparison and the `Unknown` fallback.
3. **Changing the freshness clock?** Keep it sim-tick only (`AgeTicks` from `LastSimTick`), keep the
   `max(1, …)` clamps, and keep the comms divisor sourced from `CommsTrackStaleness` so degraded-comms
   staleness stays single-sourced with the rest of the comms family.
4. **Touching the row shape or ordering?** Keep the `ContactId` ordinal sort, add the new field to
   `ContactProvenanceFingerprint` (a field the fingerprint ignores is a silent replay hole), update the
   fingerprint tests, and remember every downstream composition
   ([targetability-accept](targetability-accept.md), [track-custody](track-custody-drop-reason-ledger.md))
   folds this row — check them too.
5. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else).

---

## Tests that pin this doc

All green as of writing (DRG-206) — 12 NUnit cases in the core Delegation test assembly (headless
`dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`ContactProvenanceProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/ContactProvenanceProjectionTests.cs) | An empty log yielding `Empty` + `cp:empty`; a fresh track publishing source / `Medium` confidence / `Fresh` / zero age / last-known; a stale track naming the `Stale` quality flag + `AgeTicks`; `Denied` comms setting `OutOfCommsUnknown` + `SilentComms`; a `CatalogMiss` named when the target is absent from the reader; a **cloned instance** resolving via its orbat `platformId` and **not** catalog-missing; an orbat map whose platform is still missing from the catalog re-flagging `CatalogMiss`; a bare instance id with no orbat map still catalog-missing; degraded comms accelerating staleness via the `DegradedStaleThresholdDivisor` (tipping a would-be-fresh contact to `Stale` + `SilentComms` while `OutOfCommsUnknown` stays false); the replay-stable fingerprint for identical inputs and the `ContactId` ordinal sort (both direct-list and unordered-picture); and the ADR-010 reflection guard that the provenance DTOs expose no `selection` / `hover` / `camera` / `visible` / `visibility` / `selected` property. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~ContactProvenance"
```

---

*Verified against source at the paths above. If you change the quality flags, the confidence mapping,
the freshness clock, the catalog-miss resolution, or the fingerprint layout, update this doc (and the
downstream [targetability-accept](targetability-accept.md) / [track-custody](track-custody-drop-reason-ledger.md)
docs) together.*
