# Mission package & C2 node projection — developer guide

This is the **mission-package** read-model (`ProjectAegis.Delegation/C2Nodes/`, DRG-213, Combat-UX
Slice A): a pure, deterministic projection that folds **authored mission-package definitions** together
with **order-log evidence** (platform damage, task-org detach/rejoin, comms quality) into one
replay-stable snapshot of composable **C2 node elements** — answering *"which sensors, shooters,
relays, and C2 nodes make up this package right now, and is each one available, degraded to last-known,
or unavailable?"*. It keeps the **organic-vs-package** distinction explicit so a task re-org never
silently collapses a platform's organic sensor into the package track picture.

It lives in the engine-agnostic `ProjectAegis.Delegation` core (net8.0, no `UnityEngine`), so it runs
under plain `dotnet test`. It already has a **live host consumer**: the Unity adapter's
`CoordinationBridge` (command-review coordination surface) builds the snapshot and reads element roles
and availability. It is still a **read model** — it performs no Unity or tick-path mutation, and it
never appends to the order log it reads. Verified against source and pinned by the tests at the end.

- **Types (core):**
  [`PackageDefinition.cs`](../../src/ProjectAegis.Delegation/C2Nodes/PackageDefinition.cs) /
  [`PackageElementDefinition.cs`](../../src/ProjectAegis.Delegation/C2Nodes/PackageElementDefinition.cs) —
  the authoring-time composition input (`PackageId` / `Label` / `Elements`, each element an
  `ElementId` + `PlatformUnitId` + `C2NodeRole` + `CapabilityScope`);
  [`C2NodeRole.cs`](../../src/ProjectAegis.Delegation/C2Nodes/C2NodeRole.cs) (`Sensor` = 1 /
  `Shooter` = 2 / `Relay` = 3 / `C2` = 4);
  [`C2NodeAvailability.cs`](../../src/ProjectAegis.Delegation/C2Nodes/C2NodeAvailability.cs)
  (`Available` = 0 / `Unavailable` = 1 / `LastKnown` = 2);
  [`C2NodeMembershipKind.cs`](../../src/ProjectAegis.Delegation/C2Nodes/C2NodeMembershipKind.cs)
  (`Organic` = 0 / `Package` = 1) + [`C2NodeMembership.cs`](../../src/ProjectAegis.Delegation/C2Nodes/C2NodeMembership.cs);
  the per-element row [`C2NodeElement.cs`](../../src/ProjectAegis.Delegation/C2Nodes/C2NodeElement.cs);
  the per-package roll-up
  [`MissionPackageMembership.cs`](../../src/ProjectAegis.Delegation/C2Nodes/MissionPackageMembership.cs);
  and the ordered
  [`MissionPackageSnapshot.cs`](../../src/ProjectAegis.Delegation/C2Nodes/MissionPackageSnapshot.cs)
  (+ `Empty`).
- **Projection + fingerprint (core):**
  [`MissionPackageProjection.cs`](../../src/ProjectAegis.Delegation/C2Nodes/MissionPackageProjection.cs) —
  the `Project(...)` fold, the availability/membership/task-org resolution, the ordinal sorts, and the
  replay-stable `ComputeFingerprint(MissionPackageSnapshot?)` (no separate `*Fingerprint.cs` file — the
  canonical string is a static method on the projection).
- **Inputs it folds:** the authored `IReadOnlyList<PackageDefinition>` (composed outside the tick
  hotpath — scenario / fixture); an optional `DecisionLog` for **platform damage**
  (`PlatformDamageChanges`, one-way death on `NewHpPct ≤ 0`), **task-org** detach/rejoin
  (`GroupMemberDetaches` / `GroupMemberRejoins`, see
  [direct-control-override-runtime.md](direct-control-override-runtime.md)) and the **comms clock**
  (`CommsStateChanges`, see [comms-degradation-runtime.md](comms-degradation-runtime.md)); an optional
  authoritative `Func<string,bool> isPlatformAlive` gate per unit; and the sim tick/time to stamp on
  each element. All are pure order-log / sim / authoring facts — **never** UI selection, hover, camera,
  or panel state (ADR-010).
- **Related:** the task-org detach/rejoin producer is
  [direct-control-override-runtime.md](direct-control-override-runtime.md); the comms states that gate
  relay/C2 availability are [comms-degradation-runtime.md](comms-degradation-runtime.md); the platform
  damage that kills a node is [catalog-damage-readiness-runtime.md](catalog-damage-readiness-runtime.md)
  / [bda-contact-lifecycle-runtime.md](bda-contact-lifecycle-runtime.md); the general read-model rules
  are [c2-projection-layer.md](c2-projection-layer.md). Element availability here is package-capability
  state — **distinct** from the datalink link-paint of the DRG-214 C2 network-health projection.

---

## The pipeline at a glance

```
IReadOnlyList<PackageDefinition>   (+ optional DecisionLog / isPlatformAlive / tick / activePackageId)
        │
        ▼  MissionPackageProjection.Project(definitions, log?, isPlatformAlive?, tick, time, activePackageId?)
   OrderBy PackageId (ordinal)
   BuildPlatformAliveMap   →  isPlatformAlive ?? true, then order-log HP ≤ 0 ⇒ dead (one-way)
   BuildTaskOrgDetachedUnits →  fold detach/rejoin transitions by SequenceId
   FoldCommsState          →  latest CommsStateChange (default Nominal)
   FoldLatestPlatformDamage→  latest damage row per unit
        │
        ▼  per element (OrderBy ElementId, skip blank id/unit):
   Membership = CapabilityScope startsWith "organic-" ? Organic : Package
   Availability = ResolveAvailability(alive, damage, comms, role)   — see ladder below
   SourceRefs   = element:/unit:/package:/scope:/membership:/[task-org:detached]/[seq:<n>]  (ordinal)
        │
        ▼  elements + per-package membership roll-up, all ordinal-sorted
        ▼  MissionPackageProjection.ComputeFingerprint(snapshot)  →  "pkg:empty" | "pkg:active=…#e=…#pk=…"
```

A `null`/empty definition list returns `MissionPackageSnapshot.Empty`. The active package defaults to
the first ordered definition unless `activePackageId` overrides it. A definition with no elements still
emits an (empty) `MissionPackageMembership` roll-up.

---

## What a row carries

`C2NodeElement` is an immutable, presentation-facing record — one composable element, derived only from
authoring + sim / order-log truth:

| Field | Meaning |
|-------|---------|
| `ElementId` | The element id (elements are emitted in `ElementId` ordinal order). |
| `PlatformUnitId` | The platform hosting the element — several elements may share one platform under different scopes. |
| `Role` | `Sensor` / `Shooter` / `Relay` / `C2`. |
| `Availability` | `Available` / `Unavailable` / `LastKnown` — package-capability state (not link paint). |
| `Membership` | `C2NodeMembership(PackageId, PackageLabel, Kind)` — `Organic` vs `Package`, from `CapabilityScope`. |
| `CapabilityScope` | The authored scope string (e.g. `organic-radar`, `package-track-feed`); its `organic-` prefix decides `Kind`. |
| `TaskOrgDetached` | `true` while the platform is detached in the task-org (net detach/rejoin fold). |
| `LastSimTick` / `LastSimTime` | The projected sim clock stamped on the element. |
| `CorrelationSequenceId` | Order-log sequence id of the correlating row — latest damage, else latest detach, else `null`. |
| `SourceRefs` | Ordinal-sorted provenance tokens (`element:` / `unit:` / `package:` / `scope:` / `membership:` / optional `task-org:detached` / `seq:<n>`). |

The per-package `MissionPackageMembership` carries the ordinal-sorted `ElementIds` and `UnitIds` for the
package — a node that goes `Unavailable` is **never dropped** from its package roll-up.

---

## Availability resolution (the ladder)

`ResolveAvailability` runs a first-match-wins ladder per element:

1. **Not alive** (missing from the alive map or `false`) ⇒ **`Unavailable`**.
2. **Killed** (latest damage row `NewHpPct ≤ 0`) ⇒ **`Unavailable`**.
3. **Comms `Denied`** *and* role is `Relay` or `C2` ⇒ **`Unavailable`** (the C2 path is cut).
4. **Comms `Degraded`** *and* role is `Relay` or `C2` ⇒ **`LastKnown`** (stale but retained).
5. **Otherwise** ⇒ **`Available`**.

Comms only gates the C2 path — `Sensor` and `Shooter` elements stay `Available` under degraded/denied
comms (they still exist; the network read-model reports the link). The alive map is one-way: the
`isPlatformAlive` gate seeds it (defaulting to alive) and an order-log `HP ≤ 0` damage row flips it
dead; a later partial-HP row cannot revive an authoritatively dead platform.

### Organic vs package, and task re-org

`CapabilityScope` beginning `organic-` marks the element `Organic`; anything else is `Package`. So a
single platform can appear as **two distinct elements** — its organic radar and a package track feed —
and a task-org detach flags **both** with `TaskOrgDetached` (and a `task-org:detached` source ref)
**without** collapsing them into one node or dropping them from the package (see the fan-out and
detach tests below).

---

## Design invariants — never break these

These are load-bearing and enforced by the tests below.

| Invariant | Rule |
|-----------|------|
| **Membership survives unavailability** | An `Unavailable` node stays listed in its `MissionPackageMembership` roll-up — availability is a state, not a removal. |
| **Organic ≠ package** | Elements are keyed by `ElementId`, so an organic capability and a package feed on the same platform stay distinct through damage, comms, and task re-org. |
| **One-way death** | Once an order-log `HP ≤ 0` (or an authoritative `isPlatformAlive == false`) marks a platform dead, a later partial-HP row does not revive it. |
| **Sim/authoring inputs** | Roles, scopes, availability, membership, and task-org come from authored definitions + order-log damage/comms/detach — **never** UI selection / hover / camera / panel visibility (ADR-010; a reflection test forbids those property names). |
| **Pure / read-only** | `Project` consumes read-only definitions plus an optional read-only `DecisionLog` and returns immutable records. It never enqueues orders, resolves combat, mutates the log (a test pins the order-log fingerprint unchanged), touches `DelegationBridge` / `SimulationSession`, or reads the wall clock. |
| **Deterministic / replay-stable** | Same inputs ⇒ identical rows and fingerprint. Definitions, elements, membership ids, and source refs are all ordinal-sorted; invariant culture; no RNG; no wall clock. |
| **Off the fingerprint** | The `pkg:` fingerprint is a projection artifact for equality / regression, **not** a `SimWorldHash` or order-log-replay input — it never affects the Baltic v2 replay hash `17144800277401907079`. |

---

## The fingerprint

`MissionPackageProjection.ComputeFingerprint` yields `pkg:empty` for a null/empty snapshot, else
`pkg:active=<id>#e=<n>` followed by one `|`-delimited segment per element, then `#pk=<m>` followed by
one segment per package. Each **element** segment packs, in order, `ElementId`, `PlatformUnitId`, the
integer `Role`, the integer `Availability`, the integer membership `Kind`, `Membership.PackageId`,
`CapabilityScope`, `TaskOrgDetached` as `1`/`0`, `LastSimTick`, the `LastSimTime` formatted `"R"` under
invariant culture, `CorrelationSequenceId`, and the `+`-joined `SourceRefs`. Each **package** segment
packs `PackageId`, `PackageLabel`, the `+`-joined `ElementIds`, and the `+`-joined `UnitIds`.

> **Note (accuracy):** like the rest of the Combat-UX read-model family, this joins raw id strings with
> `,` / `|` / `+` and does **not** length-prefix them, so it is not hardened against a separator
> embedded in an id colliding two distinct snapshots. It is a canonical string for equality /
> regression assertions on the sim-clean ids the projection produces — treat it as such, not as a
> collision-resistant hash. If ids ever admit user/free text, length-prefix the segments and update the
> fingerprint tests.

---

## Extending without breaking replay

1. **Adding a role or availability state?** Add the `C2NodeRole` / `C2NodeAvailability` constant, fold
   it in `ResolveAvailability` from a sim / order-log / authoring fact (never UI), keep the ladder
   ordering, and pin the new branch with a projection test.
2. **Adding an availability signal?** Fold it from the order log or an explicit gate (like
   `isPlatformAlive`) — keep the one-way-death rule and keep comms gating scoped to `Relay` / `C2`.
3. **Touching the row or membership shape?** Keep every ordinal sort (definitions, elements, ids,
   source refs), add the new field to `ComputeFingerprint` (a field the fingerprint ignores is a silent
   replay hole), and update the fingerprint test.
4. **Before landing:** run the fixture below plus the full solution suite (`dotnet test`), and confirm
   `ReplayGolden 6/6` and the Baltic v2 hash `17144800277401907079` are unchanged (this projection is
   off the fingerprint, so a correct change here cannot move it — a moved hash means you touched
   something else).

---

## Tests that pin this doc

All green as of writing (DRG-213) — 13 NUnit cases in the core Delegation test assembly (headless
`dotnet test`).

| Test file | Covers |
|-----------|--------|
| [`MissionPackageProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/C2Nodes/MissionPackageProjectionTests.cs) | Empty definitions yielding `Empty` + `pkg:empty`; a composed package listing all four roles with membership + `Available`, and ordinal-sorted `ElementIds` / `UnitIds`; a killed shooter going `Unavailable` while staying in the package roll-up (with its damage `CorrelationSequenceId`); an authoritatively dead platform staying `Unavailable` despite a partial-HP row; `isPlatformAlive == false` marking every element `Unavailable`; comms `Denied` marking `Relay` + `C2` `Unavailable` (sensor/shooter untouched); comms `Degraded` marking `Relay` + `C2` `LastKnown`; an organic sensor and a package feed on the same platform staying two distinct elements; a task-org detach flagging both with `TaskOrgDetached` + the `task-org:detached` source ref; unordered definitions sorting to a stable fingerprint; two identical builds fingerprinting identically; `Project` leaving the order-log fingerprint unchanged; and the ADR-010 reflection guard that the DTOs expose no `selection` / `hover` / `camera` / `visible` / `visibility` / `selected` property. |

Run just this fixture:

```bash
dotnet test src/ProjectAegis.Delegation.Tests/ProjectAegis.Delegation.Tests.csproj \
  --filter "FullyQualifiedName~MissionPackage"
```

---

*Verified against source at the paths above. If you change the roles, the availability ladder, the
organic-vs-package rule, the task-org fold, or the fingerprint layout, update this doc together with the
[comms-degradation-runtime.md](comms-degradation-runtime.md) and
[direct-control-override-runtime.md](direct-control-override-runtime.md) producers it folds.*
