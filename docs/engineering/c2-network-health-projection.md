# C2 network-health projection — link health, partitions & last-known contributors (DRG-214)

The **C2 network-health projection** folds the global comms state, the friendly datalink topology,
and the order-log contact picture into a single replay-stable **network-health snapshot**: an
aggregate `Healthy` / `Degraded` / `Partitioned` verdict, a per-link health row set, the list of
track contributors that a partition has frozen to *last-known*, and the modeled paths that no longer
carry live datalink updates. It is a pure, engine-agnostic read model in
[`ProjectAegis.Delegation/C2Network/`](../../src/ProjectAegis.Delegation/C2Network/) — one static
`Project(...)` call, no mutation, no RNG, no wall-clock.

> **Scope / boundary (DRG-214, Combat UX Slice A):** this is a **headless** projection — it derives
> everything from a `DecisionLog`, a friendly unit list, and the catalog link table; it never
> mutates the sim or the order log, and it is **not wired to any host today** (the Slice A chrome,
> DRG-190, renders it later). It has **no `UnityEngine` dependency** and never touches
> `DelegationBridge`. Live capability is **never fabricated**: when a path cannot carry updates the
> projection preserves the *last-known* contributor rather than inventing a fresh track. The
> subsystem has its own [`C2NetworkHealthFingerprint`](#determinism--tests) for replay-stability
> tests; it is **not** an input to the Baltic v2 `SimWorldHash`.

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`C2NetworkHealthProjector`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkHealthProjector.cs) | `static` | The fold: `Project(log, friendlyUnitIds, catalogLinks, currentSimTick, linkStatusOverrides?)` → snapshot. |
| [`C2NetworkHealthSnapshot`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkHealthSnapshot.cs) | `sealed record` | `(NetworkHealth, CommsState, CommsNodeId, Links, LastKnownContributors, LostPaths)`. |
| [`C2NetworkHealthLevel`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkHealthLevel.cs) | `enum` | Aggregate mesh health `Healthy=0` / `Degraded=1` / `Partitioned=2`. |
| [`C2LinkHealth`](../../src/ProjectAegis.Delegation/C2Network/C2LinkHealth.cs) | `enum` | Per-link `Healthy=0` / `Degraded=1` / `Partitioned=2`. |
| [`C2NetworkLinkHealthEntry`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkLinkHealthEntry.cs) | `sealed record` | `(FromUnitId, ToUnitId, LinkType, Health, StalenessTicks, IsLiveCapability, AffectedContributorUnitIds)`. |
| [`C2NetworkContributor`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkContributor.cs) | `sealed record` | A track contributor frozen as last-known: `(UnitId, ContactId, TargetId, LifecycleState, LastKnownSimTick, IsLiveCapability)`. |
| [`C2NetworkLostPath`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkLostPath.cs) | `sealed record` | A modeled path that lost live updates: `(FromUnitId, ToUnitId, LinkType, LastKnownSimTick)`. |
| `C2NetworkHealthProjector.LinkStatusOverride` | `sealed record` (nested) | `(FromUnitId, ToUnitId, Status)` — models a single-link partition without mutating the order log. |
| [`C2NetworkHealthFingerprint`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkHealthFingerprint.cs) | `static` | Canonical `Compute(snapshot)` string for determinism tests. |

---

## The fold — `C2NetworkHealthProjector.Project`

```
Project(
    DecisionLog log,                              // order-log source (comms transitions + contacts)
    IReadOnlyList<string> friendlyUnitIds,        // the mesh membership
    IReadOnlyList<CatalogLinkEntry> catalogLinks, // datalink topology
    ulong currentSimTick,                         // "now" for staleness math
    IReadOnlyList<LinkStatusOverride>? overrides = null)
  → C2NetworkHealthSnapshot
```

All three arguments (`log`, `friendlyUnitIds`, `catalogLinks`) are null-checked and throw
`ArgumentNullException`. The fold runs in order:

1. **Comms state** — [`CommsStateProjection.Project(log)`](../../src/ProjectAegis.Delegation/Projection/CommsStateProjection.cs)
   yields the global `CommsState` (`Nominal` / `Degraded` / `Denied`) and the reporting node id.
2. **Datalink mesh** — `DatalinkUnitPairFeed.BuildMesh(friendlyUnitIds, catalogLinks)` builds the
   friendly pair mesh; if it is empty the projector short-circuits to an **empty snapshot** whose
   aggregate health is comms-state-mapped directly (`Denied→Partitioned`, `Degraded→Degraded`, else
   `Healthy`). Otherwise [`DatalinkPictureProjection.Project`](../../src/ProjectAegis.Delegation/Projection/DatalinkPictureProjection.cs)
   produces the edge rows with the base status resolved from comms
   (`DatalinkUnitPairFeed.ResolveEdgeStatus`).
3. **Contacts** — a **per-observer** contact fold (see below) keeps each receiver's own datalink
   share alive.
4. **Partition set**, **link rows**, **last-known contributors**, **lost paths**, and the
   **aggregate health** are derived from those inputs (below).

### Edge-status resolution precedence

Each edge's effective status is resolved by a fixed precedence — the load-bearing rule is that
**global denial wins over per-link overrides**, so a caller cannot resurrect an `Up`/live link under
`Denied`:

1. `CommsState.Denied` → `Down` (always, ignoring any override).
2. else a matching `LinkStatusOverride` (keyed by the ordinal-sorted endpoint pair) wins.
3. else under `CommsState.Degraded`, an `Up` edge is demoted to `Degraded` (an already
   `Degraded`/`Down` edge is left as-is).
4. else the edge keeps its own datalink status.

Status tokens are the `DatalinkPictureProjection` constants `Up` / `Degraded` / `Down`; they map to
link health as `Degraded→Degraded`, `Down→Partitioned`, everything else `→Healthy`.

### Partition reachability

`Partitioned` units are computed by flooding reachability from the **lowest-ordinal** friendly unit
across *traversable* edges (status `Up` **or** `Degraded` — a `Degraded` link still carries the
picture). Under `CommsState.Denied` every unit except the first is partitioned outright. Any unit the
flood cannot reach is `Partitioned`.

### Per-link rows

Edges are emitted ordinal-sorted by `(From, To, LinkType)`. Each row records the resolved `Health`,
whether it `IsLiveCapability` (**`true` unless `Partitioned`** — a `Degraded` link stays live because
peer shares still update), the `StalenessTicks`, and the `AffectedContributorUnitIds`:

- **Affected contributors** are populated only for `Partitioned` links. Seeds are the partitioned
  endpoints, then the set is expanded to the **entire disconnected component** the cut isolates (not
  just the two endpoints).
- **Staleness** is `0` for any non-partitioned link. For a partitioned link it is the gap between
  `currentSimTick` and the freshest `dl-*` contact observed by the affected set (falling back to the
  edge endpoints when there is no affected set); it returns `1` when there is no such contact or when
  `currentSimTick <= maxTick`, otherwise `currentSimTick - maxTick`. Preferring the affected set
  keeps a fresh **near-side** contact from masking **far-side** staleness after a partition.

### Last-known contributors

Last-known rows are produced **only** when the mesh has a non-live link (comms `Denied`, or any
`Partitioned` link). A `dl-*` share is frozen (`IsLiveCapability = false`) when its observer is
`Denied`-wide, is in the partition set, or is inside any link's affected-contributor set; reachable
endpoints of a down edge stay **live** and are skipped. A merely `Degraded` mesh stays live and
produces **no** last-known rows.

### Lost paths

For each `Partitioned`, non-live link the projector emits a `C2NetworkLostPath` whose
`LastKnownSimTick` is `currentSimTick - StalenessTicks` (clamped to `0` when staleness ≥ now), bumped
up by the freshest affected-contributor contact tick. Lost paths are ordinal-sorted by
`(From, To, LinkType)`.

### Aggregate network health

`ResolveNetworkHealth` returns `Partitioned` if comms is `Denied` **or** any link is `Partitioned`;
`Degraded` if comms is `Degraded` **or** any link is `Degraded`; otherwise `Healthy`.

---

## The per-observer contact fold (a real footgun)

The projector does **not** reuse
[`ContactPictureProjection`](../../src/ProjectAegis.Delegation/Projection/ContactPictureProjection.cs)
`.Project`. That projection collapses tracks by `ContactId` only and drops the peer shares of the
same `dl-{targetId}` track — which would erase exactly the receiver-side share a partition needs to
freeze. Instead this subsystem keys its fold by **`(ObserverId, ContactId)`** so each receiver keeps
its own `dl-*` share; a `"Lost"` transition removes the keyed track, and the fold is ordered by
`SimTick` then `SequenceId`. Datalink-shared contacts are recognised by the `dl-` `ContactId` prefix.

---

## Determinism & tests

- **Pure & deterministic.** No RNG, no wall-clock; every unit list, edge list, contributor list, and
  fingerprint segment iterates `StringComparer.Ordinal`. The projection reads the `DecisionLog` and
  the catalog link table only — it is **off the `SimWorldHash`** and does not write the order log.
- **Own fingerprint.** [`C2NetworkHealthFingerprint.Compute`](../../src/ProjectAegis.Delegation/C2Network/C2NetworkHealthFingerprint.cs)
  emits a single canonical string — `C2NetworkHealth|<level>|<comms>|<node>` followed by ordinal-sorted
  `|L|` link rows, `|C|` contributor rows, and `|P|` lost-path rows — so determinism tests can diff one
  value. Identical snapshots yield identical fingerprints; a differing partition override changes it.

| Suite | Location | Count |
|-------|----------|-------|
| `C2NetworkHealthProjectorTests` | [`src/ProjectAegis.Delegation.Tests/C2Network/C2NetworkHealthProjectorTests.cs`](../../src/ProjectAegis.Delegation.Tests/C2Network/C2NetworkHealthProjectorTests.cs) | 9 |
| `C2NetworkHealthFingerprintTests` | [`src/ProjectAegis.Delegation.Tests/C2Network/C2NetworkHealthFingerprintTests.cs`](../../src/ProjectAegis.Delegation.Tests/C2Network/C2NetworkHealthFingerprintTests.cs) | 2 |

Related: [comms-degradation-runtime.md](comms-degradation-runtime.md) (the `CommsTimelineSimulator`
that drives the `CommsState` this projection folds) ·
[datalink-share-lag-resolver.md](datalink-share-lag-resolver.md) (the bind-time link-latency side of
the datalink picture) · [cec-mesh-runtime.md](cec-mesh-runtime.md) (the *Sim*-side cooperative
composite-track mesh — a different subsystem: fusion/fire-control, not C2 link health) ·
[c2-projection-layer.md](c2-projection-layer.md) (the read-model family incl. `DatalinkPictureProjection`
and `ContactPictureProjection`).
