# Swarm runtime — aggregate drone-swarm source-of-truth (SWARM-A/B/C, DRG-87…107)

The **swarm runtime** models near-future drone swarms as a single **aggregate** platform, not a
crowd of individually simulated bodies. One swarm unit carries an integer *living drone count*
(its integrity), an aggregate *centroid* position, one *intent* (Hold/Move/Attack), one Phase-B
*operational mode*, one *formation*, and a C2 *link state*. Combat, damage, and detection treat the
swarm as **one work unit per pulse** regardless of how many logical drones it contains — the render
LOD is capped and cosmetic. It is a pure, engine-agnostic `ProjectAegis.Sim` surface.

| Area | Story | Location |
|------|-------|----------|
| **Aggregate controller + centroid/intent/integrity** | SWARM-A2 / DRG-87 | [`SwarmController`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) |
| **Operational modes + host bind + C2 link** | SWARM-B1 / DRG-94 | [`SwarmController`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) · [`SwarmLinkEvaluator`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs) |
| **Host-stores regen** | SWARM-B4 / DRG-97 | [`SwarmRegenEvaluator`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs) |
| **Multi-axis assault split** | SWARM-17 / DRG-106 | [`Assault/`](../../src/ProjectAegis.Sim/Swarm/Assault/) |
| **Expend / kamikaze pulse** | SWARM-19 | [`Expend/`](../../src/ProjectAegis.Sim/Swarm/Expend/) |
| **Formations (soft layout)** | SWARM-16 / DRG-105 | [`Formation/`](../../src/ProjectAegis.Sim/Swarm/Formation/) |
| **EMP / jam soft-kill** | SWARM-18 / DRG-107 | [`SoftKill/`](../../src/ProjectAegis.Sim/Swarm/SoftKill/) |
| **Golden replay + performance caps** | SWARM-24/25 / DRG-91 | [`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) · [`SwarmPerformanceCaps`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) |
| **Engagement effect (offense + AA hard-counter)** | SWARM-04/07/08 / DRG-88 | [`Engage/`](../../src/ProjectAegis.Sim/Engage/) (`SwarmOffensiveEffect`, `SwarmHardCounterAa`, `SwarmEngagementIntegrityApplier`) |
| **Agent/human intent delegation** | SWARM-23 / B8 / DRG-100 | [`SwarmAgentIntentIssuer`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentIntentIssuer.cs) |

> **Boundary / invariants:**
> - **Aggregate SoT, not per-drone physics (SWARM-07).** There is no per-drone body; integrity is an
>   integer count. Combat cost is O(swarm units) per pulse, never O(drones) — see
>   [`SwarmPerformanceCaps.EngagementWorkUnitsPerPulse`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs).
> - **Integrity is write-protected.** The living count only moves through the authorized
>   `TryApplyIntegrityDamage` / `TryApplyIntegrityRegen` methods (each appends a timeline row); there
>   is no public field mutation.
> - **C2 link state is independent of the CEC mesh.** `SwarmLinkState` is the *order channel*;
>   whether a unit is *in CEC mesh* is a separate axis owned by [cec-mesh-runtime.md](cec-mesh-runtime.md)
>   (SWARM-31 / B6). The swarm types never reference the CEC types and vice-versa.
> - **Deterministic.** Ordinal iteration, append-only sequence-numbered logs, and seeded pure
>   planners (assault split, formation offsets). No `Random.Shared`, no wall-clock. Swarm hashes are
>   **isolated from the Baltic v2 order-log golden 6/6** — the swarm harness does not touch it.
> - **Sim-only.** No Unity, no `DelegationBridge`. Agents drive the swarm through the same public
>   controller API as humans (SWARM-23).

---

## Types

| Type | Kind | Role |
|------|------|------|
| [`SwarmController`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) | `sealed class` | Stateful aggregate registry: centroid motion, intents/modes/formations, authorized integrity, host bind, link state, and per-family order logs. |
| [`SwarmUnitIntegrity`](../../src/ProjectAegis.Data/Catalog/SwarmUnitIntegrity.cs) | `sealed record` (Data) | Registration payload `(UnitId, PlatformId, DroneCount, MaxDrones)` with `IsDestroyed` / `IntegrityFraction`. |
| [`SwarmIntentKind`](../../src/ProjectAegis.Sim/Swarm/SwarmIntentKind.cs) | `enum` | Phase-A intent: `Hold=0` / `Move=1` / `Attack=2`. |
| [`SwarmOperationalMode`](../../src/ProjectAegis.Sim/Swarm/SwarmOperationalMode.cs) | `enum` | Phase-B mode: `Hold=0` / `Assault=1` / `Screen=2` / `Scatter=3` / `Rejoin=4`. |
| [`SwarmFormation`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormation.cs) | `enum` | Soft layout: `Cloud=0` / `Wall=1` / `Spear=2` / `Orbit=3`. |
| [`SwarmLinkState`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkState.cs) | `enum` | C2 order-channel health: `Connected=0` / `Degraded=1` / `Lost=2`. |
| [`SwarmIntegrityChange`](../../src/ProjectAegis.Sim/Swarm/SwarmIntegrityChange.cs) | `sealed record` | Timeline row `(SequenceId, SimTick, SimTime, UnitId, PreviousDroneCount, NewDroneCount, DronesLost, ReasonCode)`. Regen rows have `DronesLost = 0` and `New > Previous`. |
| [`SwarmOrderLogEntry`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLogEntry.cs) / [`SwarmOrderLog`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLog.cs) | `sealed record` / `sealed class` | Append-only intent log + fingerprint (Hold/Move/Attack rows). |
| `SwarmModeOrderLogEntry` / `SwarmFormationOrderLogEntry` / [`SwarmExpendOrderLogEntry`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendOrderLogEntry.cs) | `sealed record` | Per-family logged changes (mode / formation / expend). |
| [`SwarmLinkEvaluator`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs) | `static` | Pure link-state rules + `RangeDeg`. |
| [`SwarmRegenEvaluator`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs) | `static` | Pure regen gates (`CanRegen`). |
| [`SwarmPerformanceCaps`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) | `static` | Logical vs render caps + aggregate work-unit math. |
| [`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) | `static` | Golden scenario + reconstruct + design-max stress. |

---

## The controller — `SwarmController`

Construction takes a `SimSeed` and an optional aggregate centroid speed
(`DefaultSpeedDegPerSecond = 0.05` deg lat/lon per sim-second; non-positive falls back to the
default). Units are keyed with `StringComparer.Ordinal` for reproducible iteration.

```
Register(SwarmUnitIntegrity, latDeg, lonDeg)   // add/replace; clamps count & max to the logical cap
BindHost(unitId, hostId?) / PublishHostState(hostId, lat, lon, alive)
IssueHold / IssueMove(lat,lon) / IssueAttack(targetId, [lat,lon])   // → SwarmOrderLog, returns sequence id
IssueMode(mode) / IssueSetFormation(formation)                     // → mode / formation logs
IssueExpend(dronesToExpend, expendAuthorized, …, targetId?) → SwarmExpendResult
TryApplyIntegrityDamage(unitId, dronesLost, …, reasonCode, out change)   // authorized loss
TryApplyIntegrityRegen(unitId, dronesGained, …, reasonCode, out change)  // authorized gain (clamps to max)
TryRegenNearHost(unitId, hostHasStores, …, out change)             // gated pulse regen
RefreshLinkState(unitId, jammed) / SetLinkState(unitId, state) / NotifyHostLost(unitId)
Tick(deltaSeconds)                                                 // advance centroids
Get{Intent,Mode,Formation,LinkState,HostId,Centroid,Integrity}
ComputeOrderLogFingerprint() / ComputeIntegrityTimelineHash()
```

**Registration** reads a Data-side `SwarmUnitIntegrity` plus a spawn centroid. `MaxDrones` is clamped
to [`SwarmPerformanceCaps.LogicalMaxDronesPerSwarm`](#performance-caps--swarmperformancecaps) (**40**),
and the living count is clamped into `[0, max]`. New units default to `Hold` intent, `Hold` mode,
`Cloud` formation, and `Connected` link.

**Intents (Phase A).** `IssueMove` plots a waypoint; `IssueAttack` records a target unit id and an
optional waypoint toward it; `IssueHold` clears the waypoint and target. Each appends a
`SwarmOrderLogEntry` and returns its sequence id.

**Tick** advances the centroid for `Move`/`Attack` units that have a waypoint (straight-line toward
the target at `SpeedDegPerSecond`, snapping when the step overshoots); `Hold` is stationary. When a
unit is in **`Screen`** mode and bound to a known, alive host, the centroid instead gravitates toward
the host each tick. Units at zero integrity are skipped. Iteration is ordinal-sorted.

**Integrity is authorized-only.** `TryApplyIntegrityDamage` reduces the living count (clamped so it
never goes negative) and appends a loss row; `TryApplyIntegrityRegen` adds (clamped to `MaxDrones`)
and appends a `DronesLost = 0` gain row. Both fail closed (`false`, no mutation, no throw) on unknown
unit / already-destroyed / no-room. This is the *only* path that mutates integrity — engagement code
reaches it through the [sink interface](#engagement-touchpoints).

**Orders require a live link.** `IssueHold/Move/Attack`, `IssueMode`, `IssueSetFormation`, and
`IssueExpend` first call `EnsureOrdersAccepted`, which **throws** `InvalidOperationException` when
`LinkState == Lost` (SWARM-12). `Degraded` still accepts orders. `NotifyHostLost` forces `Lost` link
+ `Hold` mode/intent and marks the bound host dead.

---

## C2 link state — `SwarmLinkEvaluator`

`SwarmLinkState` models **only the C2 / order channel**, not the CEC sensor mesh. `Evaluate` is a pure
function of `(rangeToHostDeg?, hostAlive, jammed, degradedRangeDeg=1.0, lostRangeDeg=2.0)`:

- **`Lost`** when the host is dead, or `jammed`, or range `≥ lostRangeDeg` (**default `2.0°`**);
- **`Degraded`** when `degradedRangeDeg ≤ range < lostRangeDeg` (**default `1.0°`**);
- **`Connected`** when range `< degradedRangeDeg`, **or when there is no host geometry at all**
  (free-flying, unbound swarms are `Connected`).

`RangeDeg` is the Euclidean lat/lon placeholder (`√(Δlat² + Δlon²)`) shared across the Sim kinematics
bands. `SwarmController.RefreshLinkState` recomputes from bound-host geometry; a host that is bound but
whose geometry has not been published yet is treated as `Degraded` (or `Lost` when jammed). These
bands are independent of the CEC mesh's `2.0°` / `4.0°` connected/degraded bands.

---

## Host-stores regen — `SwarmRegenEvaluator`

`CanRegen(rangeDeg?, hostAlive, hostHasStores, droneCount, maxDrones, maxRangeDeg=0.5)` gates SWARM-13
pulse regeneration: the host must be **alive**, **have stores**, the swarm must have **room under max**,
and be **within `maxRangeDeg`** (default `0.5°`) of the host. `SwarmController.TryRegenNearHost`
resolves range from the bound host's published geometry, checks the gates, and applies
`DefaultDronesPerPulse` (**1**) via the authorized regen path — failing closed on any gate.

---

## Multi-axis assault split — `Assault/`

[`SwarmAssaultAxisSplitter.Plan(droneCount, axisCount, mode, seed, doctrineAllowSplit=true,
targetBearingDeg?)`](../../src/ProjectAegis.Sim/Swarm/Assault/SwarmAssaultAxisSplitter.cs) is a **pure
deterministic** planner (allocation only — no per-drone physics). It fans logical mass across `K`
approach axes against a single HVT **only** when mode is `Assault`, doctrine allows, and `K ≥ 2`;
otherwise it returns a single-axis plan with `SplitApplied = false`.

- Effective `K` is reduced to `droneCount` when mass is thinner than the requested axes (min ≥1 drone
  per axis).
- Shares are floor-divided; the remainder is distributed in a **seed-deterministic** axis order
  (Fisher–Yates over a SplitMix64 stream), so shares are reproducible per seed and always **sum
  exactly to `droneCount`** ([`SwarmAssaultSplitPlan.TotalDroneShare`](../../src/ProjectAegis.Sim/Swarm/Assault/SwarmAssaultSplitPlan.cs)).
- Bearings fan symmetrically around the base bearing at `DefaultAxisSpreadDeg` (**30°**) spacing,
  normalized to `[0, 360)`.

---

## Expend / kamikaze pulse — `Expend/`

`SwarmController.IssueExpend(unitId, dronesToExpend, expendAuthorized, …, targetUnitId?)` spends N
drones **irreversibly** when authorized. Authorization (`expendAuthorized`) is passed in by the caller
from doctrine/WRA (B7) — the swarm surface deliberately does **not** call the policy evaluator itself.
It returns a [`SwarmExpendResult`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendResult.cs) that is
`Denied` (with a reason: `expend-unauthorized` / `expend-count-invalid` / `expend-no-drones` /
`expend-integrity-failed`) or `Succeeded`. On success it routes through `TryApplyIntegrityDamage` with
the `expend-pulse` reason (so the loss lands on the same integrity timeline) and logs a
[`SwarmExpendOrderLogEntry`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendOrderLogEntry.cs).

---

## Formations — `Formation/`

Formations are **soft, cosmetic layout constraints**, not engagement SoT.
[`SwarmFormationLayout.ComputeOffsets(formation, droneCount, seed, hostBearingRad?)`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormationLayout.cs)
returns deterministic per-member `(dx, dy)` offsets in degrees from the centroid:

- **Cloud** — uniform-disk scatter (`r = R·√u`) from a seeded LCG stream;
- **Wall** — a line perpendicular to the host bearing (east–west when unbound);
- **Spear** — a line along the host bearing (due north when unbound);
- **Orbit** — an evenly-spaced ring, biased a fraction of the radius toward the host when a bearing is
  known.

The presentation layer must never treat these offsets (or the render-member set) as authority.

---

## EMP / jam soft-kill — `SoftKill/`

[`SwarmSoftKillApplicator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmSoftKillApplicator.cs) is an
**external** applicator that composes over a `SwarmController` without touching its internals, keeping
its own append-only [`SwarmSoftKillEvent`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmSoftKillEvent.cs)
log with explicit reason strings:

- **`ApplyEmp`** — records a per-unit **mode-switch freeze** until `simTime + duration`
  (`DefaultFreezeDurationSeconds = 30`), merging overlapping freezes by taking the later horizon
  ([`SwarmEmpEvaluator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmEmpEvaluator.cs)). It optionally
  recommends `Scatter` (only when the link is not `Lost`).
- **`TryIssueMode`** — the freeze-aware mode gate: returns `false` (no mutation, logs `ModeBlocked`)
  while frozen, otherwise forwards to `IssueMode`.
- **`ApplyJam` / `ClearJam`** — map a [`SwarmJamSeverity`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamSeverity.cs)
  (`None`/`Degraded`/`Lost`) to a C2 `SwarmLinkState` via
  [`SwarmJamEvaluator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamEvaluator.cs) and set it on the
  controller (clear can re-evaluate from geometry). **Jam affects the C2 link only — never the CEC
  mesh.**

---

## Performance caps — `SwarmPerformanceCaps`

Logical integrity and render LOD are **independent** (SWARM-25):

| Constant | Value | Meaning |
|----------|-------|---------|
| `LogicalMaxDronesPerSwarm` | `40` | Combat/integrity ceiling per swarm (registration clamps to this). |
| `RenderMaxMembersPerSwarm` | `12` | Cosmetic member sprites — presentation LOD only, never combat authority. |
| `DesignMaxConcurrentSwarms` | `16` | Design-max first-class swarms in a Phase-A scenario. |
| `DesignMaxLogicalDrones` | `640` | `16 × 40` upper bound on logical drones under design-max load. |
| `StressScenarioTicks` / `StressPulseBudgetMs` | `60` / `2000` | Stress-fixture tick count and generous CI wall-clock budget. |

`EngagementWorkUnitsPerPulse(n) = n` encodes the acceptance invariant: **per-pulse combat work scales
with the number of swarm *units*, not the logical drone total.**

---

## Engagement touchpoints

The swarm participates in the tick-8 engage/kill-chain (see [engagement-pipeline.md](engagement-pipeline.md))
through [`EngageContext`](../../src/ProjectAegis.Sim/Engage/EngageContext.cs) fields
(`ShooterDroneCount`/`ShooterMaxDrones`, `TargetDroneCount`/`TargetMaxDrones`, `TargetAaProfile`, and
the per-scenario AA overrides) — never by expanding into per-drone bodies:

- **Offense scaling (SWARM-04).** When the context marks a swarm *shooter*,
  [`SwarmOffensiveEffect.Scale`](../../src/ProjectAegis.Sim/Engage/SwarmOffensiveEffect.cs) multiplies the
  base Pk by the living integrity fraction (`droneCount / maxDrones`, clamped; monotonic
  non-decreasing). The curve is a documented tuning knob (`ScaleFactorPower`, linear by default).
- **AA hard-counter (SWARM-08).** On a Hit/Kill against a swarm *target*, the resolver reduces aggregate
  integrity via an [`ISwarmIntegrityDamageSink`](../../src/ProjectAegis.Sim/Engage/ISwarmIntegrityDamageSink.cs)
  (production impl: [`SwarmControllerIntegritySink`](../../src/ProjectAegis.Sim/Engage/SwarmControllerIntegritySink.cs)).
  [`SwarmHardCounterAa`](../../src/ProjectAegis.Sim/Engage/SwarmHardCounterAa.cs) sets the drones-lost
  table: `PointFire` sheds **1** drone/hit, `AreaAa` (flak / CIWS-class volume fire) sheds **8** —
  scenarios can override both. [`SwarmEngagementIntegrityApplier`](../../src/ProjectAegis.Sim/Engage/SwarmEngagementIntegrityApplier.cs)
  is the single funnel into the authorized `TryApplyIntegrityDamage`, tagging losses `swarm-aa-point` /
  `swarm-aa-area`.

---

## Agent & human intent delegation — `SwarmAgentIntentIssuer`

[`SwarmAgentIntentIssuer`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentIntentIssuer.cs) (SWARM-23 /
B8) lets **agents issue the same swarm intents as humans** through the controller's public API, adding
actor attribution (`SwarmOrderActor` Player/Agent + `AgentId`) to an attribution log. It validates the
request, requires an `AgentId` for agent-actor orders, applies an optional mode change, then dispatches
`Hold`/`Move`/`Attack`. A `Lost`-link `InvalidOperationException` from the controller is mapped to the
`LINK_LOST` reason; other failures map to `UNKNOWN_UNIT` / `INVALID_REQUEST` / `MISSING_AGENT_ID` /
`INVALID_ATTACK_TARGET` / `CONTROLLER_ERROR`. This is a pure Delegation surface — it does not modify Sim
or Projection swarm files. A separate read-model projection
([`SwarmUnitPanelProjection`](../../src/ProjectAegis.Delegation/Projection/SwarmUnitPanelProjection.cs),
`SwarmMapSymbolProjection`, `SwarmIntegrityReadout`) surfaces swarm state to the C2 layer — see
[c2-projection-layer.md](c2-projection-layer.md).

---

## Replay & determinism — `SwarmReplayHarness`

`SwarmReplayHarness` (SWARM-24) is the deterministic golden runner, **isolated from the Baltic v2
replay golden** (it does not mutate the `ReplayGoldenRegressionCatalog`):

- **`RunGoldenScenario(seed=42)`** runs a canonical Hold → Move → Attack sequence plus point-fire and
  area-AA integrity hits, then captures a `SwarmReplayResult` with the order-log fingerprint
  (`LayerCore`), the integrity-timeline hash (`LayerCombatOutcome`, folding both the damage rows and
  the living end-state), and a human-readable `CanonicalFingerprint` string.
- **`Replay(orders, integrityTimeline, …)`** reconstructs onto a fresh controller via the static
  `ReplayOrders` / `ReplayIntegrityTimeline` helpers (regen rows are those with `New > Previous`) and
  re-captures — same seed ⇒ byte-stable canonical fingerprint.
- **`RunDesignMaxStress(seed=7, concurrentSwarms=16, ticks=60)`** proves the SWARM-25 cost model:
  applied integrity ops stay bounded by `concurrentSwarms × ticks` regardless of the (much larger)
  logical drone total.

Two hashes stay separate: the **order-log** fingerprint (`LayerCore`) and the **integrity-timeline**
hash (`LayerCombatOutcome`). Both use an FNV-1a string mix folded through `SimWorldHash`.

---

## Determinism & tests

- **Pure & deterministic.** Ordinal iteration, append-only sequence-numbered logs, seeded pure
  planners; no RNG or wall-clock in the ordering paths.
- **Independence.** Swarm C2 link ≠ CEC mesh (no cross-imports); swarm hashes never touch the Baltic
  v2 order-log golden.
- **Aggregate.** No per-drone bodies; combat is O(swarm units) per pulse.

| Suite | Location | Test methods |
|-------|----------|--------------|
| `SwarmControllerTests` | [`src/ProjectAegis.Sim.Tests/Swarm/SwarmControllerTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmControllerTests.cs) | 7 |
| `SwarmModeHostLinkTests` | [`src/ProjectAegis.Sim.Tests/Swarm/SwarmModeHostLinkTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmModeHostLinkTests.cs) | 7 |
| `SwarmRegenTests` | [`src/ProjectAegis.Sim.Tests/Swarm/SwarmRegenTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmRegenTests.cs) | 9 |
| `SwarmReplayAndCapsTests` | [`src/ProjectAegis.Sim.Tests/Swarm/SwarmReplayAndCapsTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmReplayAndCapsTests.cs) | 6 |
| `SwarmPressureTests` (S117) | [`src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs) | 9 |
| `SwarmAssaultSplitTests` | [`src/ProjectAegis.Sim.Tests/Swarm/Assault/SwarmAssaultSplitTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Assault/SwarmAssaultSplitTests.cs) | 16 |
| `SwarmExpendTests` | [`src/ProjectAegis.Sim.Tests/Swarm/Expend/SwarmExpendTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Expend/SwarmExpendTests.cs) | 7 |
| `SwarmFormationTests` | [`src/ProjectAegis.Sim.Tests/Swarm/Formation/SwarmFormationTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/Formation/SwarmFormationTests.cs) | 11 |
| `SwarmSoftKillTests` | [`src/ProjectAegis.Sim.Tests/Swarm/SoftKill/SwarmSoftKillTests.cs`](../../src/ProjectAegis.Sim.Tests/Swarm/SoftKill/SwarmSoftKillTests.cs) | 14 |
| `SwarmEngageHotpathTests` / `SwarmOffensiveEffectTests` | [`src/ProjectAegis.Sim.Tests/Engage/`](../../src/ProjectAegis.Sim.Tests/Engage/) | 3 / 6 |
| `SwarmAgentIntentIssuerTests` | [`src/ProjectAegis.Delegation.Tests/Sim/SwarmAgentIntentIssuerTests.cs`](../../src/ProjectAegis.Delegation.Tests/Sim/SwarmAgentIntentIssuerTests.cs) | 9 |

> Counts are declared test-method counts (`[Fact]`/`[Theory]`/`[Test]`); data-driven theories expand
> to more executed cases at run time.

Related: [cec-mesh-runtime.md](cec-mesh-runtime.md) (the CEC companion — separate mesh axis) ·
[engagement-pipeline.md](engagement-pipeline.md) (the tick-8 resolver the AA hard-counter feeds) ·
[c2-projection-layer.md](c2-projection-layer.md) (the read-model panels that surface swarm state) ·
[determinism-and-replay.md](determinism-and-replay.md) (the world-hash / golden discipline the swarm
harness stays isolated from).
