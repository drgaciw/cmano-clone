# Delegation ⇄ Unity bridge

How the headless deterministic delegation/sim talks to a Unity (or any ECS)
host: the `DelegationBridge` facade, the two contracts the host must implement
(`ISimWorldSnapshot`, `IOrderSink`), the per-tick pipeline, human-order
injection, the optional MVP engage path, and the read-only C2 presentation seam.

This is the deep reference for `src/ProjectAegis.Delegation.UnityAdapter`. The
project [`README`](../../src/ProjectAegis.Delegation.UnityAdapter/README.md) is a
30-second quick start; this doc is the contract and lifecycle reference for
people wiring the bridge into a sim loop or extending it.

## Intent

`ProjectAegis.Delegation.UnityAdapter` is a **thin, `UnityEngine`-free** adapter
(ADR-010 headless-first, command-driven UI). It has no Unity dependency, so the
whole tick path is exercised by `dotnet test` — Unity is just one possible host.
The seam exists so the deterministic core (`ProjectAegis.Delegation` +
`ProjectAegis.Sim`) never imports engine types, and the engine never reaches into
delegation internals: everything crosses through `DelegationBridge`.

The bridge owns three responsibilities:

1. **Entity ↔ target mapping** — translate opaque sim/ECS entity ids
   (`EntityKey`) to delegation `TargetId`s and back (`TargetRegistry`).
2. **Per-tick orchestration** — convert a world snapshot into an `ObservedState`,
   run delegation (and optionally the MVP engage session), and push resulting
   orders back into the sim through a sink.
3. **Player intent injection** — accept human orders / attack-menu selections and
   route them deterministically into the orchestrator.

## The two host contracts

A host implements two interfaces. The bridge never reads engine state directly —
it only sees what these contracts expose.

### `ISimWorldSnapshot` — sim → bridge (per tick)

`src/ProjectAegis.Delegation.UnityAdapter/Bridge/ISimWorldSnapshot.cs`. An
immutable read of the world for one tick. The host (Unity ECS systems, or a test
stub) builds one each step.

| Member | Meaning |
|--------|---------|
| `SimTime` | Current sim clock; also reused as the integer sim-tick (`(ulong)Math.Max(0, (long)SimTime)`) for comms/spoof/fuel timelines and order-delay math. |
| `ContactCount` | Number of hostile contacts in the picture. |
| `ActiveEngagementCount` | In-flight engagements. |
| `IsMemberAlive(TargetId)` | Alive flag for a registered unit; **return `false` for unknown ids**. |
| `PrimaryHostileContactId` | Primary hostile for engage/sensor MVP; `null` when `ContactCount == 0`. |
| `HasFireControlTrackOnPrimaryContact` | FC-quality track on the primary contact. |
| `ObserverRadarEmconActive` | Whether the observer's radar EMCON permits active illumination. |

### `IOrderSink` — bridge → sim (per tick)

`src/ProjectAegis.Delegation.UnityAdapter/Bridge/IOrderSink.cs`. One method,
`ApplyOrder(EntityKey entity, in Order order)`. The bridge calls it once per
dispatched order; the host applies it to its movement / weapons / EW systems.
Orders are passed back keyed by the **`EntityKey`**, not the `TargetId`, so the
host never has to maintain its own reverse map.

## Data flow

```text
ECS / sim systems
    │  build ISimWorldSnapshot (contacts, engagements, member-alive)
    ▼
DelegationBridge.Tick(snapshot, sink)
    │  1. EmitCommsTransitions / AdvanceSpoofTimeline / EmitFuelTransitions
    │  2. ObservedStateBuilder.Build(snapshot, registry members)
    │  3. Session.Tick(observed)  (MVP engage, if enabled)  ── else ──┐
    │  4. Orchestrator.Tick(observed)                                  │
    │  5. OrderDispatcher.Dispatch(executedOrders, registry, sink) ◄───┘
    ▼
IOrderSink.ApplyOrder(entityKey, order)  →  host movement / weapons / EW
```

## Registration and the target map

`TargetRegistry` (`Bridge/TargetRegistry.cs`) is the bidirectional map. It also
registers each target with the underlying `DelegationOrchestrator`, so callers
register once and the orchestrator stays in sync.

```csharp
var bridge = new DelegationBridge(globalSeed: 42);
var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
var group    = bridge.Registry.RegisterGroup(new EntityKey(100), "blue-group");
bridge.Registry.LinkGroupMember(group.TargetId, friendly.TargetId);
```

Constraints enforced at registration:

- An `EntityKey` may be registered **once** — a second `RegisterUnit`/
  `RegisterGroup` on the same key throws `InvalidOperationException`.
- `LinkGroupMember` requires both the group **and** the member to be registered
  first, and the group target must actually be a `GroupTarget`; otherwise it
  throws.
- Only `UnitTarget`s are collected as "members" (`CollectMemberIds()`), which is
  the set fed to `ISimWorldSnapshot.IsMemberAlive` each tick.

## The tick pipeline

`DelegationBridge.Tick` (`Bridge/DelegationBridge.cs`) is the heart of the
adapter. Order of operations per call:

1. **Scenario timelines drain first.** `EmitCommsTransitions`,
   `AdvanceSpoofTimeline`, and `EmitFuelTransitions` run before delegation so the
   decision log records comms-state, spoof, and fuel changes for this tick. Each
   timeline is `null` (a no-op) unless the active `ScenarioPolicy` enabled it via
   the corresponding `TryCreate`.
2. **Observe.** `ObservedStateBuilder.Build` projects the snapshot plus the
   member-alive map into an `ObservedState` (the only delegation-facing view of
   the world).
3. **Run.**
   - **MVP engage enabled (`Session != null`):** `Session.Tick(observed)` runs
     the engage pipeline. If it returns `false` (e.g. still planning), the bridge
     still calls `Orchestrator.Tick(observed)` and returns an empty result. If it
     returns `true`, engage orders are filtered out of the dispatch set (the sim
     session already handled them) and only non-engage orders go to the sink.
   - **Delegation-only (`Session == null`):** `Orchestrator.Tick(observed)` runs
     and **all** executed orders are dispatched.
4. **Dispatch.** `OrderDispatcher.Dispatch` walks executed orders, resolves each
   `order.Target` back to an `EntityKey` via the registry, and calls
   `IOrderSink.ApplyOrder`. Orders whose target is not registered are silently
   skipped (not dispatched, not counted).

### `DelegationTickResult`

`Bridge/DelegationTickResult.cs` — what each tick returns:

| Field | Meaning |
|-------|---------|
| `ExecutedOrders` | All orders the orchestrator executed this tick (engage included). |
| `DispatchedToSim` | Count actually pushed to the sink (≤ `ExecutedOrders.Count`; engage orders and unregistered targets are excluded). |
| `EngagementsResolved` | `Session.Sim.LastEngagementResults.Count` on the MVP path; `0` in delegation-only mode. |

### Phase gate

The bridge does no work until execution begins. While `Phase ==
SimulationPhase.Planning`, `Tick` produces no orders and the sink stays empty;
call `BeginExecution()` (or the host's `BeginExecution`) to advance to
`Executing`. This is verified by `Tick_is_no_op_while_planning` in
`src/ProjectAegis.Delegation.UnityAdapter.Tests/Bridge/DelegationBridgeTests.cs`.

## Construction options

```csharp
new DelegationBridge(
    globalSeed,                  // determinism seed for the orchestrator
    policyEvaluator: null,       // optional IPolicyEvaluator
    mvpEngagement: false,        // bind the MVP engage SimulationSession
    scenarioPolicyId: null,      // load a ScenarioPolicy (e.g. "baltic-patrol")
    catalog: null);              // ICatalogReader; falls back to Baltic fixture
```

Catalog resolution falls back in order: explicit `catalog` →
`CatalogReaderFactory.TryCreateBalticPatrolReader()` →
`InMemoryCatalogReader.BalticPatrolFixture()`, so the bridge always has weapon
envelopes even without a generated catalog. MVP engage can also be turned on
after construction with `EnableMvpEngagement(...)` (the Unity-host opt-in), which
applies catalog-derived engage envelopes over the default context.

`ConfigureSimulationMode(...)` delegates to `SimulationModeConfigurator.Apply` to
assign friendly/opposing controllers, default traits, and agent autonomy in one
call.

## Player intent injection

All player-intent entry points share two guards: they **fail closed** when a
replay viewer is attached (`AttachReplayViewer == true`, so replays stay
read-only) and when the entity is not registered or not under human control.

- **`TryEnqueueHumanOrder(entity, kind, simTime, risk?)`** — enqueues an order on
  a `HumanController`-controlled unit. Risk defaults via
  `DefaultRiskClassifier.Classify(kind)`. The execute tick is delayed by
  `CommsOrderDelay.ComputeExecuteSimTick` using the current comms state, and a
  `PlayerOrderRecord` is appended to the decision log (so player orders show up in
  the order-log fingerprint as `PlayerOrder|…`).
- **`GetAttackMenuOptions(unitId, snapshot)`** — builds live attack options from a
  scenario-derived `EngageContext` and an `EngagePreviewProjection`; returns an
  empty list for an unknown unit.
- **`TryEnqueueAttackOption(entity, optionId, snapshot, out failureReason)`** —
  resolves an attack-menu option to a concrete order, applies an optional salvo
  override to the MVP session, then routes through `TryEnqueueHumanOrder`.
  `failureReason` is set to `UNKNOWN_UNIT` or a resolver reason on failure.
- **`TryTakeDirectControl` / `TryReleaseDirectControl`** — swap a unit between
  agent and direct human control (also blocked while a replay viewer is attached).

`BuildLiveEngageContext` is what makes the attack menu reflect live state: it
overlays `HasFireControlTrack`, `RadarEmconActive`, air-ops readiness (from
`Session.UnitReadiness`), and track-spoof status (from the spoof timeline) onto
the scenario engage defaults.

## The C2 presentation seam (read-only)

`IC2PresentationFeed` (`Bridge/IC2PresentationFeed.cs`) is the **read-only**
contract the UI binds to — last OOB tree, map symbols, sensor/C2 snapshot, top
bar, unit detail, plus `SelectUnit` / `SelectContact`. It is implemented by
`DelegationBridgeHost` (`unity/ProjectAegis/Assets/Scripts/Runtime/DelegationBridgeHost.cs`,
compiled only under `UNITY_5_3_OR_NEWER`).

The host is the canonical wiring example. Its `RunTick(snapshot, sink)`:

1. calls `Bridge.Tick(snapshot, sink)`, then
2. rebuilds every projection (message log, OOB tree, sensor/C2, unit detail, map
   symbols, top bar) from the decision log and registry via the per-panel
   `*Bridge` projectors.

Projections are pure derivations of `(snapshot, DecisionLog, Registry, seed)`, so
the presentation layer adds **zero** determinism risk — it never feeds back into
`Tick`. The `useGlobeMap` flag (Cesium Phase B) is presentation-only for the same
reason.

## Determinism rules for this layer

- The bridge is part of the deterministic path: a given `(scenario, seed, snapshot
  stream)` must produce an identical order log. Keep `Tick` free of wall-clock,
  unseeded RNG, and unordered iteration.
- Do **not** call Hindsight `recall`/`reflect` from `Tick` or any code it reaches
  (see `AGENTS.md`).
- Player orders are logged through the decision log so they participate in the
  order-log fingerprint — don't bypass `TryEnqueueHumanOrder` to mutate
  controllers directly in the tick path.
- The `Session.Sim.LastWorldHash` produced on the MVP path feeds the layered
  `SimWorldHash` used by the replay goldens — see
  [`replay-determinism-harness.md`](replay-determinism-harness.md).

## Common pitfalls

- **Empty sink, no error.** If `IsMemberAlive` only knows about ids you forgot to
  register, or you never called `BeginExecution()`, ticks are valid no-ops. Check
  `Phase` and `Registry.CollectMemberIds()` first.
- **Engage orders "missing" from the sink.** On the MVP path engage orders are
  intentionally filtered from dispatch — the `SimulationSession` already resolved
  them. They still appear in `ExecutedOrders`; `EngagementsResolved` reports the
  count.
- **`DispatchedToSim < ExecutedOrders.Count`.** Expected when an order targets an
  unregistered `TargetId` (silently skipped) or on the MVP engage path.
- **Double registration throws.** Reusing an `EntityKey` is an
  `InvalidOperationException`, not a silent overwrite.
- **Human-order timing.** The execute tick is comms-delayed, so an order
  enqueued at `simTime = t` may not dispatch until a later tick.

## See also

- `src/ProjectAegis.Delegation.UnityAdapter/README.md` — quick start.
- `docs/architecture/adr-010-headless-first-command-driven-ui.md` — adapter seam rationale.
- `docs/architecture/adr-001-sim-assembly-boundary.md` / `adr-002-policy-evaluator.md` — core boundaries.
- `docs/engineering/replay-determinism-harness.md` — the determinism gate the bridge feeds.
- `docs/engineering/doctrine-inheritance-override-runbook.md` — `DoctrineOverrideCommand` used by the host.
- `unity/ProjectAegis/PLAYMODE-SMOKE.md` — headless Play Mode harness exercising `RunTick`.
