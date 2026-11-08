# CEC mesh & remote engage — cooperative composite tracking (SWARM-31 / B6a–B6b)

**Cooperative Engagement Capability (CEC)** lets same-side, CEC-capable units fuse their organic
sensor detections into a shared **composite track picture**, and lets a shooter fire on a
mesh-provided track when it has no organic fire-control of its own. It is a pure, engine-agnostic
`ProjectAegis.Sim` surface split into two deterministic halves:

| Half | Story | Location |
|------|-------|----------|
| **Mesh + composite track (B6a)** | SWARM-31 / DRG-102 | [`src/ProjectAegis.Sim/Cec/`](../../src/ProjectAegis.Sim/Cec/) |
| **Remote engage-on-remote-data (B6b)** | SWARM-31 / DRG-103 | [`CecRemoteEngageGate`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) (in the engage folder) |

> **Boundary / invariant (SWARM-31):** the CEC **mesh is independent of the C2 host/order
> channel** — none of these types reference `SwarmLinkState` or any other Swarm-C2 type, so a unit
> can be *in mesh* while its C2 link is degraded, and vice-versa. Everything is **deterministic**
> (ordinal iteration, an append-only sequence-numbered event log with a stable fingerprint) and
> lives entirely in `ProjectAegis.Sim` — **no Unity, no `DelegationBridge`**. Remote engage never
> invents per-drone fire-control bodies; the **aggregate swarm source-of-truth is unchanged**. This
> is the CEC companion to the swarm runtime, which deliberately scopes CEC out.

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`CecNodeRegistration`](../../src/ProjectAegis.Sim/Cec/CecNodeRegistration.cs) | `sealed record` | Registration payload: `(UnitId, SideId, CecCapable, LatDeg, LonDeg, IsAlive=true, IsSwarm=false)`. Geometry is degrees lat/lon (the same placeholder kinematics band other Sim evaluators use). |
| [`CecMeshState`](../../src/ProjectAegis.Sim/Cec/CecMeshState.cs) | `enum` | `InMesh=0` / `Degraded=1` / `OutOfMesh=2`. |
| [`CecMeshEventKind`](../../src/ProjectAegis.Sim/Cec/CecMeshEventKind.cs) | `enum` | `Join=0` / `Degrade=1` / `Leave=2`. |
| [`CecMeshEvent`](../../src/ProjectAegis.Sim/Cec/CecMeshEvent.cs) | `sealed record` | Append-only log row `(SequenceId, UnitId, Kind, PreviousState, NewState)`. |
| [`CecCompositeTrack`](../../src/ProjectAegis.Sim/Cec/CecCompositeTrack.cs) | `sealed record` | A fused track `(TrackId, TargetId, SideId, PrimaryContributorUnitId, ContributorCount, FireControlQuality, Quality)`. |
| [`CecMeshEvaluator`](../../src/ProjectAegis.Sim/Cec/CecMeshEvaluator.cs) | `static` | Pure membership rules + range math. |
| [`CecMeshController`](../../src/ProjectAegis.Sim/Cec/CecMeshController.cs) | `sealed class` | Stateful node registry, mesh refresh, organic contributions, composite-track fusion, event log. |

---

## Mesh membership rules — `CecMeshEvaluator`

`EvaluateMeshState(cecCapable, hasPeerInRange, bestPeerRangeDeg, jammed, alive, connectedRangeDeg,
degradedRangeDeg)` is a pure function of one node's situation:

- **`OutOfMesh`** when the node is not `CecCapable`, not `alive`, `jammed`, or has no same-side CEC
  peer in range. (Jam drops the mesh *without* implying C2 loss.)
- **`InMesh`** when the best peer range `≤ connectedRangeDeg` (**default `2.0°`**).
- **`Degraded`** when `connectedRangeDeg < range ≤ degradedRangeDeg` (**default `4.0°`**).
- **`OutOfMesh`** again beyond the degraded band.

`RangeDeg(latA, lonA, latB, lonB)` is a Euclidean lat/lon placeholder (`√(Δlat² + Δlon²)`),
matching the other Sim kinematics bands. The controller constructor clamps its two thresholds:
`connectedRangeDeg` must be `> 0` (else default), and `degradedRangeDeg` must be `> connected`
(else default).

---

## The controller — `CecMeshController`

```
Register(CecNodeRegistration)   // add/replace a node (geometry + capability); UnitId & SideId required
UpdateNode(unitId, lat, lon, isAlive=true)   // move / kill an existing node
Refresh(jammed=false)           // recompute every node's mesh state, ordinal-ordered, logging transitions
GetMeshState(unitId) → CecMeshState
ContributeOrganic(side, contributor, target, sensorQuality) → bool
TryGetCompositeTracks(side) → IReadOnlyList<CecCompositeTrack>
MeshEventLog / ComputeEventLogFingerprint()
```

**`Refresh`** sorts unit ids with `StringComparer.Ordinal`, and for each node finds the *closest*
same-side, alive, CEC-capable peer; a peer only "counts" when within the outer (degraded) envelope.
The resulting `CecMeshState` is stored, and any change from the previous state appends a
`CecMeshEvent` (`OutOfMesh→Degraded` = `Degrade`, any→`OutOfMesh` = `Leave`, →`InMesh` = `Join`).
Ordinal iteration makes the event log order — and its fingerprint — reproducible regardless of
registration order.

**`ContributeOrganic`** records a detection into the composite picture only when the contributor is
`CecCapable`, currently **`InMesh`**, and on the named side; `sensorQuality` is clamped to `[0, 1]`.
(A degraded-only node is refused new contributions.)

**`TryGetCompositeTracks(side)`** fuses per target:

- includes contributions from `InMesh` **or** `Degraded` contributors;
- requires **≥ 2 contributors** on the same target (fewer ⇒ no track);
- `Quality` = mean contributor `sensorQuality` (clamped);
- `PrimaryContributorUnitId` = highest-quality contributor (ties broken by ordinal-lowest id);
- **`FireControlQuality`** is `true` only when at least one contributor is currently **`InMesh`**
  with quality `≥ 0.6` — a degraded-only mesh yields `FireControlQuality = false`;
- `TrackId` = `"cec-{side}-{targetId}"`.

**Event-log fingerprint** — `ComputeEventLogFingerprint()` joins `"{seq}:{unit}:{kind}:{prev}->{new}"`
rows with `|` (`"empty"` when the log is empty), giving determinism tests a single string to diff.

---

## Remote engage-on-remote-data (B6b) — `CecRemoteEngageGate`

At the tick-8 engagement resolve (see [engagement-pipeline.md](engagement-pipeline.md)),
[`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) consults the
gate **after** the EMCON/spoof checks and **before** the magazine/envelope gates. The relevant
[`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) inputs are
`HasFireControlTrack`, `UsesRemoteCecTrack`, `CecRemoteFireControlEligible`, and `ShooterCecCapable`.

- **`Evaluate(...)`** returns an abort reason when a remote shot was tagged (or organic FC is absent
  and remote eligibility was claimed) but the shooter is not CEC-capable or not actually eligible —
  [`EngagementAbortReason.CecRemoteTrackUnavailable`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs)
  (`= 27`). Organic FC short-circuits to a no-op **only when `!usesRemoteCecTrack`**. An explicitly
  remote-tagged shot (`usesRemoteCecTrack == true`) still runs the CEC-capable / eligibility checks
  even if organic FC is present.
- **`HasUsableFireControl(...)`** is the positive form the resolver uses to decide fire-control is
  satisfied: organic track ⇒ `true`; otherwise remote counts only when the shooter is CEC-capable
  **and** `CecRemoteFireControlEligible`. If neither holds the resolver aborts with
  `CecRemoteTrackUnavailable` (remote-tagged shot) or the classic `NoFireControlTrack`.
- **`TryResolveRemoteEligibility(mesh, side, shooter, target, out track)`** builds that eligibility
  from a live `CecMeshController` for headless fixtures / world adapters: the shooter must be
  **`InMesh`**, and there must be an **FC-quality** composite track for the target **whose primary
  contributor is not the shooter itself** (self-primary is organic, not "remote").

> **Doctrine:** organic fire-control remains the preferred path; remote CEC is used only when
> organic is absent and a mesh-quality composite track is available to a CEC-capable shooter. Mesh
> loss aborts the remote shot with the explicit `CecRemoteTrackUnavailable` reason rather than
> silently succeeding.

---

## Determinism & tests

- **Pure & deterministic.** No RNG, no wall-clock, ordinal-sorted iteration, and an append-only
  sequence-numbered event log. The mesh does not touch the order-log fingerprint directly; remote
  engage folds into the existing engagement resolve (and its abort codes) like any other gate.
- **Independence.** The mesh types never import `ProjectAegis.Sim.Swarm`; the controller tests
  assert this explicitly.

| Suite | Location | Count |
|-------|----------|-------|
| `CecMeshControllerTests` | [`src/ProjectAegis.Sim.Tests/Cec/CecMeshControllerTests.cs`](../../src/ProjectAegis.Sim.Tests/Cec/CecMeshControllerTests.cs) | 9 |
| `CecRemoteEngageTests` | [`src/ProjectAegis.Sim.Tests/Engage/CecRemoteEngageTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/CecRemoteEngageTests.cs) | 8 |

Related: [engagement-pipeline.md](engagement-pipeline.md) (the tick-8 resolver the remote gate feeds) ·
[abort-reason-catalog.md](abort-reason-catalog.md) (`CecRemoteTrackUnavailable` and the abort-code manifest) ·
[detection-pipeline.md](detection-pipeline.md) (the organic contact/fire-control picture CEC fuses).
