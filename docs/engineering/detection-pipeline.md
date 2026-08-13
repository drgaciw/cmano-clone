# Detection / contact pipeline — developer guide

The **tick-4 detection slice** is where scenario units *see* each other: it rolls a probability
of detection (`Pd`) per sensor trial, promotes contacts through a lifecycle FSM
(`Unknown → Detected → Classified → Identified → Lost`), optionally shares the picture across a
side's datalink, and folds every roll into a deterministic **detection sub-hash** for replay
pinning. It is the seam that produces the *fire-control track* the
[engagement pipeline](engagement-pipeline.md) needs — the first thing `MvpEngagementResolver`
checks is whether a track exists, and that track comes from here.

- **Source:** [`src/ProjectAegis.Sim/Sensors/`](../../src/ProjectAegis.Sim/Sensors/) — engine-agnostic
  pure C#. The scenario-side inputs (trials, jammers, EMCON, lifecycle tuning) live under
  [`src/ProjectAegis.Sim/Scenario/`](../../src/ProjectAegis.Sim/Scenario/).
- **Related:** how to *author* the JSON inputs (`detection`, `catalogDetection`, `contacts`,
  `jammers`, `contactLifecycle`, `emcon`, `datalink`) is in
  [`scenario-policy-authoring.md`](scenario-policy-authoring.md); the *concepts* (seeded RNG,
  world-state hashing, golden workflow) are in [`determinism-and-replay.md`](determinism-and-replay.md);
  the *end-to-end runner* that drives this slice each tick is
  [`baltic-replay-harness.md`](baltic-replay-harness.md). This page documents what the sensor
  code actually **does** at runtime and how to extend it without breaking replay goldens.

> **Why a separate detection sub-hash.** Detection is mixed into its own layer
> (`SimTickPipeline.DetectionSubhash`) before it is folded into the combined world hash. That lets
> the replay harness pin *detection* independently of engagement/combat, so a change to Pd rolls
> or the contact FSM is caught even when the engagement layer is unchanged.

---

## Where it lives

| File | Role |
|------|------|
| [`DeterministicDetectionLoop.cs`](../../src/ProjectAegis.Sim/Sensors/DeterministicDetectionLoop.cs) | `RollTick(...)` — the pure, sorted per-tick roll of every detection trial. |
| [`DetectionProbability.cs`](../../src/ProjectAegis.Sim/Sensors/DetectionProbability.cs) | The `Pd` formula (`ComputePd`). |
| [`SensorModality.cs`](../../src/ProjectAegis.Sim/Sensors/SensorModality.cs) | `Radar` / `Infrared` / `Visual` trial modality (S111 / DRG-10). Radar folds RF jam + EMCON; IR/Visual do not. |
| [`IrVisualDetection.cs`](../../src/ProjectAegis.Sim/Sensors/IrVisualDetection.cs) | Optical/thermal env-mask helpers (`ComputeVisualEnvMask`, `ComputeInfraredEnvMask`) for IR/EO trials. |
| [`SwarmSensorScale.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmSensorScale.cs) | Scales detection Pd by living swarm integrity (SWARM-04 / DRG-89) — see [swarm-runtime.md](swarm-runtime.md). |
| [`DetectionEntityId.cs`](../../src/ProjectAegis.Sim/Sensors/DetectionEntityId.cs) | FNV-1a hash of `(observer, sensor, target)` → the stable RNG entity id. |
| [`DetectionRollResult.cs`](../../src/ProjectAegis.Sim/Sensors/DetectionRollResult.cs) | One roll outcome: `(Trial, Pd, Draw, Detected)`. |
| [`DetectionWorldHash.cs`](../../src/ProjectAegis.Sim/Sensors/DetectionWorldHash.cs) | `MixTick(previous, rolls)` — the replay sub-hash fold. |
| [`ContactLifecycleState.cs`](../../src/ProjectAegis.Sim/Sensors/ContactLifecycleState.cs) / [`ContactTransition.cs`](../../src/ProjectAegis.Sim/Sensors/ContactTransition.cs) | The FSM enum and the emitted transition record. |
| [`PdDetectionContactSimulator.cs`](../../src/ProjectAegis.Sim/Sensors/PdDetectionContactSimulator.cs) | The stateful Pd-driven contact tracker (lifecycle, stale loss, primary target, BDA/kill removal). |
| [`ScenarioContactSimulator.cs`](../../src/ProjectAegis.Sim/Sensors/ScenarioContactSimulator.cs) | The simpler *scheduled* contact simulator (`appearAtTick` seeds, no Pd). |
| [`DatalinkSidePictureMerger.cs`](../../src/ProjectAegis.Sim/Sensors/DatalinkSidePictureMerger.cs) | Bounded per-side picture sharing (TR-sensor-004) with optional share lag and comms gating. |
| [`HostileContactFilter.cs`](../../src/ProjectAegis.Sim/Sensors/HostileContactFilter.cs) | Whether a target id is an engageable hostile (blue never / registry-red / `hostile*`). |
| [`ScenarioJamResolver.cs`](../../src/ProjectAegis.Sim/Sensors/ScenarioJamResolver.cs) | Effective noise-jam strength for a trial. |
| [`DetectionTrialResolver.cs`](../../src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs) | Builds sorted trials from inline JSON **or** catalog `basePd`. |

---

## Two contact sources: Pd-driven vs scheduled

A scenario yields **one** simulator, chosen by the harness ([`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs)):

1. **`PdDetectionContactSimulator`** — used when the profile resolves ≥1 detection trial.
   `DetectionTrialResolver.Resolve` prefers the inline `detection` array; if empty it builds one
   trial per `catalogDetection` target, pulling `basePd` from the platform catalog
   (`ICatalogReader.TryGetBasePd` — **throws** if the catalog has no `basePd` for that
   platform/sensor) and applying the Phase-B catalog detection modifier.
2. **`ScenarioContactSimulator`** — the fallback when there are no trials but the profile has
   scripted `contacts` (`ScenarioContactSeed`). Each seed simply flips `Unknown → Detected` at
   its `appearAtTick` (gated by EMCON if `requiresActiveRadar`), carrying an explicit
   `hasFireControlTrack`. No Pd, no stale loss — it is the deterministic scripted path.

Both expose the same primary-target surface (`PrimaryTargetId`, `PrimaryBlueForceTargetId`,
`PrimaryHasFireControlTrack`) that the engagement layer reads.

---

## The deterministic roll (`RollTick`)

`DeterministicDetectionLoop.RollTick` is a **pure function**: same inputs → same rolls. Each tick:

1. **Sort** trials `ObserverId → SensorId → TargetId` (Ordinal). The simulator sorts once at
   construction and passes `trialsPreSorted: true` to avoid re-sorting per tick.
2. **Skip already-detected** contacts (`alreadyDetectedContactIds`) — a live contact is not
   re-rolled.
3. **EMCON gate:** if `trial.RequiresActiveRadar` and the observer's radar is not `Active`
   (`ScenarioEmconResolver.ResolveRadar` → `CatalogRadarEmconResolver`), skip the trial.
4. **Jam:** for **radar** trials only, `jamStrength = max(trial.JamStrength, ScenarioJamResolver.ResolveJam(...))`.
   A jammer applies when `simTick >= ActiveFromTick`, its `TargetId` matches, and its optional
   `ObserverId` matches (null = all observers). **Infrared/Visual** trials skip the RF jammer fold
   entirely and use only their authored `trial.JamStrength` (see
   [Sensor modality](#sensor-modality-radar--infrared--visual)).
5. **Compute `Pd`** (below) and draw
   `SeededRng.UnitFloat(seed, RngDomain.Detection, entityId, simTick, drawIndex++)`, where
   `entityId = DetectionEntityId.FromTrial(observer, sensor, target)` (FNV-1a). `drawIndex`
   increments over the sorted, non-skipped trials, so the RNG stream is stable per `(scenario, seed)`.
6. **Detected** when `draw < Pd`. Emit a `DetectionRollResult(trial, Pd, draw, detected)`.

### The `Pd` formula

```csharp
Pd = clamp(basePd * envMask * eccmFactor * (1 - clamp(jamStrength, 0, 1)) * clamp(swarmIntegrityScale, 0, 1), 0, 1)
```

| Factor | Meaning | Default |
|--------|---------|---------|
| `basePd` | Base detection probability (inline JSON or catalog). | — |
| `envMask` | Environmental attenuation (weather/terrain/sea-state); for IR/Visual it carries the optical/thermal mask (below). | `1.0` |
| `eccmFactor` | Electronic counter-countermeasures multiplier. | `1.0` |
| `jamStrength` | Effective noise jam (higher = worse), clamped `[0,1]`. **RF jammers only fold into radar trials.** | `0.0` |
| `swarmIntegrityScale` | Sensor degradation of a depleted drone swarm (`SwarmSensorScale`, SWARM-04). `1.0` = no swarm / full integrity; `0` when no drones remain. | `1.0` |

`swarmIntegrityScale` is **precomputed per observer** and carried on the trial
(`ScenarioDetectionTrial.SwarmIntegrityScale`) — scenarios/spawners derive it from living integrity
via [`SwarmSensorScale.ForTrial(droneCount, maxDrones)`](../../src/ProjectAegis.Sim/Sensors/SwarmSensorScale.cs)
so `ComputePd` stays a pure function of its arguments. Its provenance and the integrity ledger are
documented in [swarm-runtime.md](swarm-runtime.md).

`RngDomain.Detection` is domain `0` — kept distinct from `Engage`/`Combat`/etc. so an added
detection draw never shifts the engagement RNG stream (see [`determinism-and-replay.md`](determinism-and-replay.md)).

---

## Sensor modality (Radar / Infrared / Visual)

S111 / DRG-10 gave each trial a
[`SensorModality`](../../src/ProjectAegis.Sim/Sensors/SensorModality.cs)
(`Radar = 0`, `Infrared = 1`, `Visual = 2`). Modality changes **two** things in `RollTick` and
`DetectionTrialResolver`; the Pd formula itself is unchanged:

| Behaviour | `Radar` (default) | `Infrared` / `Visual` |
|-----------|-------------------|-----------------------|
| RF noise jammers (`ScenarioJammer` / `ScenarioJamResolver`) | Folded into `jamStrength` | **Ignored** — only the trial's authored `JamStrength` applies (optical/IR jam is a separate, default-`0` channel) |
| EMCON / active-radar gate (`RequiresActiveRadar`) | Honoured (a passive radar is skipped) | Forced `false` by the resolver — an EO/IR seeker does not emit, so it is never EMCON-gated |
| `envMask` source | Authored `EnvMask` as-is (no day/night model) | Optical/thermal mask from `IrVisualDetection` (below) |

`Radar` is the schema and enum default, so every pre-S111 call site and inline trial keeps its exact
behaviour — modality is purely additive.

### Optical / thermal env masks

[`IrVisualDetection`](../../src/ProjectAegis.Sim/Sensors/IrVisualDetection.cs) produces the `EnvMask`
value for EO/IR trials (radar continues to use `EnvMask` as authored):

- **Visual/EO** — `ComputeVisualEnvMask(dayFraction, weatherMask = 1.0, nightFloor = 0.05)` =
  `clamp(dayFraction × weatherMask, 0, 1)`, then raised to at least `nightFloor`. The default
  `DefaultVisualNightFloor = 0.05` keeps night from forcing a total blackout (`Pd = 0`).
- **Infrared** — `ComputeInfraredEnvMask(thermalContrast, weatherMask = 1.0)` =
  `clamp(thermalContrast × weatherMask, 0, 1)`. Day/night independent (thermal contrast, not
  illumination).

These are pure helpers; a scenario/spawner computes the mask once and passes it as the trial's
`EnvMask`, so the roll stays deterministic.

### Where modality comes from: the catalog

Modality is an **extend-only** catalog column, not an inline trial field. It flows in only on the
**`catalogDetection`** path (profile-authored inline `detection` trials are used verbatim, with no
modality rewrite):

- [`CatalogSensorBinding.Modality`](../../src/ProjectAegis.Data/Catalog/CatalogSensorBinding.cs) — a
  string, default [`CatalogSensorModalities.Radar`](../../src/ProjectAegis.Data/Catalog/CatalogSensorModalities.cs)
  (`"Radar"` / `"Infrared"` / `"Visual"`). Added by migration
  [`016_sensor_modality.sql`](../../assets/data/catalog/migrations/016_sensor_modality.sql)
  (`ALTER TABLE sensor ADD COLUMN modality TEXT NOT NULL DEFAULT 'Radar'`); the reader's
  `NormalizeModality` maps a null/blank column back to `Radar`, so legacy rows are safe.
- [`DetectionTrialResolver.Resolve`](../../src/ProjectAegis.Sim/Scenario/DetectionTrialResolver.cs)
  joins each `catalogDetection` target to its `(platformId, sensorId)` sensor binding and calls
  `ParseSensorModality` (case-insensitive; **unknown / empty → `Radar`**). For a resolved IR/Visual
  binding it also forces `RequiresActiveRadar = false` on the emitted trial.

Baltic fixtures exercise all three: seed platform `u1` carries `radar-1` (Radar), `fixture-ir-1`
(Infrared, `basePd 0.80`) and `fixture-visual-1` (Visual, `basePd 0.70`); Baltic v3 `ucav-blue` has
an `internal-ir` (Infrared) seeker.

---

## Contact lifecycle FSM

`PdDetectionContactSimulator.Tick(simTick, simTime)` turns rolls into
[`ContactTransition`](../../src/ProjectAegis.Sim/Sensors/ContactTransition.cs) records. States:

| State | Meaning |
|-------|---------|
| `Unknown` (0) | Not on the picture. |
| `Detected` (1) | First detection this tick. |
| `Classified` (2) | Held for `classifyAfterTicks` (0 = disabled). |
| `Identified` (3) | Held for `identifyAfterTicks` (0 = disabled). |
| `Lost` (4) | Missed for the stale threshold, or removed by BDA/kill. |

Per tick, in order:

1. **New detections** — a first `Detected` roll adds the contact, opens a track at `simTick`, and
   emits `Unknown → Detected`. Re-detections just reset `MissedTicks = 0` / `LastSeenTick`.
2. **Promotions** — a live track ages by `simTick - FirstSeenTick`; at `classifyAfterTicks` it
   emits `Detected → Classified`, and at `identifyAfterTicks`, `Classified → Identified`
   (both from [`ScenarioContactLifecycle`](../../src/ProjectAegis.Sim/Scenario/ScenarioContactLifecycle.cs)).
3. **Stale loss** — a contact **not** seen this tick increments `MissedTicks`; once it reaches the
   **effective** stale threshold it emits `→ Lost` and drops off the picture. Candidate ids are
   iterated in Ordinal order so losses are deterministic.

### Comms-degraded staleness

The effective threshold is `max(1, staleThresholdTicks / commsStaleThresholdDivisor)`. The harness
sets the divisor each tick from the current comms state via
`CommsTrackStaleness.StaleThresholdDivisor(commsState, profile.CommsDisplay)` — degraded/denied
comms shrink the threshold, so contacts go stale faster when the datalink is contested. Where the
comms state itself comes from is documented in
[comms-degradation-runtime.md](comms-degradation-runtime.md).

### BDA and kill removal (combat feedback)

Two hooks let combat prune the picture without waiting for a stale timeout:

- **`ApplyTargetKill(...)`** — a confirmed kill (drained from the engagement layer's
  `KilledTargets` registry) forces every contact for that target to `Lost` and marks the target
  destroyed so it is never re-detected.
- **`ApplyTargetBdaLost(...)`** — a battle-damage-assessment hook (`combatDomainsEnabled`,
  ADR-009, via [`BdaContactLifecycleHotTickApplier`](../../src/ProjectAegis.Sim/Catalog/BdaContactLifecycleHotTickApplier.cs))
  promotes contacts to `Lost` for a damaged target **without** marking it destroyed, so a crippled
  unit can drop and re-appear.

Both recompute the primary target afterwards.

### Primary target & fire-control track

`UpdatePrimary` picks the **Ordinal-lowest** engageable-hostile target as `PrimaryTargetId`
(setting `PrimaryHasFireControlTrack = true`) and the Ordinal-lowest blue-force target as
`PrimaryBlueForceTargetId`. Hostility is decided by `HostileContactFilter.IsEngageableHostileTarget`:

- blank → `false`;
- blue force (`BalticV3SideRegistry.IsBlueForceUnit` or the synthetic `u1`) → `false`;
- scenario-registered red (`IsRedForceUnit`) → `true`;
- otherwise legacy synthetic ids starting with `hostile` → `true`.

When a primary is lost, `RecomputePrimary` re-scans the live contacts. These properties feed the
engagement layer's victim resolution (see [engagement-pipeline.md](engagement-pipeline.md) →
*Victim resolution & multi-domain concurrent engage*).

---

## Datalink side-picture merge (TR-sensor-004)

`DatalinkSidePictureMerger` shares organic contacts among peers on the **same side**, off by
default (`organicOnly = true`). It is enabled when the profile's
[`ScenarioDatalinkDoctrine`](../../src/ProjectAegis.Sim/Scenario/ScenarioDatalinkDoctrine.cs) has
`organicOnly = false` **and** a non-empty `unitSides` map. `Merge(organic, simTick, simTime, commsState)`:

1. Applies this tick's organic transitions and (if `shareLagTicks > 0`) queues them to become
   shareable `shareLagTicks` later; `Lost` cancels pending shares.
2. For each side (sorted), for each target, resolves the **best** organic lifecycle rank across
   peers (`Identified > Classified > Detected > Unknown`, `Lost = -1`; ties break Ordinal by
   observer) and emits shared transitions to peers that don't already hold an equal-or-better
   organic track. Shared contacts use the id `dl-<targetId>` and are sorted
   `Observer → Sensor → Target` for stable order-log appends.

Comms quality gates sharing via `DatalinkCommsShareState` (the sim-layer mirror of delegation
`CommsState`):

| `DatalinkCommsShareState` | Effect |
|---------------------------|--------|
| `Nominal` | Full sharing. |
| `Degraded` | Suppresses *new* (`Unknown → …`) shares; still propagates `Lost`. |
| `Denied` | No shared transitions this tick. |

---

## Determinism & the detection sub-hash

Every roll is folded into a 64-bit sub-hash by `DetectionWorldHash.MixTick`, which XOR-mixes the
FNV-1a entity id, a detected flag, and `Pd`/`Draw` scaled to fixed-point (`* 10_000` as `uint`) —
**never** raw floats or enumeration order. The Pd simulator keeps a running `LastDetectionHash`;
`SimTickPipeline` exposes `DetectionSubhash` and folds it into the combined world hash:

```csharp
LastWorldHash = SimWorldHash.Combine(core, DetectionSubhash, engageMix, killMix);
```

Because detection is a distinct layer, checkpoints capture `LastDetectionHash` alongside the sim
hash, and a divergence in detection is diagnosable separately from engagement/combat. **Do not**
introduce dictionary/hashset iteration, wall-clock reads, or float round-trips into any path that
feeds a transition or the sub-hash.

---

## Tick-4 integration (in the harness)

Each tick the [`BalticReplayHarness`](../../src/ProjectAegis.Delegation.UnityAdapter/Baltic/BalticReplayHarness.cs)
runs the detection slice in this fixed order:

1. Update the comms stale divisor (`SetCommsStaleThresholdDivisor`).
2. `transitions = pdSim.Tick(...)` (or `scheduleSim.Tick(...)`).
3. If a datalink merger exists, append its `Merge(...)` output (organic **then** shared, order
   preserved for identical hashing).
4. For each transition: fire any `mission.triggers` (contact-triggered ROE escalation) and
   `AppendContactTransition` to the order log.
5. `bridge.Tick(...)` runs the delegation/engagement tick against the updated picture.
6. Drain the engagement `KilledTargets` and apply `ApplyTargetKill`; apply BDA-lost transitions;
   append both to the order log.
7. On the checkpoint interval, capture `pdSim.LastDetectionHash`.

The contact transitions become order-log entries, which the C2 read-models project into the
tactical picture (see [`c2-projection-layer.md`](c2-projection-layer.md)).

---

## Extending safely

- **New Pd factor?** Add it to `DetectionProbability.ComputePd` *and* thread it through
  `ScenarioDetectionTrial` + `DetectionTrialResolver`. Keep the multiplicative-clamp shape;
  changing it will move detection goldens.
- **New sensor modality?** Extend `SensorModality` and `CatalogSensorModalities`, map the token in
  `DetectionTrialResolver.ParseSensorModality` (keep the unknown → `Radar` default), and decide in
  `RollTick` whether it folds RF jam / is EMCON-gated. Catalog schema changes stay **extend-only**
  (a new additive migration, never an in-place column rewrite).
- **New lifecycle stage or timing?** Extend `ContactLifecycleState` / `ScenarioContactLifecycle`
  and emit the transition inside `EmitLifecyclePromotions` — never mutate state outside `Tick` /
  the BDA/kill hooks.
- **New RNG draw?** Reuse `RngDomain.Detection` and the incrementing `drawIndex`; do not add a new
  draw before the existing ones (it shifts the stream).
- **Anything that emits a transition or touches the sub-hash** must stay deterministic (sorted
  iteration, fixed-point folds). Re-run the replay goldens + QA Gauntlet after any change here.

---

## Tests

`src/ProjectAegis.Sim.Tests/Sensors/` pins this subsystem (~40 facts/theories):

| Test file | Covers |
|-----------|--------|
| `DeterministicDetectionLoopTests` | Sort order, EMCON/already-detected skips, Pd draw stability. |
| `PdDetectionContactSimulatorTests` / `PdContactStaleTests` / `PdContactClassifyTests` | Detection, promotions, stale loss. |
| `PdContactCommsStaleTests` / `PdContactPrimaryBlueForceStaleTests` | Comms-divisor staleness + primary recompute. |
| `PdContactKillTests` / `PdContactBdaLifecycleTests` | Kill / BDA-lost removal. |
| `ScenarioContactSimulatorTests` / `ScenarioContactEmconTests` | Scheduled seeds + EMCON gating. |
| `ScenarioJamResolverTests` / `EccmScenarioFactorTests` | Jam/ECCM Pd inputs. |
| `IrVisualDetectionTests` | Optical/thermal env masks + RF-jam gate (radar suppressed, IR/Visual unaffected). |
| `DetectionTrialResolverTests` | Catalog `modality` → trial modality + `RequiresActiveRadar` for IR/Visual. |
| `SwarmSensorScaleTests` / `SwarmDetectionLoopIntegrationTests` | Swarm-integrity Pd scaling. |
| `DatalinkSidePictureMergerTests` | Side sharing, share lag, comms-gated sharing. |

The catalog-side modality column + migration/idempotency is pinned by
`ProjectAegis.Data.Tests/Catalog/SensorModalityCatalogTests`.

---

## See also

| Doc | For |
|-----|-----|
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Authoring the `detection` / `catalogDetection` / `contacts` / `jammers` / `contactLifecycle` / `emcon` / `datalink` JSON. |
| [engagement-pipeline.md](engagement-pipeline.md) | How the fire-control track produced here is consumed by `MvpEngagementResolver`. |
| [baltic-replay-harness.md](baltic-replay-harness.md) | The runner that drives the tick-4 detection slice end-to-end. |
| [determinism-and-replay.md](determinism-and-replay.md) | Seeded RNG domains, world-hash layers, and the golden-fixture workflow. |
| [c2-projection-layer.md](c2-projection-layer.md) | How contact transitions become the tactical picture. |
| [`ProjectAegis.Sim/README.md`](../../src/ProjectAegis.Sim/README.md) | The `Sensors/` folder in the wider simulation core. |
