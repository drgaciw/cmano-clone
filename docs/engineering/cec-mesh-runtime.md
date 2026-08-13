# CEC mesh & remote engage runtime — developer guide

**CEC** (Cooperative Engagement Capability) lets same-side units share a *fused* air/surface picture
and, crucially, lets a CEC-capable shooter fire on a target it cannot see organically — using a
composite track built from its mesh peers' sensors. This runtime models three things, all pure and
engine-agnostic in [`ProjectAegis.Sim/`](../../src/ProjectAegis.Sim/):

1. **Mesh membership health** — is a node `InMesh`, `Degraded`, or `OutOfMesh` with its same-side CEC
   peers (SWARM-31 / B6a).
2. **Composite track fusion** — merge ≥2 mesh-connected contributors on the same target into one
   fused track, with a fire-control-quality flag.
3. **Engage-on-remote-data** — a track gate (B6b) that lets the engagement pipeline treat a remote
   CEC composite as satisfying fire control when the shooter has no organic track.

> **CEC mesh is *not* the C2 link.** The mesh (`CecMeshState`) is deliberately independent of the
> swarm C2 / host-order channel (`SwarmLinkState`) — a unit can have solid command connectivity but be
> `OutOfMesh`, or hold a datalink mesh while its C2 is degraded. These are orthogonal axes and the CEC
> types **never** reference swarm C2 types. Don't collapse them into one "link" concept.

- **Source:** [`src/ProjectAegis.Sim/Cec/`](../../src/ProjectAegis.Sim/Cec/) (mesh state, controller,
  evaluator, composite track, event log) and the engage-side gate
  [`src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs).
- **Related:** the engage/kill-chain resolver that consumes the remote-engage gate is the
  [engagement pipeline](engagement-pipeline.md); the organic fire-control track it falls back from
  comes from the [detection pipeline](detection-pipeline.md); abort codes are in the
  [abort-reason catalog](abort-reason-catalog.md); determinism rules are in
  [determinism-and-replay.md](determinism-and-replay.md).

> **Pure Sim, deterministic, no bridge.** Everything here is single-threaded, sorts all iteration by
> ordinal id, uses no RNG or wall-clock, and never touches Unity or `DelegationBridge`. The mesh keeps
> an append-only event log with a stable fingerprint so a mesh change is diagnosable independently of
> the rest of the sim.

---

## Where it lives

| File | Role |
|------|------|
| [`CecMeshState.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshState.cs) | The membership enum: `InMesh(0)` / `Degraded(1)` / `OutOfMesh(2)`. |
| [`CecNodeRegistration.cs`](../../src/ProjectAegis.Sim/Cec/CecNodeRegistration.cs) | Registration payload: `(UnitId, SideId, CecCapable, LatDeg, LonDeg, IsAlive, IsSwarm)`. |
| [`CecMeshEvaluator.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshEvaluator.cs) | Pure membership rule `EvaluateMeshState(...)` + the degree-band ranges and `RangeDeg`. |
| [`CecMeshController.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshController.cs) | Stateful mesh: register/update, `Refresh`, organic contributions, composite tracks, event log. |
| [`CecCompositeTrack.cs`](../../src/ProjectAegis.Sim/Cec/CecCompositeTrack.cs) | A fused track `(TrackId, TargetId, SideId, PrimaryContributorUnitId, ContributorCount, FireControlQuality, Quality)`. |
| [`CecMeshEvent.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshEvent.cs) / [`CecMeshEventKind.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshEventKind.cs) | Append-only `(SequenceId, UnitId, Kind, PreviousState, NewState)` transition rows; `Join(0)` / `Degrade(1)` / `Leave(2)`. |
| [`CecRemoteEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) | Pure engage-on-remote-data gate + mesh-driven eligibility resolution. |

---

## Mesh membership (`CecMeshEvaluator` + `CecMeshController`)

Membership is a pure function of geometry + flags. `CecMeshEvaluator.EvaluateMeshState` decides one
node's state from its **best same-side CEC peer range** (degrees lat/lon) and environmental flags:

| Condition | Result |
|-----------|--------|
| Not `CecCapable`, not alive, or `jammed` | `OutOfMesh` |
| No same-side CEC peer in range | `OutOfMesh` |
| Best peer range ≤ `connectedRangeDeg` (default **2.0°**) | `InMesh` |
| Best peer range ≤ `degradedRangeDeg` (default **4.0°**) | `Degraded` |
| Beyond the degraded band | `OutOfMesh` |

Range is the Euclidean lat/lon placeholder `√(Δlat² + Δlon²)` (`RangeDeg`) — the same simple
kinematics band other Sim evaluators use. **Jam drops the mesh without implying C2 loss** — the
`jammed` flag forces `OutOfMesh` but says nothing about the order channel, reinforcing the mesh ≠ C2
separation.

`CecMeshController` holds the live nodes and states:

- **`Register(registration)`** / **`UpdateNode(unitId, lat, lon, isAlive)`** — add/replace a node
  (geometry + capability) or update its geometry/liveness. New nodes start `OutOfMesh`.
- **`Refresh(jammed = false)`** — recompute every node's state. It sorts unit ids **ordinal** first, so
  iteration (and therefore the event log) is deterministic. For each node it finds the nearest alive,
  same-side, CEC-capable peer within the degraded band and calls the evaluator. A peer only counts as
  "in range" when within `degradedRangeDeg` (the outer envelope).
- **`GetMeshState(unitId)`** — current state (`OutOfMesh` for unknown/blank ids, never throws).

Every state change appends a [`CecMeshEvent`](../../src/ProjectAegis.Sim/Cec/CecMeshEvent.cs) with a
monotonic `SequenceId`. The transition→kind mapping: entering `InMesh` → `Join`, entering `Degraded`
→ `Degrade`, leaving to `OutOfMesh` → `Leave`. `ComputeEventLogFingerprint()` renders the log as
`"{seq}:{unit}:{kind}:{prev}->{next}| …"` (or `"empty"`) for determinism assertions.

---

## Composite track fusion (`TryGetCompositeTracks`)

Nodes feed the fused picture with **organic contributions**:

- **`ContributeOrganic(sideId, contributorUnitId, targetId, sensorQuality)`** accepts a detection
  **only** when the contributor is `CecCapable`, currently `InMesh`, and on the named side. Quality is
  clamped to `[0, 1]`. Contributions are keyed `side|unit|target`, so a re-contribution replaces the
  prior one (idempotent per contributor/target).

- **`TryGetCompositeTracks(sideId)`** fuses them into `CecCompositeTrack` rows, one per target, with a
  deliberate quality contract:
  - Only contributors currently `InMesh` **or** `Degraded` are considered (out-of-mesh drops out).
  - A track requires **≥2 mesh-connected contributors** on that target — a single sensor is not a
    composite.
  - `Quality` is the clamped **average** contributor quality; `PrimaryContributorUnitId` is the
    highest-quality contributor (ties broken ordinal-lowest).
  - **`FireControlQuality`** is `true` only when at least one contributor is currently `InMesh` **and**
    has quality **≥ 0.6** — a purely `Degraded` mesh yields a track you can *see* but not *shoot* on.
  - Targets and contributors are ordinal-sorted, so output is replay-stable (`TrackId = "cec-{side}-{target}"`).

---

## Engage-on-remote-data (`CecRemoteEngageGate`, B6b)

The gate that lets the [engagement pipeline](engagement-pipeline.md) fire on a peer's picture. Organic
fire control is always preferred; remote is the fallback when the shooter has **no** organic track.
The [`MvpEngagementResolver`](../../src/ProjectAegis.Sim/Engage/MvpEngagementResolver.cs) calls it at
the track gate (after spoof/EMCON, before magazine/envelope), reading three
[`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) flags — `UsesRemoteCecTrack`,
`CecRemoteFireControlEligible`, `ShooterCecCapable`:

- **`Evaluate(hasOrganicFc, usesRemote, shooterCecCapable, remoteEligible)`** → an
  `EngagementAbortReason?`. Organic FC present (and not tagged remote) ⇒ `null` (proceed). When the
  shot is remote (or organic is missing and remote is claimed), a **non-CEC-capable shooter** or an
  **ineligible** remote track aborts with
  [`CecRemoteTrackUnavailable`](../../src/ProjectAegis.Sim/Engage/EngagementAbortReason.cs) (code 27).
- **`HasUsableFireControl(...)`** → the boolean the resolver uses to decide track availability: organic
  FC ⇒ `true`; else a CEC-capable shooter with remote eligibility ⇒ `true`; otherwise `false`. On
  `false` the resolver aborts `CecRemoteTrackUnavailable` when the shot was tagged remote, else the
  classic `NoFireControlTrack`.
- **`TryResolveRemoteEligibility(mesh, sideId, shooterUnitId, targetId, out track)`** — the headless
  adapter that computes eligibility from a live `CecMeshController`: the shooter must be `InMesh`, and
  there must be an **FC-quality** composite track for the target whose **primary contributor is not the
  shooter itself** (self-primary isn't "remote" data). This is what a world adapter uses to set
  `CecRemoteFireControlEligible` before the tick.

> **Aggregate swarm SoT is unchanged.** Remote engage does **not** invent per-drone fire-control
> bodies — a swarm's magazine/fire-control accounting stays aggregate; CEC only changes *whether a
> track exists*, not *who owns the rounds*.

---

## Determinism & safety notes

- **Pure Sim** — no RNG, no wall-clock, no Unity/`DelegationBridge`. Ordinal iteration everywhere
  (`Refresh`, contributions, composite fusion) makes the mesh event log and composite tracks
  replay-stable.
- **Append-only event log** with a stable `ComputeEventLogFingerprint()` — a divergence in mesh
  membership is diagnosable on its own.
- **Mesh ≠ C2** is an invariant, not a coincidence — the `Cec/` types must never reference
  `SwarmLinkState` or other swarm C2 types.
- **Fire-control quality gate is load-bearing** — degraded-only meshes surface tracks but must not set
  `FireControlQuality`; loosening the `InMesh` + `≥0.6` rule changes what can be shot at.
- **Remote engage is a fallback, organic FC wins** — keep the `Evaluate`/`HasUsableFireControl` order
  so a shooter with its own track never depends on the mesh.

---

## Common pitfalls

- **Coupling mesh to C2.** Don't feed `SwarmLinkState` into mesh evaluation or vice-versa; they are
  independent axes (a unit can be `InMesh` with degraded C2).
- **Expecting a single-sensor composite.** `TryGetCompositeTracks` needs **≥2** mesh-connected
  contributors; one contributor yields no track.
- **Firing off a degraded-only track.** A `Degraded`-only composite has `FireControlQuality = false` —
  it is a display track, not a shootable one.
- **Self-remote engage.** `TryResolveRemoteEligibility` rejects a composite whose primary contributor
  is the shooter — that's organic, not remote data.
- **Skipping `Refresh`.** Mesh state only updates when `Refresh` runs; geometry updates via
  `UpdateNode` don't re-evaluate membership until the next `Refresh`.

---

## Tests

| Test file | Covers |
|-----------|--------|
| [`CecMeshControllerTests`](../../src/ProjectAegis.Sim.Tests/Cec/CecMeshControllerTests.cs) | Membership bands, jam/dead/non-capable → `OutOfMesh`, event log + fingerprint, organic acceptance rules, composite ≥2-contributor + FC-quality thresholds. |
| [`CecRemoteEngageTests`](../../src/ProjectAegis.Sim.Tests/Engage/CecRemoteEngageTests.cs) | `Evaluate` / `HasUsableFireControl` truth table, `CecRemoteTrackUnavailable` aborts, mesh-driven `TryResolveRemoteEligibility`. |

Run the sim suite after any change here:

```bash
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj -v minimal
```

---

## See also

| Topic | Doc |
|-------|-----|
| The engage/kill-chain resolver that consumes the remote-engage gate | [engagement-pipeline.md](engagement-pipeline.md) |
| The organic fire-control track CEC falls back from | [detection-pipeline.md](detection-pipeline.md) |
| The `CecRemoteTrackUnavailable` abort code + `ENGAGE_ABORT` catalog | [abort-reason-catalog.md](abort-reason-catalog.md) |
| Determinism rules, hashing, golden workflow | [determinism-and-replay.md](determinism-and-replay.md) |
| The wider `ProjectAegis.Sim` simulation core | [`src/ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md) |
