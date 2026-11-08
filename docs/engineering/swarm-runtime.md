# Drone / UAS swarm runtime — developer guide

Project Aegis models drone/UAS swarms as **first-class aggregate platforms**, not clouds of
per-drone bodies. A swarm unit carries one integrity number — its **living drone count** — and every
system (motion, sensing, engagement, damage, presentation) treats that aggregate as the source of
truth (SoT). This keeps the sim deterministic and cheap under design-max load while still giving the
player swarm-flavoured behaviour: formations, operational modes, soft-kill, kamikaze pulses,
host-tethered regeneration, and cooperative-engagement (CEC) fire control.

This is the engineering companion to requirement
[`22-Drone-Swarm-Platforms.md`](../../Game-Requirements/requirements/22-Drone-Swarm-Platforms.md)
(doc 22, FR-20). Behaviour was landed in waves — **Phase A** (aggregate model + Move/Attack/Hold
intents + integrity + hard-counter AA), **Phase B** (operational modes, host bind, C2 link state,
CEC mesh, agent intent issuance, catalog Phase-B fields), and the **SWARM-C** wave (formations,
multi-axis assault split, EMP/jam soft-kill, expend/kamikaze, scenario mission types). Story tags
`SWARM-nn` / `DRG-nn` in the source map back to that wave plan.

> **Aggregate SoT — the load-bearing invariant.** Integrity is only ever written through the
> authorized `SwarmController` methods (`TryApplyIntegrityDamage` / `TryApplyIntegrityRegen` and the
> `Issue*` order/mode/expend paths). There is **no** public field mutation and **no** per-drone
> physics. Engagement and detection work is `O(swarm units)` per pulse, never `O(logical drones)`
> ([`SwarmPerformanceCaps.EngagementWorkUnitsPerPulse`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs)).

> **Replay isolation.** The swarm runtime has its **own** golden
> ([`SwarmReplayHarness`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs)) that is explicitly
> separate from the Baltic v2 order-log golden set (6/6). Baltic scenarios contain no swarm units, so
> the production replay hash `17144800277401907079` is untouched by everything on this page.

---

## Where it lives

Swarm code spans four assemblies. The engine-agnostic runtime is in `ProjectAegis.Sim`; the
delegation seam and C2 read-models are in `ProjectAegis.Delegation`; the catalog/scenario model is in
`ProjectAegis.Data`.

### Sim — the runtime SoT

| File | Role |
|------|------|
| [`Swarm/SwarmController.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) | The aggregate controller: register units, issue Hold/Move/Attack intents, set mode/formation, apply authorized integrity damage/regen, advance centroid on `Tick`, and replay orders + integrity timeline. |
| [`Swarm/SwarmIntentKind.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmIntentKind.cs) | Phase A intents: `Hold`, `Move`, `Attack` (aggregate — no per-drone selection). |
| [`Swarm/SwarmOperationalMode.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmOperationalMode.cs) | Phase B modes: `Hold`, `Assault`, `Screen`, `Scatter`, `Rejoin` (distinct from intents). |
| [`Swarm/SwarmLinkState.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkState.cs) + [`SwarmLinkEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs) | C2/order-channel health (`Connected`/`Degraded`/`Lost`) and the pure host-range/jam rules. **Not** CEC mesh. |
| [`Swarm/Formation/`](../../src/ProjectAegis.Sim/Swarm/Formation/) | `SwarmFormation` (`Cloud`/`Wall`/`Spear`/`Orbit`) + `SwarmFormationLayout` deterministic soft member offsets (cosmetic, not engagement SoT). |
| [`Swarm/Assault/`](../../src/ProjectAegis.Sim/Swarm/Assault/) | `SwarmAssaultAxisSplitter` multi-axis auto-split planner + `SwarmAssaultSplitPlan` / `SwarmAssaultAxisAllocation`. |
| [`Swarm/SoftKill/`](../../src/ProjectAegis.Sim/Swarm/SoftKill/) | `SwarmSoftKillApplicator` + `SwarmEmpEvaluator` (mode freeze) + `SwarmJamEvaluator` (link degrade/lost) + `SwarmJamSeverity` / `SwarmSoftKillKind` / `SwarmSoftKillEvent`. |
| [`Swarm/Expend/`](../../src/ProjectAegis.Sim/Swarm/Expend/) | `SwarmExpendResult` (+ the `IssueExpend` path on the controller): authorized irreversible kamikaze pulse. |
| [`Swarm/SwarmRegenEvaluator.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs) | Pure gates for host-tethered drone regeneration. |
| [`Swarm/SwarmPerformanceCaps.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) | Logical vs render caps + aggregate work-unit accounting (SWARM-25). |
| [`Swarm/SwarmReplayHarness.cs`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs) | Golden scenario runner + reconstruct path + design-max stress runner. |
| [`Sensors/SwarmContactClassifier.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClassifier.cs) + [`SwarmContactClass.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClass.cs) | Observer-facing single-airframe vs UAS-cloud classification (misclassifies at low quality). |
| [`Sensors/SwarmSensorScale.cs`](../../src/ProjectAegis.Sim/Sensors/SwarmSensorScale.cs) | Scales detection `Pd` by living integrity. |
| [`Engage/SwarmHardCounterAa.cs`](../../src/ProjectAegis.Sim/Engage/SwarmHardCounterAa.cs) | Point-fire vs area-AA drones-lost-per-hit table (hard counter). |
| [`Engage/SwarmEngagementIntegrityApplier.cs`](../../src/ProjectAegis.Sim/Engage/SwarmEngagementIntegrityApplier.cs) + [`ISwarmIntegrityDamageSink.cs`](../../src/ProjectAegis.Sim/Engage/ISwarmIntegrityDamageSink.cs) / [`SwarmControllerIntegritySink.cs`](../../src/ProjectAegis.Sim/Engage/SwarmControllerIntegritySink.cs) | Applies engagement outcomes through the authorized integrity path only. |
| [`Engage/SwarmOffensiveEffect.cs`](../../src/ProjectAegis.Sim/Engage/SwarmOffensiveEffect.cs) | Scales offensive effect (damage/salvo weight/Pk) by living integrity. |
| [`Engage/SwarmSalvoDeconfliction.cs`](../../src/ProjectAegis.Sim/Engage/SwarmSalvoDeconfliction.cs) | Deterministic first-claimant one-shooter-per-target allocation (req 14). |
| [`Cec/`](../../src/ProjectAegis.Sim/Cec/) | `CecMeshController` + `CecMeshEvaluator` (mesh membership) + `CecCompositeTrack` composite picture; [`Engage/CecRemoteEngageGate.cs`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs) engage-on-remote-data. |
| [`Policy/PolicyEvaluator.cs`](../../src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs) + [`EffectivePolicy.cs`](../../src/ProjectAegis.Sim/Policy/EffectivePolicy.cs) | SWARM-15 auto-engage / expend authorization gates layered onto ROE + WRA. |

### Delegation — agent seam + C2 read-models

| File | Role |
|------|------|
| [`Sim/SwarmAgentIntentIssuer.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentIntentIssuer.cs) | Agents issue the same intents/modes as the player through `SwarmController`, with actor attribution + machine-readable reject reasons. |
| [`Sim/SwarmOrderActor.cs`](../../src/ProjectAegis.Delegation/Sim/SwarmOrderActor.cs) / `SwarmAgentOrderRequest.cs` / `SwarmAgentOrderResult.cs` / `SwarmAgentOrderLogPayload.cs` | The request/result/attribution DTOs. |
| [`Projection/SwarmUnitPanelProjection.cs`](../../src/ProjectAegis.Delegation/Projection/SwarmUnitPanelProjection.cs) + `SwarmPanelSnapshot.cs` / `SwarmIntegrityReadout.cs` / `SwarmMapSymbolProjection.cs` | Pure C2 panel/map read-models (integrity line, mode/host/link/CEC fields, aggregate density label). |

### Data — catalog + scenario authoring

| File | Role |
|------|------|
| [`Catalog/CatalogSwarmPlatform.cs`](../../src/ProjectAegis.Data/Catalog/CatalogSwarmPlatform.cs) | The catalog row: `MaxDrones`, `IsSwarm`, Phase-B `DefaultMode` / `RequiresHost` / `AllowedHostClasses` / `CecCapable`, plus `CatalogSwarmPlatformDefaults` (generic + USN-CEC exemplars, valid mode set). |
| [`Catalog/SwarmUnitIntegrity.cs`](../../src/ProjectAegis.Data/Catalog/SwarmUnitIntegrity.cs) + [`SwarmUnitFactory.cs`](../../src/ProjectAegis.Data/Catalog/SwarmUnitFactory.cs) | The spawned aggregate integrity record + its factory from a catalog row. |
| [`Catalog/SwarmTier.cs`](../../src/ProjectAegis.Data/Catalog/SwarmTier.cs) | req-09 near-future entity **caps** (`Micro`/`Medium`/`Mass` → 50/500/5000). Distinct from the Phase A logical integrity cap. |
| [`Scenario/Authoring/SwarmMissionType.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/SwarmMissionType.cs) | `Patrol`/`Support`/`Strike` mission types → default operational mode. |
| [`Scenario/Authoring/SwarmScenarioValidation.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/SwarmScenarioValidation.cs) | Placement/configuration validation for swarm ORBAT units. |

**Related:** the surrounding tick pipeline is in [agent-decision-pipeline.md](agent-decision-pipeline.md);
the detection roll swarms scale is in [detection-pipeline.md](detection-pipeline.md); the kill-chain
gate chain swarms plug into is in [engagement-pipeline.md](engagement-pipeline.md); ROE/WRA gating is
in [autonomy-roe-gating.md](autonomy-roe-gating.md); the gauntlet swarm stress axes are in
[qa-gauntlet.md](qa-gauntlet.md).

---

## The aggregate controller

[`SwarmController`](../../src/ProjectAegis.Sim/Swarm/SwarmController.cs) owns every swarm unit's
runtime state. It is constructed with a `SimSeed` and an optional centroid speed
(`DefaultSpeedDegPerSecond = 0.05` deg lat/lon per sim-second — Phase A placeholder kinematics).

### Registration and integrity

`Register(SwarmUnitIntegrity, latDeg, lonDeg)` adds a unit. Both `MaxDrones` and the living
`DroneCount` are clamped to the Phase A logical ceiling
(`SwarmPerformanceCaps.LogicalMaxDronesPerSwarm = 40`). Each unit starts `Intent = Hold`,
`Mode = Hold`, `Formation = Cloud`, `LinkState = Connected`.

Integrity is mutated only through:

| Method | Effect |
|--------|--------|
| `TryApplyIntegrityDamage(unit, dronesLost, tick, time, reason, out change)` | Subtracts (clamped to living count), appends a `SwarmIntegrityChange` row. Returns false for dead/missing units or non-positive loss. |
| `TryApplyIntegrityRegen(unit, dronesGained, tick, time, reason, out change)` | Adds up to `MaxDrones` (clamped by remaining room), logs a change with `DronesLost = 0` and `New > Previous`. |
| `IssueExpend(...)` | Kamikaze spend (see below) — routes through `TryApplyIntegrityDamage`. |

Every change lands in the `IntegrityTimeline` with a monotonic sequence id, and
`ComputeIntegrityTimelineHash()` folds the timeline **and** current living counts into a deterministic
`SimWorldHash` mix so end-state participates even when no damage occurred.

### Intents and kinematics

`IssueHold` / `IssueMove(targetLat, targetLon)` / `IssueAttack(targetUnitId, [lat, lon])` set intent
and append to the `OrderLog`. `Tick(deltaSeconds)` advances the centroid for `Move`/`Attack` units
with a waypoint (iterating unit ids in ordinal order for determinism); `Hold` is stationary. In
`Screen` mode with a known, alive bound host the centroid gravitates toward the host instead.
Kinematics are aggregate-centroid only.

Orders are **blocked** (`InvalidOperationException`) while `LinkState == Lost` — see
`EnsureOrdersAccepted`. `NotifyHostLost(unit)` forces `Lost` + `Hold` mode/intent and marks the host
dead.

### Replay

`ReplayOrders(target, orders)` and `ReplayIntegrityTimeline(target, changes)` reconstruct a fresh
controller (same registered spawn) by re-issuing through the authorized APIs in sequence order; the
target reassigns its own sequence ids. `ComputeOrderLogFingerprint()` + the integrity hash back the
`SwarmReplayHarness` golden.

---

## Phase B: modes, host bind, and C2 link state

Operational **modes** (`IssueMode`) are a separate axis from intents:

| Mode | Meaning |
|------|---------|
| `Hold` | Loiter / station. |
| `Assault` | Coordinated attack posture; the only mode that permits the multi-axis split. |
| `Screen` | Host escort / defensive screen — centroid gravitates toward the bound host on `Tick`. |
| `Scatter` | Dispersal (the EMP soft-kill recommendation). |
| `Rejoin` | Reform toward the host/parent. |

**Host bind:** `BindHost(unit, hostId)` tethers a swarm to a mothership; `PublishHostState(hostId,
lat, lon, alive)` supplies geometry/liveness used by Screen motion, link evaluation, and regen.

**Link state** ([`SwarmLinkEvaluator`](../../src/ProjectAegis.Sim/Swarm/SwarmLinkEvaluator.cs)) is the
C2/order channel only — **independent of the CEC mesh**. `RefreshLinkState(unit, jammed)` recomputes
from host range + liveness + jam:

- Host dead **or** jammed → `Lost`.
- No host geometry (unbound) → `Connected` (free-flying swarm).
- Host bound but geometry unknown → `Degraded` (or `Lost` if jammed).
- Range `≥ 2.0°` → `Lost`; `≥ 1.0°` → `Degraded`; else `Connected`.

---

## Formations (soft layout)

[`SwarmFormationLayout.ComputeOffsets`](../../src/ProjectAegis.Sim/Swarm/Formation/SwarmFormationLayout.cs)
produces deterministic per-member `(dx, dy)` degree offsets from the centroid. These are **cosmetic**
(presentation LOD only) and never feed engagement/detection SoT:

- **Cloud** — uniform-disk scatter (radius `0.04°`) seeded by unit seed + count.
- **Wall** — line perpendicular to host bearing (east-west when unbound), spacing `0.01°`.
- **Spear** — line along host bearing (due north when unbound).
- **Orbit** — ring (radius `0.03°`) biased `0.35 ×` radius toward the host when a bearing is known.

`IssueSetFormation` records a `SwarmFormationOrderLogEntry`; default formation is `Cloud`.

---

## Multi-axis auto-split assault

[`SwarmAssaultAxisSplitter.Plan`](../../src/ProjectAegis.Sim/Swarm/Assault/SwarmAssaultAxisSplitter.cs)
fans logical mass across `K` approach axes against a single HVT. It is **allocation only** (no
per-drone physics) and applies **only** when `mode == Assault`, `doctrineAllowSplit`, and
`axisCount ≥ 2`; otherwise it returns a single-axis plan (`SplitApplied = false`).

- Effective `K` is reduced to `min(K, droneCount)` so every axis gets `≥ 1` drone.
- Shares are floor-division of `droneCount` with the remainder assigned in a **seed-deterministic**
  axis order (Fisher–Yates over a SplitMix64 stream); shares always sum exactly to `droneCount`.
- Approach bearings fan symmetrically around the target bearing at `30°` spacing.

---

## Soft-kill: EMP and jam

[`SwarmSoftKillApplicator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmSoftKillApplicator.cs) is an
external, deterministic layer that never rewrites controller internals:

- **EMP** (`ApplyEmp`) freezes **mode switches** until `simTime + freezeDuration` (default `30 s`;
  overlapping freezes take the later horizon via `MergeFreezeUntil`). While frozen, `TryIssueMode`
  returns false with reason `soft-kill-emp-mode-frozen`. On onset it optionally recommends `Scatter`
  (only when the link is not `Lost`). Pure freeze math lives in
  [`SwarmEmpEvaluator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmEmpEvaluator.cs).
- **Jam** (`ApplyJam`) maps `SwarmJamSeverity` → C2 `SwarmLinkState` via
  [`SwarmJamEvaluator`](../../src/ProjectAegis.Sim/Swarm/SoftKill/SwarmJamEvaluator.cs)
  (`Degraded`/`Lost`); `None` clears back to `Connected` (or re-evaluates geometry). It touches C2
  link only — **never** the CEC mesh.

Every action appends a reason-tagged `SwarmSoftKillEvent` to an append-only log.

---

## Expend / kamikaze pulse

`SwarmController.IssueExpend(unit, dronesToExpend, expendAuthorized, tick, time, [targetUnitId])`
spends drones irreversibly. It is **fail-closed**: callers must pass `expendAuthorized` from
doctrine/WRA (the policy `ExpendAuthorized` grant), and the controller itself does not call the
policy evaluator (surface discipline). Denials return a reason
(`expend-unauthorized` / `expend-count-invalid` / `expend-no-drones` / `expend-integrity-failed`)
without mutation; success routes through `TryApplyIntegrityDamage` with reason `expend-pulse` and
logs a `SwarmExpendOrderLogEntry`. See
[`SwarmExpendResult`](../../src/ProjectAegis.Sim/Swarm/Expend/SwarmExpendResult.cs).

---

## Host-tethered regeneration

`SwarmController.TryRegenNearHost(unit, hostHasStores, tick, time, out change, [maxRangeDeg],
[dronesPerPulse])` restores drones when **all** gates pass
([`SwarmRegenEvaluator.CanRegen`](../../src/ProjectAegis.Sim/Swarm/SwarmRegenEvaluator.cs)): host
alive, host has stores, swarm within `0.5°` of the host, and room under `MaxDrones`. It fails closed
(returns false, no mutation, no throw) on any gate miss and regenerates `1` drone/pulse by default,
tagged `regen-host`.

---

## Performance caps

[`SwarmPerformanceCaps`](../../src/ProjectAegis.Sim/Swarm/SwarmPerformanceCaps.cs) separates **logical**
integrity from **render** LOD:

| Constant | Value | Meaning |
|----------|-------|---------|
| `LogicalMaxDronesPerSwarm` | 40 | Integrity/combat ceiling per swarm (clamped at register). |
| `RenderMaxMembersPerSwarm` | 12 | Cosmetic sprites — combat/detection must never iterate this as authority. |
| `DesignMaxConcurrentSwarms` | 16 | Design-max concurrent first-class swarms in a Phase A scenario. |
| `DesignMaxLogicalDrones` | 640 | `16 × 40`. |

`EngagementWorkUnitsPerPulse(n)` returns `n` (one work unit per swarm unit, **independent** of logical
drone totals) — the SWARM-25 acceptance that logical size never expands engagement work.

---

## Sensing swarms

- **Contact classification** —
  [`SwarmContactClassifier.Classify`](../../src/ProjectAegis.Sim/Sensors/SwarmContactClassifier.cs) is
  a pure function of ground-truth `isSwarm`, observer `sensorQuality`, an optional multi-return count
  hint, and a high-resolution flag. It returns a `SwarmContactClass`
  (`Unknown`/`SingleAirframe`/`PossibleSwarm`/`UasSwarmCloud`) plus confidence and a reason token.
  **Low quality (`< 0.25`) can misclassify** — the truth flag alone is not enough to resolve a cloud.
- **Detection scale** —
  [`SwarmSensorScale.ScalePd`](../../src/ProjectAegis.Sim/Sensors/SwarmSensorScale.cs) multiplies base
  `Pd` by the living integrity fraction (monotonic non-decreasing in drone count), so a depleted swarm
  is harder to hold.

---

## Engagement integration

Swarms plug into the tick-8 kill chain ([engagement-pipeline.md](engagement-pipeline.md)) without
per-drone bodies:

- **Offensive effect** — [`SwarmOffensiveEffect.Scale`](../../src/ProjectAegis.Sim/Engage/SwarmOffensiveEffect.cs)
  scales damage/salvo-weight/Pk by living integrity fraction (linear by default; documented tuning knob).
- **Hard-counter AA** — [`SwarmHardCounterAa`](../../src/ProjectAegis.Sim/Engage/SwarmHardCounterAa.cs)
  models that **area-AA / flak / CIWS** shreds far more drones per hit than point fire
  (`AreaAaDronesLostPerHit = 8` vs `PointFireDronesLostPerHit = 1`), with per-scenario overrides on
  `EngageContext`. Losses are always applied through
  [`SwarmEngagementIntegrityApplier`](../../src/ProjectAegis.Sim/Engage/SwarmEngagementIntegrityApplier.cs)
  (authorized path only).
- **Salvo deconfliction** — [`SwarmSalvoDeconfliction.Allocate`](../../src/ProjectAegis.Sim/Engage/SwarmSalvoDeconfliction.cs)
  is a deterministic first-claimant allocation (one shooter per target per weapon slot, sorted by
  `(shooter, target, weapon)`), wired into `SimulationSession` before victim resolution.

### Auto-engage / expend authorization (SWARM-15)

[`PolicyEvaluator`](../../src/ProjectAegis.Sim/Policy/PolicyEvaluator.cs) layers two swarm gates on top
of ROE + WRA max-salvo:

| Condition | Abort reason |
|-----------|--------------|
| `IsAutoEngage && !AutoEngageAuthorized` | `AutoEngageDenied` |
| `IsExpend && !ExpendAuthorized` | `ExpendUnauthorized` |

Both grants live on [`EffectivePolicy`](../../src/ProjectAegis.Sim/Policy/EffectivePolicy.cs)
(`AutoEngageAuthorized` defaults true, `ExpendAuthorized` defaults **false** — expend requires an
explicit doctrine grant). These are resolved from scenario policy JSON, not hardcoded — see
[scenario-policy-authoring.md](scenario-policy-authoring.md).

### CEC mesh + remote engage

[`CecMeshController`](../../src/ProjectAegis.Sim/Cec/CecMeshController.cs) maintains **same-side**
cooperative-engagement mesh membership (`InMesh`/`Degraded`/`OutOfMesh`) from pairwise CEC-peer range,
and builds **composite tracks** from organic detections contributed by mesh nodes. A composite track
needs `≥ 2` mesh-connected contributors on a target; **fire-control quality** additionally requires at
least one `InMesh` contributor with sensor quality `≥ 0.6`.

The mesh is **independent of the C2 `SwarmLinkState`** (jam can drop the mesh without implying C2
lost, and vice versa). [`CecRemoteEngageGate`](../../src/ProjectAegis.Sim/Engage/CecRemoteEngageGate.cs)
lets a CEC-capable shooter fire on a remote composite track **only** when organic fire control is
absent and a mesh-quality track (whose primary contributor is not the shooter) is available;
otherwise it returns `CecRemoteTrackUnavailable`. Organic FC is always preferred.

---

## Delegation seam: agents issue swarm intents

[`SwarmAgentIntentIssuer`](../../src/ProjectAegis.Delegation/Sim/SwarmAgentIntentIssuer.cs) lets a
delegated agent issue the **same** intents/modes as a human via the controller's public API, adding
`SwarmOrderActor` (`Player`/`Agent`) attribution and an attribution log. It validates before touching
the controller and surfaces machine-readable rejects:

| Reason | When |
|--------|------|
| `INVALID_REQUEST` | null request / missing unit id / bad move-target |
| `MISSING_AGENT_ID` | `Actor == Agent` with no agent id |
| `UNKNOWN_UNIT` | unit not registered |
| `LINK_LOST` | controller blocked the order (`LinkState == Lost`) |
| `INVALID_ATTACK_TARGET` | attack with no target unit id |
| `CONTROLLER_ERROR` | any other controller failure |

This is a pure Delegation surface — it does not modify Sim or Projection swarm files.

---

## Catalog & scenario authoring

A swarm platform is a [`CatalogSwarmPlatform`](../../src/ProjectAegis.Data/Catalog/CatalogSwarmPlatform.cs)
row: `MaxDrones` (default generic `40`), `IsSwarm`, and the Phase-B fields `DefaultMode`,
`RequiresHost`, `AllowedHostClasses`, `CecCapable`. `CatalogSwarmPlatformDefaults` ships two exemplars
— the abstract `uas-swarm-generic` and the CEC-capable `usn-uas-swarm-cec` — plus the valid mode set
(`Hold`/`Assault`/`Screen`/`Scatter`/`Rejoin`). Catalog rows go through the extend-only
[write gate](catalog-write-gate.md); the near-future entity **caps** (`SwarmTier` → 50/500/5000) are a
separate req-09 concern.

Scenario ORBAT units carry a `SwarmMissionType`
([`SwarmMissionType.cs`](../../src/ProjectAegis.Data/Scenario/Authoring/SwarmMissionType.cs)) that maps
to a default operational mode:

| Mission type | Default mode |
|--------------|--------------|
| `Patrol` | `Hold` |
| `Support` | `Screen` |
| `Strike` | `Assault` |

`SwarmMissionDefaults.ResolveMode(missionType, explicitMode)` prefers an explicit mode and otherwise
falls back to the mission default. Placement/config is validated by `SwarmScenarioValidation`.

---

## Presentation (C2 read-models)

[`SwarmUnitPanelProjection`](../../src/ProjectAegis.Delegation/Projection/SwarmUnitPanelProjection.cs)
projects a **pure** panel view model for a single selected swarm unit: integrity line, a fixed
`DENSITY: swarm (aggregate)` label, and (Phase B) mode/host/link/CEC lines. Selection is always
exactly one unit id — there are no per-drone nodes. Missing telemetry renders as
`FIELD: unknown (reason)` (the CMD-17 explicit-unknown pattern). These are read-only projections and
never mutate sim state (see the read-model contract in [c2-projection-layer.md](c2-projection-layer.md)).

---

## QA Gauntlet stress axes

Four swarm **stress axes** are declared in
[`production/qa/gauntlet/corpus/stress-axes.yaml`](../../production/qa/gauntlet/corpus/stress-axes.yaml)
(loaded/validated by [`tools/qa-gauntlet/stress_axes.py`](../../tools/qa-gauntlet/stress_axes.py)):

| Axis | Layers |
|------|--------|
| `swarm_attrition` | `swarm.integrityHitSchedule`, `attritionBias`, `hardCounterAa` |
| `swarm_link` | `swarm.linkJamFromTick`, `linkStateBias` |
| `swarm_mode` | `swarm.modeSequence`, `formationStress` |
| `swarm_softkill` | `swarm.empFreezeSeconds`, `jamSeverity` |

All four are currently **`config-only`** with gap `SWARM-ladder-pending` — they configure pressure but
are not yet runtime-proven because there are no dedicated swarm ladder scenarios; promote them to
`differential-*` proofs when the swarm ladder lands. The `saboteur.py --swarm-filter` path restricts
mutation testing to the swarm-unit oracle family (integrity SoT, regen gate, EMP, caps, splitter). See
[qa-gauntlet.md](qa-gauntlet.md) for the oracle and ladder mechanics.

---

## Determinism & replay

Everything on this page is deterministic given `(seed, inputs)`:

- Integrity mutations are sequence-numbered and folded into `ComputeIntegrityTimelineHash()`; formation
  offsets, assault-share allocation, EMP freeze merges, and CEC mesh iteration all use pure math with
  ordinal-sorted iteration — no wall-clock, no `Random.Shared`.
- [`SwarmReplayHarness.RunGoldenScenario`](../../src/ProjectAegis.Sim/Swarm/SwarmReplayHarness.cs)
  produces a byte-stable canonical fingerprint for a fixed seed, and `Replay(...)` reconstructs it from
  the recorded order + integrity timeline. This golden is **isolated** from the Baltic v2 order-log
  golden (`ReplayGoldenRegressionCatalog`), so the production hash `17144800277401907079` is untouched.
- `RunDesignMaxStress` proves aggregate work stays `O(swarms × ticks)` under `16 × 40` load.

---

## How to extend

1. **New operational mode / intent** — add to `SwarmOperationalMode` / `SwarmIntentKind`, wire the
   `Issue*` path on `SwarmController`, and (if it maps from a mission) update
   `SwarmMissionDefaults` + `CatalogSwarmPlatformDefaults.ValidModes`. Add a `SwarmReplayHarness`
   golden case if the order log changes.
2. **New soft-kill effect** — add a `SwarmSoftKillKind` + evaluator (pure math, no RNG) and an
   applicator method that logs a reason-tagged event; keep it external to `SwarmController` internals.
3. **New engagement interaction** — apply integrity **only** through
   `SwarmEngagementIntegrityApplier` / `ISwarmIntegrityDamageSink`; never mutate drone count directly.
4. **New catalog field** — extend `CatalogSwarmPlatform` (extend-only via the
   [write gate](catalog-write-gate.md)) and thread it through `SwarmUnitFactory`.
5. **New policy grant** — add a bool to `EffectivePolicy`, gate it in `PolicyEvaluator`, add a
   `FireAbortReason`, and source it from scenario policy JSON — never a C# constant.

Before finishing, run the swarm tests (`dotnet test src/ProjectAegis.Sim.Tests/ProjectAegis.Sim.Tests.csproj
--filter Swarm`) plus the full verification block in [`AGENTS.md`](../../AGENTS.md), and confirm the
Baltic v2 replay hash is unchanged.

---

## Related references

| Where | What |
|-------|------|
| [`Game-Requirements/requirements/22-Drone-Swarm-Platforms.md`](../../Game-Requirements/requirements/22-Drone-Swarm-Platforms.md) | The requirement (doc 22, FR-20) + CEC addendum. |
| [engagement-pipeline.md](engagement-pipeline.md) | The tick-8 kill chain swarms plug into. |
| [detection-pipeline.md](detection-pipeline.md) | The tick-4 detection roll swarm sensor scale multiplies. |
| [autonomy-roe-gating.md](autonomy-roe-gating.md) | The ROE/WRA gate the SWARM-15 grants extend. |
| [scenario-policy-authoring.md](scenario-policy-authoring.md) | Where `AutoEngageAuthorized` / `ExpendAuthorized` are authored. |
| [catalog-write-gate.md](catalog-write-gate.md) | The extend-only path for new swarm catalog rows/fields. |
| [qa-gauntlet.md](qa-gauntlet.md) | The gauntlet oracle + the swarm stress axes above. |
