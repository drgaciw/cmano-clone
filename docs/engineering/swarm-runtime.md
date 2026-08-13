# Swarm & CEC runtime — developer guide

Project Aegis models drone/UAS **swarms** as **first-class aggregate units**, not clouds of
individually simulated bodies. A swarm has one selection id, one centroid, and an integer
**living drone count** (`0..maxDrones`); combat, motion, detection, and presentation all treat
that aggregate as the source of truth. This keeps engagement work `O(swarm units)` — never
`O(drones)` — while still expressing swarm-specific behaviour (formations, multi-axis assault,
EMP/jam soft-kill, host regen, kamikaze expend, and CEC composite tracks).

The runtime was built across the **SWARM-A → SWARM-C** program (Phase A aggregate core → Phase B
modes/host/link/CEC → Phase C formations/assault/expend/soft-kill) plus the S117 swarm pressure
suite. This page documents what the runtime actually **does**, its public seams, and how to extend
it without breaking replay goldens. It is verified against source and pinned by the tests listed at
the end.

- **Sim core:** [`src/ProjectAegis.Sim/Swarm/`](../../src/ProjectAegis.Sim/Swarm/) (controller,
  enums, evaluators, formations, assault, expend, soft-kill, replay harness) and
  [`src/ProjectAegis.Sim/Cec/`](../../src/ProjectAegis.Sim/Cec/) (cooperative-engagement mesh).
- **Engagement integration:** the swarm hooks under
  [`src/ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) (hard-counter AA, integrity
  applier, CEC remote-engage gate, salvo deconfliction).
- **Data / catalog:** swarm platform rows + spawn factory under
  [`src/ProjectAegis.Data/Catalog/`](../../src/ProjectAegis.Data/Catalog/) and scenario tasking under
  [`src/ProjectAegis.Data/Scenario/Authoring/`](../../src/ProjectAegis.Data/Scenario/Authoring/).
- **Delegation / presentation:** agent-issued intents under
  [`src/ProjectAegis.Delegation/Sim/`](../../src/ProjectAegis.Delegation/Sim/) and read-only C2
  projections under [`src/ProjectAegis.Delegation/Projection/`](../../src/ProjectAegis.Delegation/Projection/).
- **Related:** the surrounding kill chain is documented in
  [engagement-pipeline.md](engagement-pipeline.md); the WRA/auto-engage/expend authorization gate is
  in [autonomy-roe-gating.md](autonomy-roe-gating.md); contact classification is in
  [detection-pipeline.md](detection-pipeline.md); the C2 read-model layer that hosts the swarm panels
  is [c2-projection-layer.md](c2-projection-layer.md). Replay determinism rules are in
  [determinism-and-replay.md](determinism-and-replay.md).

---

## Design invariants — never break these

These are load-bearing and enforced by tests. Preserve them when extending the runtime.

| Invariant | Rule |
|-----------|------|
| **Aggregate source of truth** | Combat/detection/motion iterate **swarm units**, never logical drones. `maxDrones` is a logical integrity ceiling, not a spawn count for per-body simulation. See [`SwarmPerformanceCaps.EngagementWorkUnitsPerPulse`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs). |
| **Authorized-only integrity** | Drone count is only mutated through [`SwarmController.TryApplyIntegrityDamage`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) / `TryApplyIntegrityRegen` (and the expend path that routes through them). No public field write, no back-door mutation. |
| **C2 link ≠ CEC mesh** | `SwarmLinkState` (host/order channel) and `CecMeshState` (sensor mesh) are **independent**. The CEC types never reference `SwarmLinkState`, and jam drops the mesh **without** implying C2 is lost. |
| **Determinism** | Everything here is pure or seed-deterministic: no `Random.Shared`, no `DateTime.UtcNow`, ordinal-sorted iteration, seeded permutations (`SplitMix64`). The swarm replay fingerprint must stay reproducible per `(seed, orders, integrity)`. |
| **Replay isolation** | The swarm golden (`SwarmReplayHarness`) is **isolated** from the Baltic v2 order-log golden 6/6 and does not mutate the Baltic replay hash `17144800277401907079`. |
| **Presentation is a client** | Projections under `Delegation/Projection/` are read-only view models over aggregate state — they never mutate the controller. |

---

## Where it lives

### Data / catalog (spawn side)

| File | Role |
|------|------|
| [`CatalogSwarmPlatform.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSwarmPlatform.cs) | Catalog row for a swarm platform: `MaxDrones`, `IsSwarm`, `DefaultMode`, `RequiresHost`/`AllowedHostClasses`, `CecCapable`. `CatalogSwarmPlatformDefaults` holds the generic (`uas-swarm-generic`, `maxDrones=40`) and USN-CEC (`usn-uas-swarm-cec`) presets + valid mode names. |
| [`SwarmUnitIntegrity.cs`](../../src/ProjectAegis.Data/Catalog/SwarmUnitIntegrity.cs) | Aggregate integrity DTO `(UnitId, PlatformId, DroneCount, MaxDrones)`; `IsDestroyed`, `IntegrityFraction`. |
| [`SwarmUnitFactory.cs`](../../src/ProjectAegis.Data/Catalog/SwarmUnitFactory.cs) | `TryCreate` / `Create` a `SwarmUnitIntegrity` from a swarm catalog row (initial count defaults to `MaxDrones`, clamped to `[0, max]`). |
| [`SwarmMissionType.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/SwarmMissionType.cs) | Scenario tasking mission types (`Patrol`/`Support`/`Strike`) that map to default operational modes; string round-trip via `SwarmMissionTypeNames`. |

### Sim core — the controller

| File | Role |
|------|------|
| [`SwarmController.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) | The aggregate runtime: register units, issue Hold/Move/Attack intents, operational modes, formations, host bind + link refresh, authorized integrity damage/regen, host-stores regen, expend, centroid `Tick`, order-log fingerprint + integrity-timeline hash, and static replay reconstructors. |
| [`SwarmIntentKind.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmIntentKind.cs) | Phase A intents: `Hold(0)` / `Move(1)` / `Attack(2)`. |
| [`SwarmOperationalMode.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOperationalMode.cs) | Phase B modes: `Hold` / `Assault` / `Screen` / `Scatter` / `Rejoin` (distinct from intents). |
| [`SwarmLinkState.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkState.cs) | C2/order-channel health: `Connected` / `Degraded` / `Lost`. Orders are blocked while `Lost`. |
| [`SwarmLinkEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs) | Pure link rules from host range (`degraded ≥1.0°`, `lost ≥2.0°`), host liveness, and jam. |
| [`SwarmRegenEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs) | Pure regen gates (host alive + has stores + within `0.5°` + room under max). |
| [`SwarmPerformanceCaps.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) | Logical-vs-render caps: `LogicalMaxDronesPerSwarm=40`, `RenderMaxMembersPerSwarm=12`, `DesignMaxConcurrentSwarms=16`, and the aggregate work-unit accounting. |
| [`SwarmOrderLog.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLog.cs) / [`SwarmOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLogEntry.cs) | Append-only intent log + fingerprint. |
| [`SwarmIntegrityChange.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmIntegrityChange.cs) | One integrity delta `(SequenceId, SimTick, SimTime, UnitId, Previous, New, DronesLost, ReasonCode)`; regen rows have `New > Previous` and `DronesLost = 0`. |
| [`SwarmModeOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmModeOrderLogEntry.cs) / [`Formation/SwarmFormationOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormationOrderLogEntry.cs) / [`Expend/SwarmExpendOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendOrderLogEntry.cs) | Per-facet logged records for mode/formation/expend orders. |

### Sim core — Phase C behaviours

| File | Role |
|------|------|
| [`Formation/SwarmFormation.cs`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormation.cs) | Soft layout formations `Cloud`/`Wall`/`Spear`/`Orbit` (cosmetic member offsets — **not** engagement SoT). `SwarmFormationLayout` computes member offsets. |
| [`Assault/SwarmAssaultAxisSplitter.cs`](../../src/ProjectAegis.Sim/Swarm/Assault/SwarmAssaultAxisSplitter.cs) | Pure deterministic multi-axis auto-split planner (`Assault` mode only): floor-division shares (each `≥1`, sum == `droneCount`), seed-deterministic remainder order, fanned approach bearings. Emits `SwarmAssaultSplitPlan` of `SwarmAssaultAxisAllocation`. |
| [`Expend/SwarmExpendResult.cs`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendResult.cs) | Outcome of an authorized kamikaze/expend pulse (irreversible integrity spend). |
| [`SoftKill/SwarmSoftKillApplicator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmSoftKillApplicator.cs) | External soft-kill effects: EMP mode-freeze (+optional Scatter), jam → `SetLinkState`, freeze-aware `TryIssueMode`, append-only event log. |
| [`SoftKill/SwarmEmpEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmEmpEvaluator.cs) | Pure EMP freeze math (`30s` default freeze window, merge = later horizon). |
| [`SoftKill/SwarmJamEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamEvaluator.cs) / [`SwarmJamSeverity.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamSeverity.cs) | Jam severity → `SwarmLinkState` mapping (`None`/`Degraded`/`Lost`). |

### Sim — CEC cooperative-engagement mesh

| File | Role |
|------|------|
| [`Cec/CecMeshController.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshController.cs) | Same-side CEC mesh membership + composite-track picture: register nodes, `Refresh` pairwise states, `ContributeOrganic`, `TryGetCompositeTracks` (needs ≥2 mesh contributors; fire-control quality needs ≥1 `InMesh` contributor with quality ≥0.6). |
| [`Cec/CecMeshEvaluator.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshEvaluator.cs) | Pure mesh-state rules (`InMesh ≤2.0°`, `Degraded ≤4.0°`; non-capable/dead/jammed/peerless → `OutOfMesh`). |
| [`Cec/CecMeshState.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshState.cs) | `InMesh` / `Degraded` / `OutOfMesh`. |
| [`Cec/CecCompositeTrack.cs`](../../src/ProjectAegis.Sim/Cec/CecCompositeTrack.cs) / [`CecNodeRegistration.cs`](../../src/ProjectAegis.Sim/Cec/CecNodeRegistration.cs) / [`CecMeshEvent.cs`](../../src/ProjectAegis.Sim/Cec/CecMeshEvent.cs) | Composite track DTO, node registration input, and join/leave/degrade event. |

### Sim — engagement integration

| File | Role |
|------|------|
| [`Engage/SwarmHardCounterAa.cs`](../../src/ProjectAegis.Sim/Engage/SwarmHardCounterAa.cs) | Hard-counter AA profile table: `PointFire` (default `1` drone/hit) vs `AreaAa` (default `8` drones/hit); per-scenario overrides ride on `EngageContext`. |
| [`Engage/SwarmEngagementIntegrityApplier.cs`](../../src/ProjectAegis.Sim/Engage/SwarmEngagementIntegrityApplier.cs) | Applies engagement losses **only** via `SwarmController.TryApplyIntegrityDamage`; `ApplyHits` short-circuits on destruction; `TryApplyFromEngageContext` resolves loss from an `EngageContext`. |
| [`Engage/CecRemoteEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) | Engage-on-remote-data gate: organic FC preferred; remote allowed only for a CEC-capable shooter with an FC-quality composite track whose primary contributor is **not** the shooter. Otherwise `CecRemoteTrackUnavailable`. |
| [`Engage/SwarmSalvoDeconfliction.cs`](../../src/ProjectAegis.Sim/Engage/SwarmSalvoDeconfliction.cs) | Deterministic one-shooter-per-target allocation by sorted `(shooterId, targetId, weaponId)` (req 14). |
| [`Sensors/SwarmContactLabel.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactLabel.cs) | Formats a `SwarmContactClassificationResult` for projection (e.g. `UAS swarm cloud (0.82)`). |

### Delegation / presentation

| File | Role |
|------|------|
| [`Sim/SwarmAgentIntentIssuer.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentIntentIssuer.cs) | Lets **agents** issue the same intents/modes as humans through the controller, with actor attribution and machine-readable reject reasons (`UNKNOWN_UNIT`, `MISSING_AGENT_ID`, `LINK_LOST`, …). |
| [`Sim/SwarmOrderActor.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmOrderActor.cs) / [`SwarmAgentOrderRequest.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentOrderRequest.cs) / [`SwarmAgentOrderResult.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentOrderResult.cs) / [`SwarmAgentOrderLogPayload.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentOrderLogPayload.cs) | `Player`/`Agent` actor, request/result records, and attribution payload. |
| [`Projection/SwarmUnitPanelProjection.cs`](../../src/ProjectAegis.Delegation/Projection/SwarmUnitPanelProjection.cs) | C2 unit panel: integrity line + Phase B mode/host/link/CEC fields (unknown-with-reason when telemetry is missing). |
| [`Projection/SwarmIntegrityReadout.cs`](../../src/ProjectAegis.Delegation/Projection/SwarmIntegrityReadout.cs) | Presentation integrity readout with a **textual** count channel (`24/40`) so colour is never the only signal. |
| [`Projection/SwarmMapSymbolProjection.cs`](../../src/ProjectAegis.Delegation/Projection/SwarmMapSymbolProjection.cs) | Projects a swarm as one density-safe APP-6 map symbol with an integrity label suffix. |
| [`Projection/SwarmPanelSnapshot.cs`](../../src/ProjectAegis.Delegation/Projection/SwarmPanelSnapshot.cs) | Input snapshot for the Phase B panel projection. |

---

## Lifecycle: catalog → spawn → orders → tick → integrity → replay

```
CatalogSwarmPlatform (maxDrones, CecCapable, defaultMode, requiresHost)
        │  SwarmUnitFactory.Create(unitId, platformId, catalog)
        ▼
SwarmUnitIntegrity (UnitId, PlatformId, DroneCount, MaxDrones)
        │  SwarmController.Register(integrity, latDeg, lonDeg)   ← clamps to logical cap (40)
        ▼
SwarmController (per unit: Intent, Mode, Formation, LinkState, HostId, centroid)
        │  Issue{Hold,Move,Attack}/IssueMode/IssueSetFormation  → append to order logs
        │  BindHost / PublishHostState / RefreshLinkState        → C2 link health
        │  Tick(deltaSeconds)                                    → advance centroid (Move/Attack/Screen)
        │  TryApplyIntegrityDamage / Regen / IssueExpend         → integrity timeline (authorized only)
        ▼
Fingerprints: ComputeOrderLogFingerprint() + ComputeIntegrityTimelineHash()
        │  SwarmReplayHarness.Run/Replay
        ▼
SwarmReplayResult (canonical fingerprint; same seed → byte-stable)
```

Registration is where `maxDrones` and the living count get clamped to
`SwarmPerformanceCaps.LogicalMaxDronesPerSwarm` (40). Every unit starts `Hold` intent, `Hold` mode,
`Cloud` formation, and `Connected` link.

---

## Intents, modes & formations

- **Intents** (`IssueHold`/`IssueMove`/`IssueAttack`) drive centroid motion. `Tick(deltaSeconds)`
  advances Move/Attack units toward their waypoint at `SpeedDegPerSecond` (default `0.05°/s`); Hold
  is stationary. `Attack` additionally records a target unit id.
- **Operational modes** are orthogonal to intents. `Screen` mode gravitates the centroid toward a
  bound, alive host each tick (before waypoint motion). Modes are logged separately
  (`ModeOrderLog`).
- **Formations** are **soft layout constraints only** — they change cosmetic member offsets, never
  the aggregate engagement/integrity state. They are logged (`FormationOrderLog`) but do not affect
  fingerprinted combat.
- **Order gating:** all issue methods call `EnsureOrdersAccepted`, which throws when
  `LinkState == Lost` (SWARM-12). `NotifyHostLost` forces `Lost` link + `Hold` mode/intent and marks
  the host dead.

## C2 link vs CEC mesh — two independent channels

This split is deliberate and enforced structurally (the CEC types never reference swarm C2 types):

| | **C2 link** (`SwarmLinkState`) | **CEC mesh** (`CecMeshState`) |
|--|--------------------------------|-------------------------------|
| Question | Can I *command* this swarm? | Can these sensors *share tracks*? |
| Source | host range + host liveness + jam | same-side CEC peers in range + jam |
| Ranges | degraded ≥1.0°, lost ≥2.0° | InMesh ≤2.0°, Degraded ≤4.0° |
| Effect | `Lost` blocks new orders | mesh state gates composite-track fire-control quality |
| Jam | jam → `Lost` | jam → `OutOfMesh` (does **not** imply C2 lost) |

## Hard-counter AA & offensive effect

Area-AA / flak / CIWS-class fire is a hard counter: at equal nominal DPS framing it shreds many
more drones per successful hit than point fire (`8` vs `1` by default). Losses are applied through
`SwarmEngagementIntegrityApplier` (authorized path), with per-scenario overrides carried on
`EngageContext`. Engagement work stays aggregate — one integrity op per swarm unit per pulse,
independent of the logical drone total.

## Soft-kill: EMP & jam

`SwarmSoftKillApplicator` layers external EW effects on top of a controller **without** rewriting
its internals:

- **EMP** freezes mode switches for a sim-time window (default `30s`, overlapping freezes merge to
  the later horizon) and, at onset, may recommend `Scatter` (only when the link is not `Lost`).
  `TryIssueMode` refuses (no mutation) while frozen and logs `ModeBlocked`.
- **Jam** maps severity to `SwarmLinkState` via `SetLinkState` (`Degraded`/`Lost`); `ClearJam` can
  restore `Connected` or re-evaluate from geometry. All effects are append-only events with explicit
  reason strings.

## Assault multi-axis split

In `Assault` mode (and only when doctrine allows and `axisCount ≥ 2`),
`SwarmAssaultAxisSplitter.Plan` allocates living mass across approach axes: floor-division base
share, remainder distributed in a **seed-deterministic** (SplitMix64/Fisher–Yates) axis order so
every axis gets `≥1` drone and shares sum exactly to the living count. Approach bearings fan
symmetrically around the target bearing at `30°` spacing. It is a pure planner — no per-drone
physics.

## Expend / kamikaze pulse

`SwarmController.IssueExpend` spends N drones irreversibly through the authorized integrity path.
It is **not** self-authorizing: callers must pass `expendAuthorized` sourced from doctrine/WRA (the
`EffectivePolicy.ExpendAuthorized` flag — see [autonomy-roe-gating.md](autonomy-roe-gating.md)). It
returns a `SwarmExpendResult` with a deny reason (`expend-unauthorized`, `expend-count-invalid`,
`expend-no-drones`, …) or a success payload with the integrity change.

## Host regen

`TryRegenNearHost` fails closed: it restores drones (default 1/pulse, clamped to `maxDrones`) only
when the bound host is published, alive, has stores, and the swarm is within range (`0.5°`). Regen
rows land in the integrity timeline with `New > Previous`.

## CEC composite tracks & remote engage

`CecMeshController` builds same-side composite tracks from organic contributions of mesh-connected
nodes. A track needs **≥2** contributors; **fire-control quality** additionally requires at least one
`InMesh` contributor with sensor quality `≥0.6` (degraded-only mesh yields FC=false). `CecRemoteEngageGate`
then decides whether a shooter may fire on remote data: organic FC is always preferred; remote is
only usable by a **CEC-capable, `InMesh`** shooter against an FC-quality track whose primary
contributor is someone else — otherwise the shot aborts with `CecRemoteTrackUnavailable`. The gate
never invents per-drone fire-control bodies (aggregate SoT unchanged).

## Presentation

All C2 surfaces are read-only projections over aggregate state: a single selection id, an integrity
readout with a **textual** count channel (`24/40`, so colour is never the only cue), a density-safe
APP-6 map glyph, and a Phase B panel that renders mode/host/link/CEC with explicit
*unknown (reason)* when telemetry is missing. Agents issue the same intents as humans via
`SwarmAgentIntentIssuer`, which attributes each order to a `Player`/`Agent` actor.

---

## Determinism & replay

- `SwarmController.ComputeOrderLogFingerprint()` and `ComputeIntegrityTimelineHash()` fold the seed,
  ordered order-log entries, the full integrity timeline, and the living end-state into stable
  `ulong` mixes (ordinal-sorted, `SimWorldHash`-based).
- [`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) runs a canonical
  Phase A golden (Hold → Move → Attack + point-fire then area-AA hits, `seed=42`) and a
  `Replay(...)` reconstructor that rebuilds from recorded orders + integrity timeline onto a fresh
  controller with the same spawn. Same seed → byte-stable canonical fingerprint. It also exposes a
  design-max stress runner (`16` swarms × `60` ticks) that asserts aggregate `O(swarms×ticks)` work.
- This golden is **isolated** from the Baltic v2 order-log golden 6/6 and does **not** touch the
  production replay hash `17144800277401907079`.

---

## How to extend without breaking goldens

1. **Never mutate drone count directly.** Route every loss/gain through
   `TryApplyIntegrityDamage` / `TryApplyIntegrityRegen` (or a helper that does), so it lands in the
   integrity timeline and the hash.
2. **Keep the aggregate SoT.** New behaviour must be `O(swarm units)`; do not iterate logical
   drones as authority. Use `SwarmPerformanceCaps` for LOD/work accounting.
3. **Keep C2 and CEC separate.** Do not couple `SwarmLinkState` to `CecMeshState`. Add CEC logic
   under `Sim/Cec/`; add C2/order logic under `Sim/Swarm/`.
4. **Stay deterministic.** No wall-clock, no `Random.Shared`. Derive any needed randomness from the
   seed with the existing SplitMix64/Fisher–Yates helpers, and iterate in ordinal order.
5. **Authorize offensive effects upstream.** Expend/auto-engage authorization belongs in
   `EffectivePolicy` / the WRA gate, not in the controller (surface discipline).
6. **Re-run the swarm golden** (`SwarmReplayHarness` tests) plus the pressure suite; a fingerprint
   move must be intentional. The Baltic v2 hash must stay `17144800277401907079`.

---

## Tests (behaviour pins)

| Area | Test file |
|------|-----------|
| Controller core / intents / integrity | [`SwarmControllerTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmControllerTests.cs) |
| Modes / host / link | [`SwarmModeHostLinkTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmModeHostLinkTests.cs) |
| Replay + performance caps | [`SwarmReplayAndCapsTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmReplayAndCapsTests.cs) |
| Design-max pressure | [`SwarmPressureTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs) |
| Formations | [`Formation/SwarmFormationTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Formation/SwarmFormationTests.cs) |
| Assault axis split | [`Assault/SwarmAssaultSplitTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Assault/SwarmAssaultSplitTests.cs) |
| Expend / kamikaze | [`Expend/SwarmExpendTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Expend/SwarmExpendTests.cs) |
| Host regen | [`SwarmRegenTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmRegenTests.cs) |
| Soft-kill (EMP/jam) | [`SoftKill/SwarmSoftKillTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SoftKill/SwarmSoftKillTests.cs) |
| Hard-counter AA / offensive effect | [`Engage/SwarmOffensiveEffectTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/SwarmOffensiveEffectTests.cs), [`Engage/SwarmEngageHotpathTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/SwarmEngageHotpathTests.cs) |
| Salvo deconfliction | [`Engage/SwarmSalvoDeconflictionTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/SwarmSalvoDeconflictionTests.cs) |
| CEC mesh | [`Cec/CecMeshControllerTests.cs`](../../src/ProjectAegis.Sim.Tests/Cec/CecMeshControllerTests.cs) |
| CEC remote engage | [`Engage/CecRemoteEngageTests.cs`](../../src/ProjectAegis.Sim.Tests/Engage/CecRemoteEngageTests.cs) |
| Contact classification | [`Sensors/SwarmContactClassifierTests.cs`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmContactClassifierTests.cs), [`Sensors/SwarmDetectionLoopIntegrationTests.cs`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmDetectionLoopIntegrationTests.cs) |
| Doctrine / WRA | [`Policy/SwarmDoctrinePolicyTests.cs`](../../src/ProjectAegis.Sim.Tests/Policy/SwarmDoctrinePolicyTests.cs) |
| Agent-issued intents | [`Sim/SwarmAgentIntentIssuerTests.cs`](../../src/ProjectAegis.Delegation.Tests/Sim/SwarmAgentIntentIssuerTests.cs) |
| C2 projections | [`Projection/SwarmC2ProjectionTests.cs`](../../src/ProjectAegis.Delegation.Tests/Projection/SwarmC2ProjectionTests.cs) |
| Catalog / scenario placement | [`Catalog/SwarmPlatformCatalogTests.cs`](../../src/ProjectAegis.Data.Tests/Catalog/SwarmPlatformCatalogTests.cs), [`Scenario/SwarmScenarioPlacementTests.cs`](../../src/ProjectAegis.Data.Tests/Scenario/SwarmScenarioPlacementTests.cs), [`Scenario/SwarmMissionTypeTests.cs`](../../src/ProjectAegis.Data.Tests/Scenario/SwarmMissionTypeTests.cs) |
