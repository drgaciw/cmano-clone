# CEC mesh & composite tracking — cooperative engagement (SWARM-31 / B6a + B6b)

The `Cec/` folder in [`ProjectAegis.Sim`](../../src/ProjectAegis.Sim/Cec/) is the deterministic
**Cooperative Engagement Capability** runtime: same-side sensor nodes form a proximity **mesh**,
pool their organic detections into fused **composite tracks**, and — when a shooter has no organic
fire-control solution — a separate gate lets it **engage on remote mesh data**. It is the sensor-
fusion counterpart to the aggregate-integrity [swarm-runtime.md](swarm-runtime.md), which
deliberately scopes CEC *out* (`SwarmLinkState` is the C2 channel; CEC is a different mesh).

This guide covers the mesh membership rules, the composite-track fusion contract, the remote-engage
gate and where it sits in the engagement kill-chain, the hard **CEC-mesh-is-not-the-C2-link**
invariant, and how to extend it without breaking determinism.

> **Scope.** SWARM-31 mesh half (B6a, DRG-102) = `Cec/`; remote engage-on-remote-data (B6b,
> DRG-103) = [`CecRemoteEngageGate`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) in
> `Engage/`. Everything here is **pure Sim** — no Unity, no `DelegationBridge`, no RNG, no
> wall-clock. Geometry uses the same degrees-lat/lon placeholder kinematics band as the other Sim
> evaluators. The CEC mesh **never references** `SwarmLinkState` or any Swarm C2 type.

Related: [swarm-runtime.md](swarm-runtime.md) (aggregate integrity + C2 link; CEC scoped out) ·
[engagement-pipeline.md](engagement-pipeline.md) (the gate chain the remote-engage gate joins) ·
[detection-pipeline.md](detection-pipeline.md) (organic contacts that feed contributions) ·
[abort-reason-catalog.md](abort-reason-catalog.md) (`ENGAGE_ABORT` codes) ·
[determinism-and-replay.md](determinism-and-replay.md) ·
[Sim README](../../src/ProjectAegis.Sim/README.md).

---

## The core invariant: CEC mesh ≠ C2 link

The mesh models **sensor cooperation**, not command-and-control. `CecMeshState` is computed purely
from same-side peer proximity + environment, and is **independent** of the
[`SwarmLinkState`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkState.cs) host/order channel. A unit
can be `Lost` on C2 yet `InMesh` for CEC, or vice-versa. Jamming drops the mesh **without** implying
C2 loss. Every CEC type's doc-comment restates this — do not couple the two.

---

## Node & mesh-state model

A participant is described by an immutable
[`CecNodeRegistration`](../../src/ProjectAegis.Sim/Cec/CecNodeRegistration.cs)
`(UnitId, SideId, CecCapable, LatDeg, LonDeg, IsAlive = true, IsSwarm = false)`.

[`CecMeshState`](../../src/ProjectAegis.Sim/Cec/CecMeshState.cs) has three members:

| State | Ordinal | Meaning |
|-------|:------:|---------|
| `InMesh` | 0 | ≥1 same-side CEC peer within **connected** range |
| `Degraded` | 1 | nearest peer only within the **degraded** band (beyond connected, within degraded) |
| `OutOfMesh` | 2 | non-CEC, dead, jammed, out of range, or no peer |

[`CecMeshEvaluator`](../../src/ProjectAegis.Sim/Cec/CecMeshEvaluator.cs) is the pure rule
(`static`). `EvaluateMeshState` short-circuits to `OutOfMesh` when a node is `!cecCapable`,
`!alive`, `jammed`, or peerless; otherwise it bands the best peer range:

- `range ≤ connectedRangeDeg` → `InMesh`
- `range ≤ degradedRangeDeg` → `Degraded`
- else → `OutOfMesh`

Defaults are `DefaultConnectedRangeDeg = 2.0` and `DefaultDegradedRangeDeg = 4.0` degrees.
`RangeDeg` is a Euclidean lat/lon placeholder (`√(Δlat² + Δlon²)`), matching the other Sim
kinematics bands.

---

## `CecMeshController` — membership, events & the composite picture

[`CecMeshController`](../../src/ProjectAegis.Sim/Cec/CecMeshController.cs) is the stateful (but
deterministic) aggregate. Its constructor clamps the range bands to sane values
(`connected > 0`, `degraded > connected`, else defaults).

### Lifecycle

| Method | Behaviour |
|--------|-----------|
| `Register(registration)` | Add/replace a node (validates non-empty `UnitId` / `SideId`); new nodes start `OutOfMesh`. |
| `UpdateNode(unitId, lat, lon, isAlive)` | Move / kill an existing node (throws `KeyNotFoundException` if unregistered). |
| `Refresh(jammed = false)` | Recompute **all** mesh states pairwise among same-side CEC peers. Iteration is **unit-id ordinal sorted** for determinism; `jammed` drops the mesh without touching C2. |
| `GetMeshState(unitId)` | Current state (`OutOfMesh` for unknown/blank ids). |

`FindBestSameSideCecPeer` only considers **alive, CEC-capable, same-side** peers, and a peer counts
as "in range" only within the **degraded** (outer) envelope. On any state change, `Refresh` appends
a [`CecMeshEvent`](../../src/ProjectAegis.Sim/Cec/CecMeshEvent.cs)
`(SequenceId, UnitId, Kind, PreviousState, NewState)` to an append-only log. The
[`CecMeshEventKind`](../../src/ProjectAegis.Sim/Cec/CecMeshEventKind.cs) is derived from the
transition: entering `OutOfMesh` → `Leave`, entering `InMesh` → `Join`, entering `Degraded` →
`Degrade`. `ComputeEventLogFingerprint()` renders the log as
`"{seq}:{unit}:{kind}:{prev}->{next}"` joined by `|` (`"empty"` when no events) for determinism
checks.

### Composite tracks

Nodes push detections in with `ContributeOrganic(sideId, contributorUnitId, targetId,
sensorQuality)`. A contribution is **accepted only when** the contributor is `CecCapable`, currently
`InMesh`, and on the named side; `sensorQuality` is clamped to `[0,1]` and stored keyed by
`side|unit|target` (so re-contributing overwrites).

`TryGetCompositeTracks(sideId)` fuses them into
[`CecCompositeTrack`](../../src/ProjectAegis.Sim/Cec/CecCompositeTrack.cs) records:

- Only contributions from nodes currently `InMesh` **or** `Degraded` are considered (a node that
  dropped to `OutOfMesh` since contributing is excluded at fusion time).
- A target needs **≥2 mesh-connected contributors** to form a track (fewer → skipped).
- `Quality` = clamped **average** contributor `sensorQuality`.
- `PrimaryContributorUnitId` = highest-quality contributor, ties broken by **ordinal** unit id.
- `FireControlQuality` is **true only when** at least one contributor is currently `InMesh` **and**
  has `sensorQuality ≥ 0.6` — a degraded-only mesh yields FC `false`.
- Output is deterministic: targets sorted ordinal, contributors sorted ordinal, `TrackId` =
  `cec-{side}-{target}`.

---

## Remote engage-on-remote-data (B6b)

[`CecRemoteEngageGate`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) (pure `static`,
in `Engage/`) lets a CEC-capable shooter fire on a **remote** composite track when it has no
**organic** fire-control solution. Organic FC always remains the preferred path; remote never
invents per-drone fire-control bodies (the swarm aggregate SoT is unchanged).

The [`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) consults
it **after** the spoof/EMCON gates and **before** the magazine/envelope gates, reading four
[`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) fields — `HasFireControlTrack`,
`UsesRemoteCecTrack`, `ShooterCecCapable`, `CecRemoteFireControlEligible` (all default `false`):

1. `Evaluate(...)` returns an abort reason when a remote shot was required/requested but the shooter
   is not CEC-capable or has no eligible remote track → `EngagementAbortReason.CecRemoteTrackUnavailable`
   (code `27`). Organic-present-and-not-remote returns `null` (gate N/A).
2. `HasUsableFireControl(...)` then decides whether FC exists at all. If not, the resolver aborts
   with `CecRemoteTrackUnavailable` when the shot was tagged remote, otherwise the classic
   `NoFireControlTrack` (code `4`). The **distinct** codes let callers tell an organic-only failure
   from a mesh drop.

For headless fixtures / world adapters, `TryResolveRemoteEligibility(mesh, side, shooter, target,
out track)` derives eligibility straight from a live `CecMeshController`: the shooter must be
`InMesh`, and there must be an **FC-quality** composite track on the target whose
`PrimaryContributorUnitId` is **not** the shooter (self-primary is organic, not "remote").

---

## Determinism & replay safety

- **Pure evaluators.** `CecMeshEvaluator` and `CecRemoteEngageGate` hold no state, read no clock,
  draw no RNG.
- **Ordered everywhere.** `Refresh`, `TryGetCompositeTracks`, and peer search all iterate over
  **ordinal-sorted** ids; tie-breaks are ordinal. `ComputeEventLogFingerprint` gives a stable
  determinism probe.
- **Isolated from the Baltic v2 golden.** CEC lives on the sensor/engage side and is exercised by
  its own fixtures; it does not touch the Baltic v2 replay hash `17144800277401907079` (it is a
  distinct concern from the swarm golden noted in [swarm-runtime.md](swarm-runtime.md)).
- **Aggregate SoT preserved.** Remote engage adds a fire-control *permission* only; it never mints
  new track bodies or per-drone state.

---

## Runbook — extend the mesh or remote gate

1. **New mesh-state rule** → change `CecMeshEvaluator.EvaluateMeshState` (keep it pure and
   band-monotonic) and add a `CecMeshControllerTests` case. Do not read the C2 `SwarmLinkState`.
2. **New composite-track field** → extend the `CecCompositeTrack` record and populate it in
   `TryGetCompositeTracks`; keep ordinal sorting and the ≥2-contributor / FC-quality thresholds
   deterministic.
3. **New remote-engage condition** → adjust `CecRemoteEngageGate.Evaluate` /
   `HasUsableFireControl` and wire the field through `EngageContext`; if a new abort is needed, add
   it to `EngagementAbortReason` per the [abort-reason-catalog.md](abort-reason-catalog.md) codegen
   workflow (append-only codes) and cover it in `CecRemoteEngageTests`.
4. **Never** introduce a `DelegationBridge`, Unity, RNG, or wall-clock dependency into `Cec/`.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`CecMeshControllerTests.cs`](../../src/ProjectAegis.Sim.Tests/Cec/CecMeshControllerTests.cs) | Register/refresh membership banding (`InMesh` / `Degraded` / `OutOfMesh`), jam + death drops, same-side/CEC-capability filtering, organic-contribution gating, ≥2-contributor composite fusion, FC-quality threshold, primary/tie-break ordering, and event-log fingerprint determinism. |
| [`CecRemoteEngageTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/CecRemoteEngageTests.cs) | The remote-engage gate: organic-preferred passthrough, `CecRemoteTrackUnavailable` vs. `NoFireControlTrack` distinction, non-CEC shooter denial, and `TryResolveRemoteEligibility` self-primary exclusion. |
