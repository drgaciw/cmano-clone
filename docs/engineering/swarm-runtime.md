# Swarm runtime — developer guide

Project Aegis models UAS (drone) swarms as a **single aggregate unit with an integrity count**, not
as per-drone physics. A swarm platform has a living `DroneCount` out of a `MaxDrones` ceiling; every
behaviour — motion, engagement losses, regeneration, soft-kill, expend/kamikaze — is expressed as
authorized changes to that aggregate state-of-truth (SoT). This is the deliberate **Phase A**
design (`SWARM-07`): combat and detection cost scale with the number of swarm *units*, never with
the logical drone total.

The runtime lives entirely in the engine-agnostic
[`ProjectAegis.Sim/Swarm/`](../../src/ProjectAegis.Sim/Swarm/) package, with the offensive/defensive
kill-chain helpers under [`ProjectAegis.Sim/Engage/`](../../src/ProjectAegis.Sim/Engage/) and the
contact-classification helper under [`ProjectAegis.Sim/Sensors/`](../../src/ProjectAegis.Sim/Sensors/).
It is **self-contained and deterministic**: `SwarmController` holds no RNG, uses `SimSeed` +
`SimWorldHash` for hashing, and ships its own golden replay path
([`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs)) that is **isolated
from the Baltic v2 order-log golden 6/6** — swarm work never touches the production replay hash.

- **Source:** [`src/ProjectAegis.Sim/Swarm/`](../../src/ProjectAegis.Sim/Swarm/) (aggregate SoT,
  intents, modes, formations, integrity, link, soft-kill, assault split, expend, replay) plus the
  engage/sensor helpers below.
- **Data model:** the Data-side unit record and catalog defaults live in
  [`src/ProjectAegis.Data/Catalog/`](../../src/ProjectAegis.Data/Catalog/)
  (`SwarmUnitIntegrity`, `CatalogSwarmPlatform`, `SwarmUnitFactory`, `SwarmTier`).
- **Related:** the aggregate SoT / order-log discipline follows **ADR-010**; the general
  determinism rules are in [determinism-and-replay.md](determinism-and-replay.md); the general
  kill-chain gates are in [engagement-pipeline.md](engagement-pipeline.md); the C2 read-models that
  surface swarm state are covered by [c2-projection-layer.md](c2-projection-layer.md). This page
  documents what the swarm runtime actually **does** and how to extend it without breaking its
  golden.

> **Phase A scope.** Kinematics are placeholder aggregate motion in degrees of lat/lon
> (`SwarmController.DefaultSpeedDegPerSecond = 0.05`), there is no per-drone selection or physics
> (`SWARM-07`), and the swarm order log is sim-local — the Delegation `IOrderLog` bridge is tracked
> under `DRG-91`. Story tags (`SWARM-##` / `DRG-##`) referenced below are the source-of-record for
> each behaviour and appear in the corresponding file's doc comment.

---

## Where it lives

### Aggregate controller & state

| File | Role |
|------|------|
| [`SwarmController.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) | The sealed aggregate controller — register/motion/intents/modes/formations, authorized integrity damage & regen, host bind + link, expend, and the two hashes. Integrity is **not** writable except through its authorized methods. |
| [`SwarmIntentKind.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmIntentKind.cs) | Phase A aggregate intents: `Hold(0)` / `Move(1)` / `Attack(2)`. |
| [`SwarmOperationalMode.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOperationalMode.cs) | Phase B operational modes (distinct from intents): `Hold(0)` / `Assault(1)` / `Screen(2)` / `Scatter(3)` / `Rejoin(4)`. |
| [`SwarmLinkState.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkState.cs) | C2/order channel health: `Connected(0)` / `Degraded(1)` / `Lost(2)`. Independent of the CEC mesh. |
| [`SwarmLinkEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs) | Pure link rules from host range/liveness/jam (`RangeDeg`, `Evaluate`). |
| [`SwarmRegenEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs) | Pure regen gate `CanRegen(range, hostAlive, hostHasStores, count, max, maxRange)`. |
| [`SwarmPerformanceCaps.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) | Logical-vs-render caps and the aggregate work-unit model (`SWARM-25`). |
| [`SwarmIntegrityChange.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmIntegrityChange.cs) | One authorized integrity delta row (loss **or** regen). |

### Order logs (append-only)

| File | Role |
|------|------|
| [`SwarmOrderLog.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLog.cs) / [`SwarmOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOrderLogEntry.cs) | Intent log (Hold/Move/Attack) + `ComputeFingerprint()` (`LayerCore`). |
| [`SwarmModeOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmModeOrderLogEntry.cs) | Logged operational-mode change. |
| [`Formation/SwarmFormationOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormationOrderLogEntry.cs) | Logged formation change. |
| [`Expend/SwarmExpendOrderLogEntry.cs`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendOrderLogEntry.cs) | Logged expend/kamikaze pulse. |

### Formations, assault split, expend, soft-kill

| File | Role |
|------|------|
| [`Formation/SwarmFormation.cs`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormation.cs) / [`SwarmFormationLayout.cs`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormationLayout.cs) | `Cloud/Wall/Spear/Orbit` soft layouts — **cosmetic** member offsets, not engagement SoT. |
| [`Assault/SwarmAssaultAxisSplitter.cs`](../../src/ProjectAegis.Sim/Swarm/Assault/SwarmAssaultAxisSplitter.cs) | Pure multi-axis auto-split planner (`SWARM-17`); returns `SwarmAssaultSplitPlan` + `SwarmAssaultAxisAllocation`. |
| [`Expend/SwarmExpendResult.cs`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendResult.cs) | Expend outcome (`Applied` + deny reason). |
| [`SoftKill/SwarmSoftKillApplicator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmSoftKillApplicator.cs) | External EMP/jam applicator over the controller (`SWARM-18`). |
| [`SoftKill/SwarmEmpEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmEmpEvaluator.cs) / [`SwarmJamEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamEvaluator.cs) | Pure EMP-freeze / jam-severity → linkState maps. |

### Engage & sensor helpers (kill chain)

| File | Role |
|------|------|
| [`Engage/SwarmEngagementIntegrityApplier.cs`](../../src/ProjectAegis.Sim/Engage/SwarmEngagementIntegrityApplier.cs) | Applies engagement losses **only** via the controller's authorized damage API (`SWARM-07`). |
| [`Engage/SwarmHardCounterAa.cs`](../../src/ProjectAegis.Sim/Engage/SwarmHardCounterAa.cs) | `PointFire` vs `AreaAa` hard-counter table (drones lost per hit). |
| [`Engage/SwarmOffensiveEffect.cs`](../../src/ProjectAegis.Sim/Engage/SwarmOffensiveEffect.cs) | Scales offensive effect by living integrity fraction. |
| [`Engage/SwarmSalvoDeconfliction.cs`](../../src/ProjectAegis.Sim/Engage/SwarmSalvoDeconfliction.cs) | Deterministic one-shooter-per-target salvo allocation (req 14). |
| [`Engage/ISwarmIntegrityDamageSink.cs`](../../src/ProjectAegis.Sim/Engage/ISwarmIntegrityDamageSink.cs) / [`SwarmControllerIntegritySink.cs`](../../src/ProjectAegis.Sim/Engage/SwarmControllerIntegritySink.cs) | Production seam: engagement outcome → aggregate integrity. |
| [`Sensors/SwarmContactClassifier.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClassifier.cs) | Pure `SingleAirframe / PossibleSwarm / UasSwarmCloud / Unknown` classification by sensor quality (`SWARM-26`). |

### Replay & data model

| File | Role |
|------|------|
| [`SwarmReplayHarness.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) | Golden scenario runner + reconstruct path (`SWARM-24`); **isolated** from the Baltic golden. |
| [`SwarmUnitIntegrity.cs`](../../src/ProjectAegis.Data/Catalog/SwarmUnitIntegrity.cs) | Data-side `(UnitId, PlatformId, DroneCount, MaxDrones)` record; `IsDestroyed` at 0. |
| [`CatalogSwarmPlatform.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSwarmPlatform.cs) | Catalog defaults (`GenericSwarmPlatformId = "uas-swarm-generic"`, `GenericMaxDrones = 40`). |

---

## 1. Registration & the aggregate SoT

A swarm enters the runtime via `SwarmController.Register(SwarmUnitIntegrity integrity, latDeg, lonDeg)`:

- `MaxDrones` is clamped to `SwarmPerformanceCaps.LogicalMaxDronesPerSwarm` (**40**) and the living
  `DroneCount` is clamped into `[0, max]` (`SwarmPerformanceCaps.ClampDroneCount`). A registered unit
  starts `Intent = Hold`, `Mode = Hold`, `Formation = Cloud`, `LinkState = Connected`.
- The only writable integrity paths are `TryApplyIntegrityDamage` (loss) and
  `TryApplyIntegrityRegen` (gain). Both emit a `SwarmIntegrityChange` with a monotonic
  `SequenceId`, the pre/post counts, `DronesLost`, and a reason code, and both **clamp** (loss to 0,
  gain to `MaxDrones`). There is no public field mutation — this is the load-bearing "authorized
  API only" invariant (`SWARM-02` / `SWARM-07`, pinned by
  `Integrity_updates_only_via_authorized_damage_api`).
- A unit with `DroneCount <= 0` is destroyed: it is skipped by `Tick` and rejects further damage
  (`Integrity_clamps_at_zero_and_marks_destroyed`, `Destroyed_swarm_does_not_advance_centroid`).

## 2. Intents & aggregate motion (`SWARM-03`)

`IssueHold` / `IssueMove(targetLat, targetLon)` / `IssueAttack(targetUnitId, …)` set the aggregate
intent and append an `SwarmOrderLogEntry`. `Move`/`Attack` record a centroid waypoint; `Hold` clears
it. `Tick(deltaSeconds)` then advances the centroid toward the waypoint by
`SpeedDegPerSecond × deltaSeconds` (straight-line interpolation, snapping when the step overshoots);
`Hold` is stationary. Units are iterated in `Ordinal` id order for determinism.

**`Screen` mode overrides intent motion:** if a unit is in `Screen` mode with a bound, *alive* host
of known geometry, `Tick` gravitates its centroid toward the host instead of the waypoint
(`Screen_mode_gravitates_toward_host`).

All order-issuing methods first call `EnsureOrdersAccepted`, which **throws** when
`LinkState == Lost` — a comms-lost swarm cannot accept new orders (`SWARM-12`,
`Lost_link_blocks_new_orders`).

## 3. Host binding & link state (`SWARM-11` / `SWARM-12`)

`BindHost(unitId, hostId)` associates a mothership; `PublishHostState(hostId, lat, lon, alive)`
publishes its geometry/liveness. `RefreshLinkState(unitId, jammed)` recomputes link health via the
pure `SwarmLinkEvaluator`:

| Condition | Result |
|-----------|--------|
| host dead, or jammed | `Lost` |
| range `≥ DefaultLostRangeDeg` (**2.0°**) | `Lost` |
| range `≥ DefaultDegradedRangeDeg` (**1.0°**) | `Degraded` |
| range `< 1.0°` | `Connected` |
| no host geometry at all | `Connected` (free-flying, unbound) |
| host bound but geometry unknown | `Degraded` (or `Lost` if jammed) |

`SetLinkState` is the explicit override (tests / external comms timeline). `NotifyHostLost` is the
host-death stub: it forces `LinkState = Lost`, `Mode = Hold`, `Intent = Hold`, clears the waypoint,
and marks the host not-alive.

## 4. Integrity regeneration near host (`SWARM-13`)

`TryRegenNearHost(unitId, hostHasStores, …)` restores drones when the pure
`SwarmRegenEvaluator.CanRegen` gate passes — host **alive**, host **has stores**, swarm within
`DefaultMaxRangeDeg` (**0.5°**) of the host, and room remains under `MaxDrones`. It restores
`DefaultDronesPerPulse` (**1**) drones per pulse with reason `regen-host`, and **fails closed** (no
mutation, no throw) when any gate fails. Regen rows are the timeline entries where
`NewDroneCount > PreviousDroneCount` (`DronesLost = 0`). Pinned by the `SwarmRegenTests` suite.

## 5. Operational modes & formations (`SWARM-10` / `SWARM-16`)

`IssueMode` and `IssueSetFormation` change the Phase B mode / soft formation and log a dedicated
entry each. Formations (`Cloud/Wall/Spear/Orbit`) are **cosmetic** — `SwarmFormationLayout.ComputeOffsets`
produces deterministic per-member `(dx, dy)` degree offsets from the centroid (Cloud = seeded uniform
disk; Wall/Spear = lines perpendicular/along host bearing; Orbit = ring with a soft host bias). These
offsets are presentation-only and are **not** part of the engagement/integrity SoT.

## 6. Assault multi-axis auto-split (`SWARM-17`)

`SwarmAssaultAxisSplitter.Plan(droneCount, axisCount, mode, seed, doctrineAllowSplit, targetBearingDeg)`
is a pure planner. A split is applied **only** when `mode == Assault`, `doctrineAllowSplit` is true,
and `axisCount ≥ 2`; otherwise it returns a single-axis plan. The effective axis count is reduced to
`min(axisCount, droneCount)` so every axis gets `≥ 1` drone, and the per-axis shares are floor
division plus a remainder distributed in a **seed-deterministic** Fisher–Yates order — so shares
**sum exactly to the living drone count** while the split varies with seed. Approach bearings fan
around the base bearing at `DefaultAxisSpreadDeg` (**30°**). Pinned by `SwarmAssaultSplitTests` and
`Assault_split_under_attrition_shares_sum_to_living_count`.

## 7. Expend / kamikaze pulse (`SWARM-19`)

`IssueExpend(unitId, dronesToExpend, expendAuthorized, …)` irreversibly spends
`min(dronesToExpend, DroneCount)` drones via the authorized damage path (reason `expend-pulse`) and
logs a `SwarmExpendOrderLogEntry`. Authorization is the caller's responsibility — the doctrine/WRA
`expendAuthorized` flag is passed in; the controller deliberately does **not** call the
`PolicyEvaluator` itself (surface discipline). Deny reasons are explicit: `expend-unauthorized`,
`expend-count-invalid`, `expend-no-drones`, `expend-integrity-failed`.

## 8. Soft-kill: EMP & jam (`SWARM-18` / `DRG-107`)

`SwarmSoftKillApplicator` wraps a controller and applies **external** soft-kill without rewriting its
internals; every action appends a `SwarmSoftKillEvent` with an explicit reason string.

- **EMP** (`ApplyEmp`): freezes mode switches until `simTime + freezeDuration`
  (`DefaultFreezeDurationSeconds = 30`); overlapping EMPs merge by taking the later horizon
  (deterministic max). It optionally recommends `Scatter` at onset — but only when orders are
  accepted (link not `Lost`) and the unit isn't already scattering. `TryIssueMode` rejects mode
  changes while frozen (`Emp_freezes_mode_switches_for_duration`).
- **Jam** (`ApplyJam`): maps `SwarmJamSeverity` (`None/Degraded/Lost`) to the C2 `SwarmLinkState`
  via `SetLinkState`; `ClearJam` restores `Connected` (or re-evaluates geometry). Jam touches the
  **C2 channel only** — never the integrity count or the CEC mesh.

Soft-kill and attrition compose without any integrity side-channel
(`Softkill_emp_and_attrition_compose_without_integrity_side_channel`).

## 9. Kill chain: hard-counter AA & offensive scaling

Engagement outcomes against a swarm route through `SwarmEngagementIntegrityApplier`, which applies
losses **only** via `SwarmController.TryApplyIntegrityDamage` (directly, or through
`ISwarmIntegrityDamageSink` / `SwarmControllerIntegritySink` in production). The hard-counter table
(`SwarmHardCounterAa`) makes **area** fire shred far more of the cloud than point fire per hit:

| Profile | Drones lost per hit (default) | Reason code |
|---------|-------------------------------|-------------|
| `PointFire` | `PointFireDronesLostPerHit` = **1** | `swarm-aa-point` |
| `AreaAa` (flak / CIWS) | `AreaAaDronesLostPerHit` = **8** | `swarm-aa-area` |

Per-scenario overrides ride on `EngageContext` (`0` = table default). Conversely,
`SwarmOffensiveEffect.Scale` scales a swarm's *offensive* effect by its living integrity fraction
(`droneCount / maxDrones`, clamped `[0, 1]`, linear by default), so an attritted swarm hits softer.
`SwarmSalvoDeconfliction.Allocate` deconflicts salvos to **one shooter per target** by sorted
`(shooterId, targetId, weaponId)` (req 14).

## 10. Detection classification (`SWARM-26`)

`SwarmContactClassifier.Classify(targetIsSwarmPlatform, sensorQuality, estimatedCountHint, highResolutionMode)`
is a pure classifier returning `Unknown / SingleAirframe / PossibleSwarm / UasSwarmCloud` plus a
confidence and a reason tag. **Ground truth alone is not enough** — below `LowQualityCeiling` (0.25)
even a real swarm stays `Unknown` (a modelled misclassification path); mid quality (`< 0.5`) tops
out at `PossibleSwarm`; high quality resolves `UasSwarmCloud` from the truth flag or a multi-return
count over the threshold (lowered in high-resolution mode).

---

## Determinism & the swarm golden

`SwarmController` is deterministic by construction: it holds **no RNG**, iterates units in `Ordinal`
id order, and all sequence ids are monotonic. The seeded mixing inside `SwarmFormationLayout` (Cloud
disk) and `SwarmAssaultAxisSplitter` (remainder order) is pure and reproducible for the same inputs.

Two hashes summarize a run:

- **`ComputeOrderLogFingerprint()`** folds the intent log with `SimWorldHash.MixLayer(…, LayerCore)`.
- **`ComputeIntegrityTimelineHash()`** folds the seed, every `SwarmIntegrityChange`, and the current
  living/max counts with `SimWorldHash.MixLayer(…, LayerCombatOutcome)` — so the end-state
  participates even when no damage occurred. A `Lost` link (which blocks orders) does **not** by
  itself perturb the integrity hash (`Link_lost_does_not_mutate_integrity_hash`).

[`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) exercises this:
`RunGoldenScenario(seed = 42)` runs a Hold → Move → Attack sequence plus mixed point-fire/area-AA
integrity hits and emits a byte-stable `CanonicalFingerprint` string
(`Golden_canonical_fingerprint_is_pinned`). `Replay(orders, integrityTimeline, …)` reconstructs the
same end-state on a fresh controller via `SwarmController.ReplayOrders` +
`ReplayIntegrityTimeline` (`Integrity_replay_via_controller_api_matches_live_end_state`).

> **Isolation invariant.** The swarm golden is intentionally **separate** from the Baltic v2 replay
> golden set (6/6) and does not mutate `ReplayGoldenRegressionCatalog`. Swarm changes must keep their
> own canonical fingerprint stable but never move the production hash `17144800277401907079`.

### Performance model (`SWARM-25`)

`SwarmPerformanceCaps` splits **logical** integrity from **render** LOD so combat cost never grows
with drone totals:

| Constant | Value | Meaning |
|----------|-------|---------|
| `LogicalMaxDronesPerSwarm` | **40** | integrity ceiling per swarm unit (combat SoT) |
| `RenderMaxMembersPerSwarm` | **12** | cosmetic sprites only — never iterated as authority |
| `DesignMaxConcurrentSwarms` | **16** | design-max first-class swarms in a scenario |
| `DesignMaxLogicalDrones` | **640** | `16 × 40` |
| `StressScenarioTicks` / `StressPulseBudgetMs` | **60** / **2000** | stress fixture bounds |

`EngagementWorkUnitsPerPulse(n) = n` — one aggregate op per swarm unit per pulse, independent of
logical drones. `RunDesignMaxStress` proves the loop is `O(swarms × ticks)`, not `O(drones)`
(`Design_max_stress_is_O_swarms_not_O_drones_and_meets_pulse_budget`).

---

## Extending the runtime without breaking the golden

1. **New integrity source (new AA profile, hazard, effect).** Add a reason code and route it through
   `TryApplyIntegrityDamage` / `TryApplyIntegrityRegen` — never mutate `DroneCount` directly. The
   change auto-appears on the integrity timeline and hash.
2. **New intent / mode / formation.** Extend the enum + a logged `Issue*` method, and mirror the new
   order-log row in the fingerprint if it must be replay-load-bearing. Update `ReplayOrders`'
   `switch` so reconstruction stays exhaustive.
3. **New soft-kill / doctrine gate.** Prefer a pure evaluator (like `SwarmEmpEvaluator` /
   `SwarmRegenEvaluator`) plus a thin applicator method; keep RNG out of the controller.
4. **Regenerate the golden deliberately.** If a change legitimately alters the swarm canonical
   fingerprint, update the pinned expectation in `SwarmReplayAndCapsTests` and call it out — do
   **not** touch the Baltic v2 goldens.

Verification for any swarm change (from repo root):

```bash
dotnet build ProjectAegis.sln
dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj --filter FullyQualifiedName~Swarm
```

### Pinned by

| Area | Tests |
|------|-------|
| Controller / SoT | [`SwarmControllerTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmControllerTests.cs) |
| Replay & caps | [`SwarmReplayAndCapsTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmReplayAndCapsTests.cs) |
| Regen | [`SwarmRegenTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmRegenTests.cs) |
| Modes / host / link | [`SwarmModeHostLinkTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmModeHostLinkTests.cs) |
| Pressure / attrition | [`SwarmPressureTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SwarmPressureTests.cs) |
| Assault / expend / soft-kill / formation | [`Assault/SwarmAssaultSplitTests`](../../src/ProjectAegis.Sim.Tests/Swarm/Assault/SwarmAssaultSplitTests.cs) · [`Expend/SwarmExpendTests`](../../src/ProjectAegis.Sim.Tests/Swarm/Expend/SwarmExpendTests.cs) · [`SoftKill/SwarmSoftKillTests`](../../src/ProjectAegis.Sim.Tests/Swarm/SoftKill/SwarmSoftKillTests.cs) · [`Formation/SwarmFormationTests`](../../src/ProjectAegis.Sim.Tests/Swarm/Formation/SwarmFormationTests.cs) |
| Engage / sensors | [`Engage/SwarmOffensiveEffectTests`](../../src/ProjectAegis.Sim.Tests/Engage/SwarmOffensiveEffectTests.cs) · [`Engage/SwarmSalvoDeconflictionTests`](../../src/ProjectAegis.Sim.Tests/Engage/SwarmSalvoDeconflictionTests.cs) · [`Sensors/SwarmContactClassifierTests`](../../src/ProjectAegis.Sim.Tests/Sensors/SwarmContactClassifierTests.cs) |
