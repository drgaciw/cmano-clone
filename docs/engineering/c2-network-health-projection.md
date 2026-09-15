# C2 network / datalink health read-model

> **Scope.** The pure, headless read-model that turns the friendly datalink mesh into a
> **`Healthy` / `Degraded` / `Partitioned`** command-and-control health picture — per-link status,
> which units are cut off, and which shared tracks have gone stale after a partition. It lives in
> [`ProjectAegis.Delegation/C2Network/`](../../src/ProjectAegis.Delegation/C2Network/) (DRG-214,
> Combat UX Slice A wave 2) and follows the read-only projection contract of ADR-010 §2–3 / ADR-007
> / ADR-001: it is derived from the order log + link catalog, does **no mutation**, has **no
> `UnityEngine` dependency**, and is **off the sim hot path and the replay fingerprint**. The Unity
> render layer (DRG-190) binds it later; today it is headless and test-pinned.

---

## What it folds

`C2NetworkHealthProjector.Project` composes three existing pure projections into one snapshot:

```
C2NetworkHealthProjector.Project(
    DecisionLog log,
    IReadOnlyList<string> friendlyUnitIds,
    IReadOnlyList<CatalogLinkEntry> catalogLinks,
    ulong currentSimTick,
    IReadOnlyList<LinkStatusOverride>? linkStatusOverrides = null)
  → C2NetworkHealthSnapshot
```

| Input fold | Source | Role |
|-----------|--------|------|
| Global comms state | `CommsStateProjection.Project(log)` | `Nominal` / `Degraded` / `Denied` + `NodeId`. |
| Datalink topology | `DatalinkUnitPairFeed.BuildMesh` + `DatalinkPictureProjection.Project` | The friendly mesh edges and their `Up` / `Degraded` / `Down` status. |
| Shared contacts | an **observer-keyed** order-log contact fold (private to this projector) | The `dl-*` datalink shares used for staleness / last-known. |

`log`, `friendlyUnitIds`, and `catalogLinks` are required (`ArgumentNullException` otherwise). An
empty mesh returns an empty snapshot whose aggregate is mapped straight from comms state.

## The snapshot

```csharp
public sealed record C2NetworkHealthSnapshot(
    C2NetworkHealthLevel NetworkHealth,          // Healthy=0 / Degraded=1 / Partitioned=2
    CommsState CommsState,                        // Nominal=0 / Degraded=1 / Denied=2
    string CommsNodeId,
    IReadOnlyList<C2NetworkLinkHealthEntry> Links,
    IReadOnlyList<C2NetworkContributor> LastKnownContributors,
    IReadOnlyList<C2NetworkLostPath> LostPaths);
```

- **`C2NetworkLinkHealthEntry`** `(FromUnitId, ToUnitId, LinkType, Health, StalenessTicks, IsLiveCapability, AffectedContributorUnitIds)` — one per mesh edge, sorted `From → To → LinkType` (ordinal). `Health` is `C2LinkHealth` (`Healthy` / `Degraded` / `Partitioned`).
- **`C2NetworkContributor`** `(UnitId, ContactId, TargetId, LifecycleState, LastKnownSimTick, IsLiveCapability)` — a track share frozen as last-known because its observer is cut off. `IsLiveCapability` is always `false` here.
- **`C2NetworkLostPath`** `(FromUnitId, ToUnitId, LinkType, LastKnownSimTick)` — a modeled path that no longer carries live updates.

## Resolution rules

### Edge status (`ResolveEdgeStatus`)

1. **Global `Denied` wins** over everything — every edge resolves `Down`, so a caller override can
   never resurrect a live link under jamming/EMP.
2. Otherwise a per-link `LinkStatusOverride` (keyed by the ordinal-sorted endpoint pair) applies —
   this models a *single-link* partition without mutating the order log.
3. Otherwise under global `Degraded`, an `Up` edge is demoted to `Degraded` (other statuses pass
   through).
4. Otherwise the edge keeps its `DatalinkPictureProjection` status.

`Up`/`Degraded` map to `Healthy`/`Degraded`; `Down` maps to `Partitioned`.

### Partition (mesh reachability)

`ComputePartitionedUnits` runs a BFS from the **lowest ordinal** friendly unit over *traversable*
edges (`Up` or `Degraded`); any unit not reached is partitioned. Under global `Denied` every unit
except the first is partitioned. A `Partitioned` link then expands to the **full disconnected
component** it cuts off (`CollectAffectedContributors`), not just its two endpoints.

### Aggregate (`ResolveNetworkHealth`)

`Partitioned` if comms is `Denied` **or** any link is `Partitioned`; else `Degraded` if comms is
`Degraded` **or** any link is `Degraded`; else `Healthy`.

### Staleness & last-known (the load-bearing "honest picture" part)

- **`Degraded` stays live.** A degraded link (or degraded mesh) still carries peer shares, so its
  rows keep `IsLiveCapability = true`, `StalenessTicks = 0`, and produce **no** last-known /
  lost-path rows.
- **Only `Partitioned` / `Denied` freezes.** `StalenessTicks` on a cut link is
  `currentSimTick − <freshest dl-* share on the affected far side>` (min `1`). Crucially it prefers
  the *affected-contributor* set, so a fresher **near-side** share does not mask far-side
  staleness after a partition.
- **Never fabricate live capability.** Cut-off observers' `dl-*` shares are emitted as
  `C2NetworkContributor` last-known rows with `IsLiveCapability = false`; still-reachable endpoints
  of a down edge are **not** demoted.
- **Per-observer `dl-*` shares survive ContactId collisions.** The projector keys contacts by
  `(ObserverId, ContactId)` — unlike `ContactPictureProjection`, which collapses by `ContactId`
  only — so two receivers of the same `dl-<target>` track both keep their own share for last-known.

## Determinism

`C2NetworkHealthFingerprint.Compute` renders a stable string (`C2NetworkHealth|<level>|<comms>|<node>`
then ordinal-sorted `|L|…` link, `|C|…` contributor, and `|P|…` lost-path segments). Every list is
ordinal-sorted at build time and there is no wall-clock or RNG use, so the snapshot is byte-stable
for a given `(log, mesh, tick)`. This projection is **off** the `SimWorldHash` / order-log replay
fingerprint, so it cannot move the Baltic v2 hash `17144800277401907079`.

## Invariants

| Invariant | Why |
|-----------|-----|
| **Read-only / no `UnityEngine`** | Pure `Delegation/C2Network/` fold; the order log and catalog are inputs only. Headless-testable. |
| **Off the fingerprint** | Derived view; never appended to `DecisionLog`, so the replay hash is untouched. |
| **Global `Denied` beats overrides** | A caller can't paint a link `Up` while comms is denied. |
| **`Degraded` never freezes** | Peer shares still flow; only `Partitioned`/`Denied` emits last-known / lost-path rows. |
| **Never fabricate live capability** | Cut-off contributors are `IsLiveCapability = false`; reachable endpoints stay live. |
| **Per-observer `dl-*` keying** | `(ObserverId, ContactId)` fold preserves each receiver's share for a truthful partition picture. |
| **Deterministic ordinal ordering** | All rows sorted `From → To → LinkType` / `Unit → Contact`; fingerprint is replay-stable. |

## Extending it

- **Add a link-status source** — feed it as a `LinkStatusOverride` (endpoint pair + `Up`/`Degraded`/`Down`);
  the projector normalizes and applies it after the global-`Denied` short-circuit.
- **Add a snapshot field** — extend `C2NetworkHealthSnapshot` (and its row records) *and*
  `C2NetworkHealthFingerprint.Compute` in the same change so the fingerprint stays total; keep the
  new field ordinal-sorted.
- Nothing here needs a replay-golden re-bless — it is read-only presentation.

## Tests

| Suite | Assembly | Covers |
|-------|----------|--------|
| `C2NetworkHealthProjectorTests` (9) | `ProjectAegis.Delegation.Tests` (NUnit) | Healthy mesh (zero staleness), single-link partition (affected contributors + lost path, no fabricated capability), near-side staleness masking, global `Degraded` (live, no last-known), global `Denied` overriding a per-link `Up`, per-observer `dl-*` collision survival, full-disconnected-component cut, null-log throw. |
| `C2NetworkHealthFingerprintTests` (2) | `ProjectAegis.Delegation.Tests` (NUnit) | Replay-stable fingerprint text / ordering. |

## See also

- [c2-projection-layer.md](c2-projection-layer.md) — the datalink feed (`DatalinkPictureProjection`) and the read-only projection contract this follows.
- [comms-degradation-runtime.md](comms-degradation-runtime.md) — the global comms `Nominal`/`Degraded`/`Denied` model this folds in.
- [datalink-share-lag-resolver.md](datalink-share-lag-resolver.md) — the per-share datalink lag model behind the mesh contacts.
- [order-log-runtime.md](order-log-runtime.md) — the `DecisionLog` contact/comms source of truth.
